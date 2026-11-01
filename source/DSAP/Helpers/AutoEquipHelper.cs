using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DSAP.Enums;

namespace DSAP.Helpers
{
    /// <summary>
    /// Equips weapons, shields, armor and rings as they are received.
    /// Items found in our own world come back to us from the server, so this one path covers both
    /// pickups and items sent by other players. Unrandomized items never go through here.
    ///
    /// The game equips an item once its id and inventory row are written to these arrays. Layout from
    /// the UWYG mod (github.com/sebastien-lorrain/Gaming.DarkSoulsRemastered-UWYG-QoL-Edition).
    /// </summary>
    internal class AutoEquipHelper
    {
        /* Offsets from PlayerGameData */
        private const uint InventoryHeaderOffset = 0x670;
        private const uint InventoryOffset = 0x680;
        private const uint WeaponSlotsOffset = 0x2A4; // left 1, right 1, left 2, right 2
        private const uint ArmorSlotsOffset = 0x2C4; // head, chest, hands, legs
        private const uint RingSlotsOffset = 0x2D8; // left, right
        private const uint WeaponIdsOffset = 0x324;
        private const uint ArmorIdsOffset = 0x344;
        private const uint RingIdsOffset = 0x358;

        private const int WeaponSlotCount = 4;
        private const int ArmorSlotCount = 4;
        private const int RingSlotCount = 2;
        private const int WeaponLeft1 = 0;
        private const int WeaponRight1 = 1;
        private const int WeaponLeft2 = 2;

        /* Inventory rows: category, id, count, (unknown), valid, durability, hits */
        private const int InventoryRowCount = 2048;
        private const int InventoryRowSize = 28;
        private const int RowIdOffset = 0x4;
        private const int RowValidOffset = 0x10;
        private const int EmptySlot = -1;

        private const int RowLookupAttempts = 5;
        private const int RowLookupDelayMs = 100;
        private const int SkullLanternId = 1396000;
        private const int CovenantOfArtoriasId = 138;
        private const int OrangeCharredRingId = 139;

        private readonly record struct EquipTarget(uint IdsOffset, uint SlotsOffset, int Index);

        private static readonly object _equipLock = new object();
        private static int _nextRingSlot = 0;

        /* Weapon categories share one enum value, and reinforcing or infusing an item only changes its last 3 digits. */
        private static readonly Lazy<HashSet<int>> MeleeWeapons = new(() => WeaponFamilies(MiscHelper.GetMeleeWeapons()));
        private static readonly Lazy<HashSet<int>> Shields = new(() => WeaponFamilies(MiscHelper.GetShields()));
        private static readonly Lazy<HashSet<int>> SpellTools = new(() => WeaponFamilies(MiscHelper.GetSpellTools()));
        private static readonly Lazy<HashSet<int>> RangedWeapons = new(() => WeaponFamilies(
            MiscHelper.GetRangedWeapons().Where(x => !x.Name.Contains("Arrow") && !x.Name.Contains("Bolt")).ToList()));
        private static readonly Lazy<HashSet<int>> Armor = new(() => MiscHelper.GetArmor().Select(x => x.Id).ToHashSet());
        private static readonly Lazy<HashSet<int>> Rings = new(() => MiscHelper.GetRings().Select(x => x.Id).ToHashSet());

        private static HashSet<int> WeaponFamilies(List<DarkSoulsItem> items) => items.Select(x => WeaponFamily(x.Id)).ToHashSet();
        private static int WeaponFamily(int id) => id / 1000;

