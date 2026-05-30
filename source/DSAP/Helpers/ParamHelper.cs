using Archipelago.Core.Models;
using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    internal class ParamHelper
    {
        internal static void UpdateItemLots(Dictionary<int, ItemLot> itemLotReplacementMap)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<ItemLotParam> paramStruct,
                                                     ItemLotParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Debug("Skipping reload of Item Lots");
                //return false;
            }

            Log.Logger.Debug($"ItemParam list rowcount='{paramStruct.ParamEntries.Count}'");

            if (!ItemLotHelper.VerifyItemLots(paramStruct))
            {
                App.Client.AddOverlayMessage($"ERROR DETECTED, SEE DSAP CLIENT LOG!");
                App.Client.AddOverlayMessage($"ERROR DETECTED, SEE DSAP CLIENT LOG!");
                Log.Logger.Error("Incorrect item lots detected. This is usually a sign of leftover mod files,");
                Log.Logger.Error(" and will cause problems with detection of location checks.");
                Log.Logger.Error("RECOMMENDED ACTIONS:");
                Log.Logger.Error("1) Full uninstall of the game,");
                Log.Logger.Error("2) Completely clean out the DS:R install directory, and then");
                Log.Logger.Error("3) Reinstall.");
            }

            // if we are here, we are updating the params.
            int new_entries = 0;
            ItemLotHelper.AddInitItemLots(paramStruct, ref new_entries);
            ItemLotHelper.OverwriteItemLots(paramStruct, itemLotReplacementMap);
            //bool success = ItemLotHelper.AddInitItemLots();

            // add a dummy item at 99999998 so that we can know we've been here.
            byte[] parambytes = new byte[ItemLotParam.Size];
            Array.Copy(BitConverter.GetBytes(-1), 0, parambytes, 0x80, sizeof(int)); // overwrite getitemflagid with -1, so it isn't used
            paramStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            paramStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Information($"Added {new_entries} items to ItemLotParams");

            ParamHelper.WriteFromParamSt(paramStruct, ItemLotParam.spOffset);

            watch.Stop();

            Log.Logger.Information($"Finished overwriting items, took {watch.ElapsedMilliseconds}ms");
            App.Client.AddOverlayMessage($"Finished overwriting items, took {watch.ElapsedMilliseconds}ms");

            Log.Logger.Debug($"Player in game? {(MiscHelper.IsInGame() ? "yes" : "no")}");
            Log.Logger.Debug($"ingame time = {MiscHelper.getIngameTime()}");
            if (MiscHelper.IsInGame())
            {
                App.HomewardBoneCommand();
                Log.Logger.Information($"After Load screen, new item lots will be live.");
                App.Client.AddOverlayMessage($"After Load screen, new item lots will be live.");
            }
            else
            {
                Log.Logger.Information($"You are now safe to load your save.");
                App.Client.AddOverlayMessage($"You are now safe to load your save.");
            }
        }

        internal static bool RemoveSpellRequirements()
        {
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<MagicParam> paramStruct,
                                                     MagicParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Debug("Skipping reload of Magic");
                return false;
            }

            // For each existing spell item, modify required int/faith
            for (int i = 0; i < paramStruct.ParamEntries.Count; i++)
            {
                var ent = paramStruct.ParamEntries[i];
                
                if (App.DSOptions.NoSpellStatRequirements) // Remove int and faith requirements
                {
                    paramStruct.ParamBytes[ent.paramOffset + MagicParam.Int_Requirement] = 0;   // int
                    paramStruct.ParamBytes[ent.paramOffset + MagicParam.Faith_Requirement] = 0; // faith
                }
                if (App.DSOptions.NoMiracleCovenantRequirements) // Remove vow restrictions
                {   
                    paramStruct.ParamBytes[ent.paramOffset + MagicParam.VOW_00_07] = 0xff;   // spell usable while in no covenant and first 7
                    paramStruct.ParamBytes[ent.paramOffset + MagicParam.VOW_08_15] = 0xff;   // spell usable while in any of the other covenants
                }
            }

            if (App.DSOptions.NoSpellStatRequirements) // Remove int and faith requirements
                Log.Logger.Information("Removed spell stat requirements.");
            if (App.DSOptions.NoMiracleCovenantRequirements) // Remove vow restrictions
                Log.Logger.Information("Removed spell covenant requirements.");

            // Get first entry's Param (e.g. White Sign Soapstone), use it as basis for new params.
            byte[] parambytes = new byte[MagicParam.Size];

            // add a dummy item at 99999998 so that we can know we've been here.
            paramStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            paramStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Added 1 items to Spells struct and removed stat or vow requirements");

            ParamHelper.WriteFromParamSt(paramStruct, MagicParam.spOffset);

            return true;
        }
        internal static bool TestMoveChange(int field, int value)
        {
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<MoveParam> paramStruct,
                                                     MoveParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Debug("Skipping reload of Moves");
                return false;
            }
            // if we are here, we are updating the params.

            var updentry = paramStruct.ParamEntries.Find((x) => x.id == 103); // get dog moveset
            Memory.Write(paramStruct.BufferLoc + (ulong)(paramStruct.ParamEntries.Count * 12 + 0x30) + (ulong)field, value);

            // Get first entry's Param (e.g. White Sign Soapstone), use it as basis for new params.
            //byte[] parambytes = new byte[MoveParam.Size];
            //var copyentry = paramStruct.ParamEntries.Find((x) => x.id == 103); // get dog moveset
            //Array.Copy(paramStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);

            //// For each existing item, modify required str/dex/int/faith
            //for (int i = 0; i < paramStruct.ParamEntries.Count; i++)
            //{
            //    var ent = paramStruct.ParamEntries[i];
            //    if (ent.id > 20)
            //        break;
            //    Array.Copy(parambytes, 0, paramStruct.ParamBytes, ent.paramOffset, MoveParam.Size); // copy moveset over taret
            //}

            // add a dummy item at 99999998 so that we can know we've been here.
            //paramStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            //paramStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            //Log.Logger.Information($"Added 1 items to MoveParam struct and changed player moveset");

            //ParamHelper.WriteFromParamSt(paramStruct, MoveParam.spOffset);

            return true;
        }
        internal static uint WeightReducersReceived = 0;
        internal static void UpdateWeightMultiplier()
        {

            uint old_multiplier = CalculateWeightMultiplier();
            var pwr = MiscHelper.GetProgressiveItems().Find(x => x.Name == "Progressive Weight Reducer");
            WeightReducersReceived = (uint)App.Client.CurrentSession.Items.AllItemsReceived.Where(x => x.ItemId == pwr.ApId).Count();
            uint new_multiplier = CalculateWeightMultiplier();

            if (new_multiplier == old_multiplier)
            {
                Log.Logger.Information($"Setting weight multiplier to {new_multiplier}%");
                App.Client.AddOverlayMessage($"Setting weight multiplier to {new_multiplier}%");
            }
            else
            {
                Log.Logger.Information($"Updating weight multiplier from {old_multiplier}% to {new_multiplier}%");
                App.Client.AddOverlayMessage($"Updating weight multiplier from {old_multiplier}% to {new_multiplier}%");
            }
                
        }
        internal static uint CalculateWeightMultiplier()
        {
            uint multiplier = App.DSOptions.WeightMultiplierBase; // base value, if 0 or less multiplier items received

            if (App.DSOptions.WeightMultiplierSteps == 0) // explicitly handle this case to avoid dividing by 0
                multiplier = App.DSOptions.WeightMultiplierBase;
            else if (WeightReducersReceived >= App.DSOptions.WeightMultiplierSteps) // cap at min multiplier
                multiplier = App.DSOptions.WeightMultiplierMin;
            else if (WeightReducersReceived > 0) // otherwise, calculate as part of the way from base to max
            {
                uint difference = (App.DSOptions.WeightMultiplierBase - App.DSOptions.WeightMultiplierMin);
                uint distance = (difference * WeightReducersReceived) / App.DSOptions.WeightMultiplierSteps;
                multiplier = App.DSOptions.WeightMultiplierBase - distance;
            }
            return multiplier;
        }
        internal static bool ModifyWeaponParams()
        {

            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<EquipParamWeapon> weaponParamStruct,
                                                     EquipParamWeapon.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
                Log.Logger.Debug("There may be an error during reloading Weapons Params");


            if (App.DSOptions.NoWeaponRequirements)
                ParamHelper.RemoveWeaponRequirements(weaponParamStruct); // modifies EquipParamWeapons
            if (App.DSOptions.GhostDifficulty == Enums.DSGhostDifficulty.ghosts_are_not_ghostly)
                ParamHelper.AddGhostEffectiveWeapons(weaponParamStruct); // modifies npc params

            if (App.DSOptions.WeightMultiplierBase != 100 || App.DSOptions.WeightMultiplierSteps > 0)
                MultiplyWeaponWeight(weaponParamStruct); // modify weight

            // add a dummy item at 99999998 so that we can know we've been here.
            // Get a weapon's Param, and use it as basis for new params.
            byte[] parambytes = new byte[EquipParamWeapon.Size];
            var copyentry = weaponParamStruct.ParamEntries.Find((x) => x.id == 100000);
            Array.Copy(weaponParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);

            Array.Copy(BitConverter.GetBytes(2000000), 0, parambytes, 0x0, sizeof(int)); // make it an arrow
            parambytes[0x8a] = 98; // set to 98 arrows
            weaponParamStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            weaponParamStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Added 1 items to EquipParamWeapon struct");

            ParamHelper.WriteFromParamSt(weaponParamStruct, EquipParamWeapon.spOffset);
            return true;
        }
        internal static bool RemoveWeaponRequirements(ParamStruct<EquipParamWeapon> paramStruct)
        {
            // if we are here, we are updating the params.
            // For each existing item, modify required str/dex/int/faith
            for (int i = 0; i < paramStruct.ParamEntries.Count; i++)
            {
                var ent = paramStruct.ParamEntries[i];
                paramStruct.ParamBytes[ent.paramOffset + 0xed] = 0; // str
                paramStruct.ParamBytes[ent.paramOffset + 0xee] = 0; // dex
                paramStruct.ParamBytes[ent.paramOffset + 0xef] = 0; // int
                paramStruct.ParamBytes[ent.paramOffset + 0xf0] = 0; // faith
            }

            Log.Logger.Information($"Remvoed stat requirements from all weapons and shields");
            return true;
        }
        // Updates Weapons
        internal static bool AddGhostEffectiveWeapons(ParamStruct<EquipParamWeapon> weaponParamStruct)
        {
            // For each existing weapon, modify it to allow attacking ghosts
            for (int i = 0; i < weaponParamStruct.ParamEntries.Count; i++)
            {
                var ent = weaponParamStruct.ParamEntries[i];
                weaponParamStruct.ParamBytes[ent.paramOffset + 0x102] |= 0x80; // turn on 0x102 bit 7 - isVersusGhostWep
            }
            Log.Logger.Information($"Added ghost-effectiveness to all weapons and shields");

            return true;
        }
        // Updates Weapons
        internal static bool MultiplyWeaponWeight(ParamStruct<EquipParamWeapon> weaponParamStruct)
        {
            uint multiplier = CalculateWeightMultiplier();
            // multiply all souls values
            foreach (var weapon in weaponParamStruct.ParamEntries)
            {
                float weight = BitConverter.ToSingle(weaponParamStruct.ParamBytes, (int)(weapon.paramOffset + EquipParamWeapon.WEIGHT_OFFSET));
                weight = (weight * (int)multiplier) / 100;
                Array.Copy(BitConverter.GetBytes(weight), 0, weaponParamStruct.ParamBytes, weapon.paramOffset + EquipParamWeapon.WEIGHT_OFFSET, sizeof(float));
            }
            Log.Logger.Verbose($"Updated weapon weights to multiplier {multiplier}%");
            return true;
        }
        internal static bool ModifyArmorParams()
        {
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<EquipParamProtector> armorParamStruct,
                                                     EquipParamProtector.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
                Log.Logger.Debug("There may be an error during reloading Protectors Params");

            if (App.DSOptions.WeightMultiplierBase != 100 || App.DSOptions.WeightMultiplierSteps > 0)
                MultiplyArmorWeight(armorParamStruct); // modify weight

            // add a dummy item at 99999998 so that we can know we've been here.
            // Get a armor's Param, and use it as basis for new params.
            byte[] parambytes = new byte[EquipParamProtector.Size];
            var copyentry = armorParamStruct.ParamEntries.Find((x) => x.id == 10000); // catarina helm
            Array.Copy(armorParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);

            armorParamStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            armorParamStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Added 1 items to EquipParamProtector struct");

            ParamHelper.WriteFromParamSt(armorParamStruct, EquipParamProtector.spOffset);
            return true;
        }
        // Updates Armors
        internal static bool MultiplyArmorWeight(ParamStruct<EquipParamProtector> armorParamStruct)
        {
            uint multiplier = CalculateWeightMultiplier();
            // multiply all souls values
            foreach (var armor in armorParamStruct.ParamEntries)
            {
                float weight = BitConverter.ToSingle(armorParamStruct.ParamBytes, (int)(armor.paramOffset + EquipParamProtector.WEIGHT_OFFSET));
                weight = (weight * (int)multiplier) / 100;
                Array.Copy(BitConverter.GetBytes(weight), 0, armorParamStruct.ParamBytes, armor.paramOffset + EquipParamProtector.WEIGHT_OFFSET, sizeof(float));
            }
            Log.Logger.Verbose($"Updated armor weights to multiplier {multiplier}%");
            return true;
        }

        internal static uint SoulMultipliersReceived = 0;
        internal static void UpdateSoulMultiplier()
        {
            uint old_multiplier = CalculateSoulMultiplier();
            var psm = MiscHelper.GetProgressiveItems().Find(x => x.Name == "Progressive Soul Multiplier");
            SoulMultipliersReceived = (uint)App.Client.CurrentSession.Items.AllItemsReceived.Where(x => x.ItemId == psm.ApId).Count();
            uint new_multiplier = CalculateSoulMultiplier();

            if (new_multiplier == old_multiplier)
            {
                Log.Logger.Information($"Setting soul multiplier to {new_multiplier}%");
                App.Client.AddOverlayMessage($"Setting soul multiplier to {new_multiplier}%");
            }
            else
            {
                Log.Logger.Information($"Updating soul multiplier from {old_multiplier}% to {new_multiplier}%");
                App.Client.AddOverlayMessage($"Updating soul multiplier from {old_multiplier}% to {new_multiplier}%");
            }
        }
        internal static uint CalculateSoulMultiplier()
        {
            uint multiplier = App.DSOptions.SoulMultiplierBase; // base value, if 0 or less multiplier items received

            if (App.DSOptions.SoulMultiplierSteps == 0) // explicitly handle this case to avoid dividing by 0
                multiplier = App.DSOptions.SoulMultiplierBase;
            else if (SoulMultipliersReceived >= App.DSOptions.SoulMultiplierSteps) // cap at max multiplier
                multiplier = App.DSOptions.SoulMultiplierMax;
            else if (SoulMultipliersReceived > 0) // otherwise, calculate as part of the way from base to max
            {
                uint difference = (App.DSOptions.SoulMultiplierMax - App.DSOptions.SoulMultiplierBase);
                uint distance = (difference * SoulMultipliersReceived) / App.DSOptions.SoulMultiplierSteps;
                multiplier = App.DSOptions.SoulMultiplierBase + distance;
            }
            return multiplier;
        }
        // Updates Npc Params
        internal static bool ModifyNpcParams()
        {
            // Update the NPC Param Struct, and the Weapon Param struct passed in
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<NpcParam> npcParamStruct,
                                                     NpcParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Warning("Warning: reload of NPC Params");
                //return false;
            }
            // if we are here, we are updating the params.
            if (App.DSOptions.GhostDifficulty == Enums.DSGhostDifficulty.ghosts_are_not_ghostly)
                RemoveGhostGhostliness(npcParamStruct);
            if (App.DSOptions.SoulMultiplierBase != 100 || App.DSOptions.SoulMultiplierSteps > 0)
                MultiplyNpcSouls(npcParamStruct);

            // Get first entry's Param (e.g. test npc), use it as basis for new params.
            byte[] parambytes = new byte[NpcParam.Size];
            var copyentry = npcParamStruct.ParamEntries.Find((x) => x.id == 0);
            Array.Copy(npcParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);

            // add a dummy item at 99999998 so that we can know we've been here.
            npcParamStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            npcParamStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Added 1 items to NpcParam struct");

            ParamHelper.WriteFromParamSt(npcParamStruct, NpcParam.spOffset);
            return true;

        }
        internal static void RemoveGhostGhostliness(ParamStruct<NpcParam> npcParamStruct)
        {
            // find ghosts. Make them non-ghost
            var ghosts = npcParamStruct.ParamEntries.Where(x => x.id == 267000 || x.id == 268000);

            foreach (var ghost in ghosts)
            {
                npcParamStruct.ParamBytes[ghost.paramOffset + 0x145] &= 0xef;   // turn off bit 4, "isGhost"
            }
            Log.Logger.Information($"Removed Ghostliness from ghosts");
        }
        internal static void MultiplyNpcSouls(ParamStruct<NpcParam> npcParamStruct)
        {
            uint multiplier = CalculateSoulMultiplier();
            // multiply all souls values
            foreach (var enemy in npcParamStruct.ParamEntries)
            {
                int souls = BitConverter.ToInt32(npcParamStruct.ParamBytes, (int)(enemy.paramOffset + 0x28));
                souls = (souls * (int)multiplier) / 100;
                Array.Copy(BitConverter.GetBytes(souls), 0, npcParamStruct.ParamBytes, enemy.paramOffset + 0x28, sizeof(int));
            }
            Log.Logger.Verbose($"Updated enemy soul drops with multiplier {multiplier}%");
        }
        // Updates Game Area Params
        internal static bool ModifyGameAreaParams()
        {
            // Update the NPC Param Struct, and the Weapon Param struct passed in
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<GameAreaParam> gameAreaParamStruct,
                                                     GameAreaParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Warning("Warning: reload of GameArea Params");
                //return false;
            }
            // if we are here, we are updating the params.
            if (App.DSOptions.SoulMultiplierBase != 100 || App.DSOptions.SoulMultiplierMax != 100)
                MultiplyBossSouls(gameAreaParamStruct);

            // Get first entry's Param (e.g. test npc), use it as basis for new params.
            byte[] parambytes = new byte[GameAreaParam.Size];
            var copyentry = gameAreaParamStruct.ParamEntries.Find((x) => x.id == 0);
            Array.Copy(gameAreaParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);

            // add a dummy item at 99999998 so that we can know we've been here.
            gameAreaParamStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            gameAreaParamStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            Log.Logger.Debug($"Added 1 items to GameAreaParam struct");

            ParamHelper.WriteFromParamSt(gameAreaParamStruct, GameAreaParam.spOffset);
            return true;

        }
        internal static void MultiplyBossSouls(ParamStruct<GameAreaParam> gameAreaParamStruct)
        {
            uint multiplier = CalculateSoulMultiplier();
            // multiply all souls values
            foreach (var area in gameAreaParamStruct.ParamEntries)
            {
                int singlesouls = BitConverter.ToInt32(gameAreaParamStruct.ParamBytes, (int)(area.paramOffset + 0x4));
                singlesouls = (singlesouls * (int)multiplier) / 100;
                Array.Copy(BitConverter.GetBytes(singlesouls), 0, gameAreaParamStruct.ParamBytes, area.paramOffset + 0x4, sizeof(int));

                int multisouls = BitConverter.ToInt32(gameAreaParamStruct.ParamBytes, (int)(area.paramOffset + 0x0));
                multisouls = (multisouls * (int)multiplier) / 100;
                Array.Copy(BitConverter.GetBytes(multisouls), 0, gameAreaParamStruct.ParamBytes, area.paramOffset + 0x0, sizeof(int));
            }
            Log.Logger.Verbose($"Updated Boss soul drops with multiplier {multiplier}%");
        }
        internal static bool ModifyShopLineupParams()
        {
            // Read in the Param Structure
            // Modify it,
            // Then save it back
            bool reloadRequired = ParamHelper.ReadFromBytes(out ParamStruct<ShopLineupParam> shopLineupParamStruct,
                                                     ShopLineupParam.spOffset,
                                                     (ps) => ps.ParamEntries.Last().id >= 99999990);
            if (!reloadRequired)
            {
                Log.Logger.Warning("Skipping reload of Shop Lineup Params");
                return false;
            }
            // if we are here, we are updating the params.

            // Get rickert's weapon item, use it as basis for new shop lineup item.
            if (App.DSOptions.GhostDifficulty == Enums.DSGhostDifficulty.rickert_sells_curses)
            {
                AddRickertCurses(shopLineupParamStruct);
            }

            byte[] parambytes = new byte[ShopLineupParam.Size];
            var copyentry = shopLineupParamStruct.ParamEntries.Find((x) => x.id == 2102);
            Array.Copy(shopLineupParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);
            // add a dummy item at 99999998 so that we can know we've been here.
            shopLineupParamStruct.AddParam(99999998, parambytes, Encoding.ASCII.GetBytes("")); // mark that we've been here

            shopLineupParamStruct.ParamEntries.Sort((x, y) => (x.id.CompareTo(y.id)));
            
            ParamHelper.WriteFromParamSt(shopLineupParamStruct, ShopLineupParam.spOffset);
            
            Log.Logger.Debug($"Updated Shop Lineup Params");

            return true;
        }
        internal static bool AddRickertCurses(ParamStruct<ShopLineupParam> shopLineupParamStruct)
        {
            // Get rickert's weapon item, use it as basis for new shop lineup item.
            byte[] parambytes = new byte[ShopLineupParam.Size];
            var copyentry = shopLineupParamStruct.ParamEntries.Find((x) => x.id == 2102);
            Array.Copy(shopLineupParamStruct.ParamBytes, copyentry.paramOffset, parambytes, 0, parambytes.Length);
            Array.Copy(BitConverter.GetBytes(312), 0, parambytes, 0, sizeof(int)); // equip id for transient curse
            parambytes[0x17] = (byte)3; // equip type = "good"
            Array.Copy(BitConverter.GetBytes(1500), 0, parambytes, 0x4, sizeof(int)); // value = 1500 souls

            shopLineupParamStruct.AddParam(2103, parambytes, Encoding.ASCII.GetBytes("[AP]+transient curses"));
            Log.Logger.Information("Added transient curses to Rickert's shop");

            return true;
        }


        // return = whether reload is required
        public static bool ReadFromBytes<ParamT>(out ParamStruct<ParamT> result, int soloParamOffset, Func<ParamStruct<ParamT>, bool> isUsedCondition) where ParamT : IParam, new()
        {
            result = new ParamStruct<ParamT>();

            ulong ResCapLoc = Memory.ReadULong((ulong)(AddressHelper.SoloParamAob.Address + soloParamOffset));
            if (ResCapLoc == 0)
            {
                Log.Logger.Error($"Could not find params at offset {soloParamOffset}");
            }
            int BufferSize = (int)Memory.ReadUInt(ResCapLoc + 0x30);
            ulong BufferLoc = Memory.ReadULong(ResCapLoc + 0x38);

            result.ReadFromBytes(BufferLoc, BufferSize, isUsedCondition);
            if (result.DescArea != null)
            {
                bool canReload = MiscHelper.ValidateDescArea(result.DescArea, typeof(ParamT).Name);
                if (!canReload)
                {
                    return false;
                }
                Log.Logger.Warning($"overwriting {typeof(ParamT).Name}");
                result.ReadFromBytes(result.DescArea.OldAddress, result.DescArea.OldLength, isUsedCondition);
            }
            return true;
        }
        public static bool WriteFromParamSt<ParamT>(ParamStruct<ParamT> input, int soloParamOffset) where ParamT : IParam, new()
        {
            ulong resCapLoc = Memory.ReadULong((ulong)(AddressHelper.SoloParamAob.Address + soloParamOffset));
            int oldBufferSize = (int)Memory.ReadUInt(resCapLoc + 0x30);
            ulong oldBufferLoc = Memory.ReadULong(resCapLoc + 0x38);
            bool hadOldUpdatedArea = false;
            // if "desc area" exists, then we're reloading the new updated area. Set up for later Free().
            if (input.DescArea != null)
            {
                hadOldUpdatedArea = true;
                oldBufferSize = input.DescArea.FullAllocLength;
                oldBufferLoc = input.BufferLoc - 0x10;
            }

            byte[] newBytes = input.GenerateWriteArray(out int shortLength);
            ulong allocArea = (ulong)Memory.Allocate((uint)newBytes.Length);
            Log.Logger.Debug($"Allocated {newBytes.Length:X} bytes at {allocArea:X}");

            Memory.WriteByteArray(allocArea, newBytes);
            ulong newBufferLoc = allocArea + 0x10; // get past prologue

            
            Log.Logger.Debug($"Overwrite {typeof(ParamT).Name} @ {oldBufferLoc:X} to {allocArea:X}");

            /* Then switch out the pointer */
            Memory.Write(resCapLoc + 0x38, newBufferLoc);
            Memory.Write(resCapLoc + 0x30, shortLength);

            if (hadOldUpdatedArea)
            {
                Memory.FreeMemory((nint)oldBufferLoc);
                Log.Logger.Debug($"Free old {typeof(ParamT).Name} @ {oldBufferLoc:X}");
            }
            
            return true;
        }
    }
}