        /// <summary>
        /// Equip a just-received item, if it is equipment and auto equip is on.
        /// </summary>
        internal static void TryEquip(DarkSoulsItem item)
        {
            if (!App.ControlsContext.AutoEquip || !IsEquippable(item))
                return;

            Task.Run(async () =>
            {
                try
                {
                    // Receiving is done on a game thread, so the new row may take a moment to show up.
                    for (int attempt = 0; attempt < RowLookupAttempts; attempt++)
                    {
                        if (!MiscHelper.IsInGame())
                            return;
                        lock (_equipLock)
                        {
                            if (EquipFromInventory(item))
                                return;
                        }
                        await Task.Delay(RowLookupDelayMs);
                    }
                    Log.Logger.Warning($"Auto equip: {item.Name} ({item.Id}) was not found in the inventory.");
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Auto equip of {item.Name} failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Print whether the equipment memory looks the way auto equip expects (for /autoequip status).
        /// </summary>
        internal static void LogStatus()
        {
            Log.Logger.Warning($"Auto equip is {(App.ControlsContext.AutoEquip ? "on" : "off")}.");
            if (!MiscHelper.IsInGame())
            {
                Log.Logger.Warning("Load into a save to check the equipment memory.");
                return;
            }
            ulong playerGameData = AddressHelper.GetPlayerGameDataAddress();
            byte[] inventory = Memory.ReadByteArray(playerGameData + InventoryOffset, InventoryRowCount * InventoryRowSize);
            if (TryValidateLayout(playerGameData, inventory, out string reason))
                Log.Logger.Warning("Equipment memory looks correct.");
            else
                Log.Logger.Error($"Equipment memory check failed: {reason}");
        }

        private static bool IsEquippable(DarkSoulsItem item)
        {
            if (item.Category == DSItemCategory.Armor)
                return Armor.Value.Contains(item.Id);
            if (item.Category == DSItemCategory.Rings)
                return Rings.Value.Contains(item.Id);
            if (item.Category != DSItemCategory.AnyWeapon)
                return false;

            int family = WeaponFamily(item.Id);
            return MeleeWeapons.Value.Contains(family) || Shields.Value.Contains(family)
                || SpellTools.Value.Contains(family) || RangedWeapons.Value.Contains(family);
        }

        /// <returns>False if the item's inventory row isn't there yet, true once there is nothing left to retry.</returns>
        private static bool EquipFromInventory(DarkSoulsItem item)
        {
            ulong playerGameData = AddressHelper.GetPlayerGameDataAddress();
            if (playerGameData == 0)
                return false;

            byte[] inventory = Memory.ReadByteArray(playerGameData + InventoryOffset, InventoryRowCount * InventoryRowSize);
            if (!TryValidateLayout(playerGameData, inventory, out string reason))
            {
                // A wrong write here would corrupt the character's equipment, so don't risk it.
                Log.Logger.Error($"Auto equip skipped for {item.Name}: {reason}");
                return true;
            }

            int row = FindUnequippedRow(playerGameData, inventory, item);
            if (row == EmptySlot)
                return false;

            EquipTarget? target = GetTarget(playerGameData, item);
            if (target == null)
            {
                Log.Logger.Information($"Auto equip: no free slot for {item.Name}.");
                return true;
            }

            int rowId = BitConverter.ToInt32(inventory, row * InventoryRowSize + RowIdOffset);
            uint elementOffset = (uint)target.Value.Index * sizeof(int);
            Memory.Write(playerGameData + target.Value.IdsOffset + elementOffset, rowId);
            Memory.Write(playerGameData + target.Value.SlotsOffset + elementOffset, row);
            Log.Logger.Information($"Auto-equipped {item.Name}.");
            return true;
        }

        /// <summary>
        /// Check the header of the inventory, and that each equipped item is in the inventory row it claims to be.
        /// </summary>
        private static bool TryValidateLayout(ulong playerGameData, byte[] inventory, out string reason)
        {
            reason = "";
            if (playerGameData == 0)
            {
                reason = "player data not found";
                return false;
            }

            if (Memory.ReadInt(playerGameData + InventoryHeaderOffset + 0x8) != InventoryRowCount)
            {
                reason = "inventory header not found";
                return false;
            }

            return SlotsMatchInventory(playerGameData, inventory, WeaponSlotsOffset, WeaponIdsOffset, WeaponSlotCount, "weapon", ref reason)
                && SlotsMatchInventory(playerGameData, inventory, ArmorSlotsOffset, ArmorIdsOffset, ArmorSlotCount, "armor", ref reason)
                && SlotsMatchInventory(playerGameData, inventory, RingSlotsOffset, RingIdsOffset, RingSlotCount, "ring", ref reason);
        }

        private static bool SlotsMatchInventory(ulong playerGameData, byte[] inventory, uint slotsOffset, uint idsOffset, int count, string name, ref string reason)
        {
            for (uint i = 0; i < count; i++)
            {
                int row = Memory.ReadInt(playerGameData + slotsOffset + i * sizeof(int));
                int id = Memory.ReadInt(playerGameData + idsOffset + i * sizeof(int));
                if (row == EmptySlot)
                    continue;
                if (row < 0 || row >= InventoryRowCount
                    || BitConverter.ToInt32(inventory, row * InventoryRowSize + RowIdOffset) != id)
                {
                    reason = $"{name} slot {i} holds id {id} at row {row}, which doesn't match the inventory";
                    return false;
                }
            }
            return true;
        }

        /// <returns>The newest row holding this item that isn't already equipped, or EmptySlot if there is none.</returns>
        private static int FindUnequippedRow(ulong playerGameData, byte[] inventory, DarkSoulsItem item)
        {
            HashSet<int> equipped = new HashSet<int>();
            ReadInts(playerGameData + WeaponSlotsOffset, WeaponSlotCount, equipped);
            ReadInts(playerGameData + ArmorSlotsOffset, ArmorSlotCount, equipped);
            ReadInts(playerGameData + RingSlotsOffset, RingSlotCount, equipped);

            int category = (int)item.Category;
            for (int row = InventoryRowCount - 1; row >= 0; row--)
            {
                int start = row * InventoryRowSize;
                int rowId = BitConverter.ToInt32(inventory, start + RowIdOffset);
                if (BitConverter.ToInt32(inventory, start) == category
                    && (rowId == item.Id || rowId == item.Id + category)
                    && BitConverter.ToInt32(inventory, start + RowValidOffset) != 0
                    && !equipped.Contains(row))
                {
                    return row;
                }
            }
            return EmptySlot;
        }

        private static void ReadInts(ulong address, int count, HashSet<int> values)
        {
            for (uint i = 0; i < count; i++)
                values.Add(Memory.ReadInt(address + i * sizeof(int)));
        }

        private static EquipTarget? GetTarget(ulong playerGameData, DarkSoulsItem item)
        {
            if (item.Category == DSItemCategory.Armor)
                return new EquipTarget(ArmorIdsOffset, ArmorSlotsOffset, (item.Id % 10000) / 1000);
            if (item.Category == DSItemCategory.Rings)
                return GetRingTarget(playerGameData, item);

            int family = WeaponFamily(item.Id);
            int hand;
            if (MeleeWeapons.Value.Contains(family))
                hand = WeaponRight1;
            else if (Shields.Value.Contains(family) && item.Id != SkullLanternId)
                hand = WeaponLeft1;
            else // bows, catalysts and the lantern get the spare hand, so they don't displace the main loadout
                hand = WeaponLeft2;
            return new EquipTarget(WeaponIdsOffset, WeaponSlotsOffset, hand);
        }

        private static EquipTarget? GetRingTarget(ulong playerGameData, DarkSoulsItem item)
        {
            int left = Memory.ReadInt(playerGameData + RingIdsOffset);
            int right = Memory.ReadInt(playerGameData + RingIdsOffset + sizeof(int));
            if (left == item.Id || right == item.Id) // the game doesn't allow wearing the same ring twice
                return null;

            int slot;
            if (left == EmptySlot)
                slot = 0;
            else if (right == EmptySlot)
                slot = 1;
            else if (IsProtectedRing(left) && IsProtectedRing(right))
                return null;
            else if (IsProtectedRing(left))
                slot = 1;
            else if (IsProtectedRing(right))
                slot = 0;
            else
            {
                slot = _nextRingSlot;
                _nextRingSlot = 1 - _nextRingSlot;
            }
            return new EquipTarget(RingIdsOffset, RingSlotsOffset, slot);
        }

        /* Taking these off in the Abyss or in lava kills the player */
        private static bool IsProtectedRing(int id) => id == CovenantOfArtoriasId || id == OrangeCharredRingId;
    }
}
