using Archipelago.Core.Util;
using DSAP.Models;
using DynamicData.Aggregation;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Location = Archipelago.Core.Models.Location;
namespace DSAP.Helpers
{
    public class MiscHelper
    {
        /// <summary>
        /// Determine if a reload of this area should be done
        /// </summary>
        /// <param name="descArea"></param>
        /// <param name="checkArea"></param>
        /// <returns>whether can continue with reload</returns>
        internal static bool ValidateDescArea(DescArea descArea, string checkArea)
        {
            if (descArea.DescSize >= DescArea.size)
            {
                int old_slot = descArea.Slot;

                if (descArea.SeedHash != MiscHelper.HashSeed(App.Client.CurrentSession.RoomState.Seed)) // different seed
                {
                    if (MiscHelper.IsInGame())
                    {
                        App.Client.AddOverlayMessage($"Error - check the client log");
                        Log.Logger.Error("Different seed detected than your previous connection to Archipelago.");
                        Log.Logger.Error("However, you are loaded into a save. Try again while not loaded in.");
                        return false;
                    }
                    else
                    {
                        Log.Logger.Information("Different seed detected than your previous load. Resetting area");
                        return true;
                    }


                }
                else if (old_slot != App.Client.CurrentSession.ConnectionInfo.Slot) // different slot
                {
                    if (MiscHelper.IsInGame())
                    {
                        App.Client.AddOverlayMessage($"Error - check the client log");
                        Log.Logger.Error("Different slotdetected than your previous connection to Archipelago.");
                        Log.Logger.Error("However, you are loaded into a save. Try again while not loaded in.");
                        return false;
                    }
                    else
                    {
                        Log.Logger.Information("Different seed detected than your previous load. Resetting area");
                        return true;
                    }
                }
                else // seed and slot checked out fine. Looks good to proceed
                {
                    return true;
                }
            }
            else // desc area too small
            {
                Log.Logger.Error($"Unknown metadata size detected on {checkArea}. A different mod may be interfering.");
                Log.Logger.Error("Try restarting DSR without other mods.");
                return false; // stop reload
            }
        }
        internal static int GetPlayerHP()
        {
            return Memory.ReadInt(AddressHelper.GetPlayerHPAddress());
        }

        public static ulong OffsetPointer(ulong ptr, uint offset)
        {
            ulong newAddress = ptr;
            return ptr + (ulong)offset;
        }

        public static bool GetIsPlayerOnline()
        {
            var baseCOffset = AddressHelper.GetBaseCOffset();
            ulong onlineFlagOffset = 0xB7D;

            var isOnline = Memory.ReadByte(baseCOffset + onlineFlagOffset) != 0;
            return isOnline;

        }
        public static bool SetLastBonfireTo(Enums.Bonfires bonfireId)
        {
            var baseCoff = AddressHelper.GetBaseCOffset();
            if (baseCoff != 0)
            {
                var baseC = Memory.ReadULong(baseCoff);
                if (baseC != 0)
                {
                    var lastBonfireAddress = OffsetPointer(baseC, 0xB34);
                    Memory.Write(lastBonfireAddress, (int)bonfireId);
                    return true;
                }
            }
            return false;
        }
        public static LastBonfire GetLastBonfire()
        {

            var baseC = AddressHelper.GetBaseCOffset();
            var lastBonfireAddress = OffsetPointer(baseC, 0xB34);
            var lastBonfireId = Memory.ReadInt(lastBonfireAddress);
            //todo get last bonfire
            var list = GetLastBonfireList();
            var lastBonfire = list.FirstOrDefault(x => x.id == lastBonfireId);
            if (lastBonfire != null)
            {
                return lastBonfire;
            }
            return null;
        }
        public static async void MonitorLastBonfire(Action<LastBonfire> action)
        {
            var lastBonfire = GetLastBonfire();
            if (lastBonfire == null)
            {
                Log.Logger.Debug("No Last Bonfire found");
            }
            else Log.Logger.Debug($"Last bonfire was {lastBonfire.id}:{lastBonfire.name} ");
            while (true)
            {
                var currentLastBonfire = GetLastBonfire();
                if (currentLastBonfire != lastBonfire)
                {
                    Log.Logger.Debug("Last Bonfire Changed");
                    lastBonfire = currentLastBonfire;
                    action?.Invoke(currentLastBonfire);
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        public static List<LastBonfire> GetLastBonfireList()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.LastBonfire.json");
            var list = JsonSerializer.Deserialize<List<LastBonfire>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<Loadout> GetLoadouts()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Loadouts.json");
            var list = JsonSerializer.Deserialize<List<Loadout>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<EventFlag> GetGiftParams()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.GiftParams.json");
            var list = JsonSerializer.Deserialize<List<EventFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<Gift> GetGiftsPool()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.GiftsPool.json");
            var list = JsonSerializer.Deserialize<List<Gift>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<ThiefItem> GetThiefItemsPool()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.ThiefItemsPool.json");
            var list = JsonSerializer.Deserialize<List<ThiefItem>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetConsumables()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Consumables.json");
            var list = System.Text.Json.JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetUpgradeMaterials()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.UpgradeMaterials.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetEmbers()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Embers.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetKeyItems()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.KeyItems.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetRings()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Rings.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetSpells()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Spells.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetShields()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Shields.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetTraps()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Traps.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetBonfireWarpItems()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Bonfires.json");
            var list = JsonSerializer.Deserialize<List<BonfireWarp>>(json, GetJsonOptions());
            List<DarkSoulsItem> newlist = list.Where(x => x.ItemId != 0).Select(x => new DarkSoulsItem()
            {
                Name = x.ItemName,
                Id = x.DsrId, // dsr id of warp unlock item
                StackSize = 1,
                UpgradeType = Enums.ItemUpgrade.None,
                Category = Enums.DSItemCategory.BonfireWarp,
                ApId = x.ItemId, // ap id of event item
            }).ToList();
            return newlist;
        }
        // flag, entityid, fmgid
        public static List<(int, int, int)> GetBonfireDsrStruct(bool vanillaNames, string sortType)
        {
            var list = App.AllowedBonfireWarps;
            List<(int, int, int)> dsrBonfireList = [];
            if (!App.DSOptions.WarpToAllBonfires) // only vanilla warps
                list = list.Where(x => x.Flag <= 220).ToList();
            
            if (sortType == "Vanilla") // vanilla sort
                list = list.OrderBy(x => x.VanillaSort).ToList();
            else if (sortType == "Alphabetical") // alphabetical sort, but force Firelink to the top
            {
                if (vanillaNames)
                    list = list.OrderBy(x => x.Name == "Firelink Shrine" ? "aa" : x.VanillaName).ToList();
                else
                    list = list.OrderBy(x => x.Name == "Firelink Shrine" ? "aa" : x.Name).ToList();
            }
            else if (sortType == "Progression") // progression sort
            {
                list = list.OrderBy(x => x.ProgressionSort).ToList();
            }

            if (vanillaNames)
                dsrBonfireList = list.Select(x => (x.Flag, x.EntityId, x.VanillaFmgId))
                    .ToList();
            else
                dsrBonfireList = list.Select(x => (x.Flag, x.EntityId, x.UpdatedFmgId))
                    .ToList();

            return dsrBonfireList;
        }
        public static List<BonfireWarp> GetBonfireWarpInfos()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Bonfires.json");
            var list = JsonSerializer.Deserialize<List<BonfireWarp>>(json, GetJsonOptions());
            return list;
        }
        internal static List<DarkSoulsItem> cached_keychains = null;
        public static List<DarkSoulsItem> GetKeychainItems()
        {
            if (cached_keychains != null)
                return cached_keychains;

            var json = OpenEmbeddedResource("DSAP.Resources.Keychains.json");
            var list = JsonSerializer.Deserialize<List<Keychain>>(json, GetJsonOptions());
            List<DarkSoulsItem> newlist = list.Select(x => new DarkSoulsItem()
            {
                Name = x.Itemname,
                Id = x.Dsrid, // dsr id of keychain item
                StackSize = 1,
                UpgradeType = Enums.ItemUpgrade.None,
                Category = Enums.DSItemCategory.KeyItems
            }
            ).ToList();
            cached_keychains = newlist;
            return cached_keychains;
        }
        public static List<DarkSoulsItem> GetDsrEventItems()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.DsrEvents.json");
            var list = JsonSerializer.Deserialize<List<DsrEvent>>(json, GetJsonOptions());
            List<DarkSoulsItem> newlist = list.Select(x => new DarkSoulsItem()
            {
                Name = x.Itemname,
                Id = x.Dsrid, // dsr id of event item
                StackSize = 1,
                UpgradeType = Enums.ItemUpgrade.None,
                Category = Enums.DSItemCategory.DsrEvent,
                ApId = x.Itemid, // ap id of event item
            }
            ).ToList();
            return newlist;
        }
        public static List<EmkController> GetDsrEventEmks()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.DsrEvents.json");
            var list = JsonSerializer.Deserialize<List<DsrEvent>>(json, GetJsonOptions());
            List<EmkController> newlist = list.Select(x => new EmkController(x.Locname, x.KeychainName, x.Type,
                x.Eventid, x.Eventslot, x.Itemid)).ToList();
            return newlist;
        }
        static public Dictionary<string, DsrEvent> cached_DsrBossDeps = [];
        public static Dictionary<string, DsrEvent> GetDsrBossDeps() // returns a map of boss loc ap ids to the dsr events whose fogwalls they should pop if cheesed
        {
            if (cached_DsrBossDeps.Count == 0)
            {
                var json = OpenEmbeddedResource("DSAP.Resources.DsrEvents.json");
                var events = JsonSerializer.Deserialize<List<DsrEvent>>(json, GetJsonOptions());

                Dictionary<string, DsrEvent> newlist = [];
                foreach (var dsrevent in events)
                {
                    if (dsrevent.BossName.Length > 0)
                    {
                        newlist.Add(dsrevent.BossName, dsrevent);
                    }
                }
                cached_DsrBossDeps = newlist;
            }
            return cached_DsrBossDeps;
        }
        public static List<DarkSoulsItem> GetRangedWeapons()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.RangedWeapons.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetMeleeWeapons()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.MeleeWeapons.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetArmor()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.Armor.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetSpellTools()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.SpellTools.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        static Dictionary<int, DarkSoulsItem> cached_all_weapons = [];
        public static Dictionary<int, DarkSoulsItem> GetAllWeaponsById()
        {
            if (cached_all_weapons.Count == 0)
            {
                var melee_weapons = MiscHelper.GetMeleeWeapons().ToList();
                var ranged_weapons = MiscHelper.GetRangedWeapons().Where(x => !x.Name.Contains("Arrow") && !x.Name.Contains("Bolt")).ToList();
                var spell_tools = MiscHelper.GetSpellTools();
                var shields = MiscHelper.GetShields();
                cached_all_weapons = (melee_weapons.Union(ranged_weapons).Union(spell_tools).Union(shields)).ToDictionary(x => x.ApId);
            }
            return cached_all_weapons;
        }
        public static List<DarkSoulsItem> GetProgressiveItems()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.ProgressiveItems.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        static Lookup<string, MapPoi>? cached_mapPois = null;
        public static Lookup<string, MapPoi> GetMapPois()
        {
            if (cached_mapPois == null)
            {
                var json = OpenEmbeddedResource("DSAP.Resources.MapPois.json");
                var list = JsonSerializer.Deserialize<List<MapPoi>>(json, GetJsonOptions());
                cached_mapPois = (Lookup<string, MapPoi>)list.ToLookup(x => x.MapName, x=>x);
            }
            return cached_mapPois;
        }

        public static DarkSoulsItem UpgradeItem(DarkSoulsItem item, bool log = false)
        {
            // Set seed to slot seed hash + apid, then choice.random() into the list of allowed infusions. Then calculate its path depending on level

            ushort roomseed = MiscHelper.HashSeed(App.Client.CurrentSession.RoomState.Seed);
            ushort connslot = (ushort)App.Client.CurrentSession.ConnectionInfo.Slot;

            int itemseed = item.ApId + roomseed + connslot;
            Random rand = new Random(itemseed);
            string infusion_type = "Normal";
            uint lvl = ParamHelper.CalculateIncomingWeaponUpgradeLevel();
            if (lvl == 0)
                return item;

            if (item.UpgradeType == Enums.ItemUpgrade.Infusable)
            {
                int infusionIdx = rand.Next() % App.DSOptions.IncomingWeaponUpgradeInfusionPaths.Count;
                infusion_type = App.DSOptions.IncomingWeaponUpgradeInfusionPaths[infusionIdx];
            }
            if (item.UpgradeType == Enums.ItemUpgrade.InfusableRestricted)
            {
                // get limited infusion list
                List<String> RestrictedInfusions = ["Normal", "Crystal", "Lightning", "Magic", "Divine", "Fire"];
                List<String> AllowedRestrictedInfusions = App.DSOptions.IncomingWeaponUpgradeInfusionPaths.Where(x => RestrictedInfusions.Contains(x)).ToList();
                if (AllowedRestrictedInfusions.Count == 0) // set at minimum Normal
                {
                    AllowedRestrictedInfusions = ["Normal"]; // always must be able to upgrade to something.
                }
                int infusionIdx = rand.Next() % AllowedRestrictedInfusions.Count;
                infusion_type = AllowedRestrictedInfusions[infusionIdx];
            }
            if (item.UpgradeType == Enums.ItemUpgrade.Unique)
            {
                lvl = lvl / 3;
                if (lvl == 0)
                    return item;
            }
            // type, id, max level, min level, prev type id
            Dictionary<String, (int id, uint maxlevel, uint minlevel, string prevtype)> infusionmap = new Dictionary<string, (int id, uint maxlevel, uint minlevel, string prevtype)>
            {
                {"Normal", (0, 15, 0, "")},
                {"Crystal", (1, 15, 10, "Normal")},
                {"Lightning", (2, 15, 10, "Normal")},
                {"Raw", (3, 10, 5, "Normal")},
                {"Magic", (4, 15, 5, "Normal")},
                {"Enchanted", (5, 15, 10, "Magic")},
                {"Divine", (6, 15, 5, "Normal")},
                {"Occult", (7, 15, 10, "Divine")},
                {"Fire", (8, 15, 5, "Normal")},
                {"Chaos", (9, 15, 10, "Fire")},
            };

            if (infusionmap.ContainsKey(infusion_type))
            {
                var infusion_entry = infusionmap[infusion_type];
                if (lvl > infusion_entry.maxlevel) // if raw but > +10, cap it
                    lvl = infusion_entry.maxlevel;
                
                while (lvl < infusion_entry.minlevel) // if it's on the path to this spot, find the earlier branch. e.g. Occult of +9? -> Divine +4
                {
                    infusion_type = infusion_entry.prevtype; // update type
                    infusion_entry = infusionmap[infusion_type]; // update entry
                }

                // should be in range by now..
                //if (lvl >= infusion_entry.minlevel) // if in the range, all good.
                // normalize (e.g. Occult of lvl 11 => Occult +1)
                int actual_lvl = (int)lvl - (int)infusion_entry.minlevel;

                DarkSoulsItem newitem = new DarkSoulsItem
                {
                    Name = item.Name,
                    Id = item.Id + (int)actual_lvl + 100 * infusion_entry.id,
                    StackSize = item.StackSize,
                    UpgradeType = item.UpgradeType,
                    Category = item.Category,
                    ApId = item.ApId
                };

                if (log)
                {
                    string itemUpg = (infusion_type == "Normal" ? "" : (infusion_type + " ")) + (actual_lvl > 0 ? "+" + actual_lvl : "");
                    Log.Logger.Information($"Upgraded item {item.Name} to {itemUpg}");
                    App.Client.AddOverlayMessage($"Upgraded item {item.Name} to {itemUpg}");
                }

                return newitem;

            }

            Log.Logger.Error($"Error upgrading item {item.Name} to {infusion_type}");
            App.Client.AddOverlayMessage($"Error upgrading item {item.Name} to {infusion_type}");

            return item;
        }
        public static List<DarkSoulsItem> GetUsableItems()
        {
            var json = OpenEmbeddedResource("DSAP.Resources.UsableItems.json");
            var list = JsonSerializer.Deserialize<List<DarkSoulsItem>>(json, GetJsonOptions());
            return list;
        }
        public static List<DarkSoulsItem> GetAllItems()
        {
            var results = new List<DarkSoulsItem>();

            results = results.Concat(GetConsumables()).ToList();
            results = results.Concat(GetKeyItems()).ToList();
            results = results.Concat(GetRings()).ToList();
            results = results.Concat(GetUpgradeMaterials()).ToList();
            results = results.Concat(GetEmbers()).ToList();
            results = results.Concat(GetSpells()).ToList();
            results = results.Concat(GetShields()).ToList();
            results = results.Concat(GetRangedWeapons()).ToList();
            results = results.Concat(GetSpellTools()).ToList();
            results = results.Concat(GetUsableItems()).ToList();
            results = results.Concat(GetMeleeWeapons()).ToList();
            results = results.Concat(GetArmor()).ToList();
            results = results.Concat(GetTraps()).ToList();
            results = results.Concat(GetDsrEventItems()).ToList();
            results = results.Concat(GetBonfireWarpItems()).ToList();
            results = results.Concat(GetProgressiveItems()).ToList();

            return results;
        }
        public static bool IsInGame()
        {
            if (getIngameTime() != 0)
                return true;
            return false;
        }        
        public static uint getIngameTime()
        {
            var baseB = AddressHelper.GetBaseBAddress();
            if (baseB != 0)
            {
                var next = OffsetPointer(baseB, 0xA4);
                return Memory.ReadUInt(next);
            }
            return 0; 
        }
        public static string OpenEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string file = reader.ReadToEnd();
                return file;
            }
        }
        public static byte[] GetItemCommand()
        {
            byte[] x = new byte[] {
                0x41, 0xb9, 0x00, 0x00, 0x00, 0x00,       //mov         r9d,0x0          // amount
                0x41, 0xb8, 0x00, 0x00, 0x00, 0x00,       //mov         r8d,0x0          // itemid
                0xba, 0x00, 0x00, 0x00, 0x00,             //mov         edx,0x0          // category
                0x48, 0xa1, 0x30, 0xa5, 0xc8, 0x41, 0x01, //movabs      rax,[0x141c8a530]
                0x00, 0x00, 0x00,
                0x48, 0x85, 0xc0,                         //TEST        RAX,RAX
                0x74, 0x31,                               //JZ          0x31  (BadRaX)
                0x4c, 0x8b, 0x78, 0x10,                   //mov         r15,[rax+0x10]
                0x49, 0x8d, 0x8f, 0x80, 0x02, 0x00, 0x00, //lea         rcx,[r15+0x280]
                0x48, 0x83, 0xec, 0x38,                   //sub         rsp,0x38
                0x49, 0xbe, 0xe0, 0x79, 0x74, 0x40, 0x01, //movabs      r14,0x1407479E0  // addItemToInventory()
                0x00, 0x00, 0x00,
                0x41, 0xff, 0xd6,                         //call        r14
                0x48, 0x83, 0xc4, 0x38,                   //add         rsp,0x38

                0x48, 0xb8,                               //movabs rax,0x1234567812345678 // replace with resultArea
                0x78, 0x56, 0x34, 0x12,  // target operand -> result area (8 bytes)
                0x78, 0x56, 0x34, 0x12,
                0xc7, 0x00, 0x00, 0x00, 0x00, 0x00,        //mov DWORD PTR[rax],0x00000000
                0xc3,                                     //RET 
                // BadRAX:
                0x48, 0xb8,                               //movabs rax,0x1234567812345678 // replace with resultArea
                0x78, 0x56, 0x34, 0x12,  // target operand -> result area (8 bytes)
                0x78, 0x56, 0x34, 0x12,
                0xc7, 0x00, 0xff, 0xff, 0xff, 0xff,        //mov DWORD PTR[rax],0xffffffff
                0xc3,                                     //RET 

            };
            return x;
        }


        public static byte[] GetItemWithMessageCommand()
        {
            // could use additional validation
            // - check 0x141c891a8 / ItemGetMenuManImpl before the addItemToInventory,
            // - check 0x141c8a530 / GameDataMan for null before it is dereferenced
            byte[] x = new byte[] {
                0x41, 0xb9, 0x00, 0x00, 0x00, 0x00,       //mov         r9d,0x0          // amount
                0x41, 0xb8, 0x00, 0x00, 0x00, 0x00,       //mov         r8d,0x0          // itemid
                0xba, 0x00, 0x00, 0x00, 0x00,             //mov         edx,0x0          // category
                0x48, 0xa1, 0x30, 0xa5, 0xc8, 0x41, 0x01, //movabs      rax,[0x141c8a530]
                0x00, 0x00, 0x00,
                0x48, 0x85, 0xc0,                         //TEST        RAX,RAX
                0x0f, 0x84, 0xda, 0x00, 0x00, 0x00,       //JZ          +218 / 0xDA (BadRAX)
                0x4c, 0x8b, 0x78, 0x10,                   //mov         r15,[rax+0x10]
                0x49, 0x8d, 0x8f, 0x80, 0x02, 0x00, 0x00, //lea         rcx,[r15+0x280]
                0x48, 0x83, 0xec, 0x38,                   //sub         rsp,0x38
                0x49, 0xbe, 0xe0, 0x79, 0x74, 0x40, 0x01, //movabs      r14,0x1407479E0  // addItemToInventory()
                0x00, 0x00, 0x00,
                0x41, 0xff, 0xd6,                         //call        r14
                0x48, 0x83, 0xc4, 0x38,                   //add         rsp,0x38

                0x41, 0xb9, 0x00, 0x00, 0x00, 0x00,       //mov         r9d,0x0          // amount
                0x41, 0xb8, 0x00, 0x00, 0x00, 0x00,       //mov         r8d,0x0          // itemid
                0xba, 0x00, 0x00, 0x00, 0x00,             //mov         edx,0x0          // category
                0x48, 0xb9, 0xa8, 0x91, 0xc8, 0x41, 0x01, //movabs      rcx,0x141c891a8 // ItemGetMenuMan 
                0x00, 0x00, 0x00,
                0x48, 0x8b, 0x09,                         //mov         rcx,QWORD PTR [rcx]
                0x48, 0x83, 0xec, 0x64,                   //sub         rsp,0x64

                0x40, 0x53,                               //PUSH        RBX
                0x4c, 0x8b, 0xd9,                         //MOV         R11,RCX
                0x33, 0xc0,                               //XOR         EAX,EAX
                0x48, 0x85, 0xc9,                         //TEST        RCX,RCX
                0x0f, 0x84, 0x99, 0x00, 0x00, 0x00,       //JZ          +153 / 0x99 (BadRCX)
                0x48, 0x8b, 0x49, 0x10,                   //MOV         RCX,qword ptr [RCX + 0x10]
                0x8b, 0xda,                               //MOV         EBX,EDX
                0x48, 0x85, 0xc9,                         //TEST        RCX,RCX
                0x74, 0x0c,                               //JZ          0x0c
                0x4c, 0x8b, 0x11,                         //MOV         R10,qword ptr [RCX]
                0x48, 0x8b, 0xc1,                         //MOV         RAX,RCX
                0x4d, 0x89, 0x53, 0x10,                   //MOV         qword ptr [R11 + 0x10],R10
                0xeb, 0x34,                               //JMP         0x34
                0x49, 0x8b, 0x4b, 0x08,                   //MOV         RCX,qword ptr [R11 + 0x8]
                0x48, 0x85, 0xc9,                         //TEST        RCX,RCX
                0x74, 0x42,                               //JZ          0x42
                0x48, 0x8b, 0xc1,                         //MOV         RAX,RCX
                0x48, 0x8b, 0x09,                         //MOV         RCX,qword ptr [RCX]
                0x48, 0x85, 0xc9,                         //TEST        RCX,RCX
                0x74, 0x18,                               //JZ          0x18
                0x48, 0x8b, 0xd0,                         //MOV         RDX,RAX
                0x48, 0x8b, 0xc1,                         //MOV         RAX,RCX
                0x48, 0x8b, 0x09,                         //MOV         RCX,qword ptr [RCX]
                0x48, 0x85, 0xc9,                         //TEST        RCX,RCX
                0x75, 0xf2,                               //JNZ         0xf2
                0x48, 0x85, 0xd2,                         //TEST        RDX,RDX
                0x74, 0x05,                               //JZ          0x8
                0x48, 0x89, 0x0a,                         //MOV         qword ptr [RDX],RCX
                0xeb, 0x08,                               //JMP         0x8
                0x49, 0xc7, 0x43,                         //MOV         qword ptr [R11 + 0x8],0x0
                0x08, 0x00, 0x00,
                0x00, 0x00,
                0x48, 0x85, 0xc0,                         //TEST        RAX,RAX
                0x74, 0x12,                               //JZ          0x12
                0x48, 0xc7, 0x00,                         //MOV         qword ptr [RAX],0x0
                0x00, 0x00, 0x00, 0x00,
                0x89, 0x58, 0x08,                         //MOV         dword ptr [RAX + 0x8],EBX
                0x44, 0x89, 0x40, 0x0c,                   //MOV         dword ptr [RAX + 0xc],R8D
                0x44, 0x89, 0x48, 0x10,                   //MOV         dword ptr [RAX + 0x10],R9D
                0x49, 0x8b, 0x4b, 0x08,                   //MOV         RCX,qword ptr [R11 + 0x8]
                0x48, 0x89, 0x08,                         //MOV         qword ptr [RAX],RCX
                0xb9, 0x2c, 0x01,                         //MOV         ECX,0x12c
                0x00, 0x00,
                0x49, 0x89, 0x43, 0x08,                   //MOV         qword ptr [R11 + 0x8],RAX
                0x5b,                                     //POP         RBX
                
                0x48, 0x83, 0xc4, 0x64,                   //add         rsp,0x64
                0x48, 0xb8,                               //movabs rax,0x1234567812345678
                0x78, 0x56, 0x34, 0x12,
                0x78, 0x56, 0x34, 0x12,
                0xc7, 0x00, 0x00, 0x00, 0x00, 0x00,       //mov DWORD PTR[rax],0x00000000
                0xc3,                                     //RET 
                // BadRAX:
                0x48, 0xb8,                               //movabs rax,0x1234567812345678 // replace with resultArea
                0x78, 0x56, 0x34, 0x12,  // target operand -> result area (8 bytes)
                0x78, 0x56, 0x34, 0x12,
                0xc7, 0x00, 0xff, 0xff, 0xff, 0xff,        //mov DWORD PTR[rax],0xffffffff
                0xc3,                                     //RET 
                // BadRCX:
                0x5b,                                     //POP         RBX
                0x48, 0x83, 0xc4, 0x64,                   //add         rsp,0x64
                0x48, 0xb8,                               //movabs rax,0x1234567812345678
                0x78, 0x56, 0x34, 0x12,
                0x78, 0x56, 0x34, 0x12,
                0xc7, 0x00, 0x00, 0x00, 0x00, 0x00,       //mov DWORD PTR[rax],0x00000000
                0xc3,                                     //RET 

            };
            return x;
        }
        /*  ----------Code To Emulate--------------

    mov rcx,[BaseB]
    mov edx,1
    sub rsp,38
    call 0x1404867e0
    add rsp,38
    ret

*/
        /*  ----------Homeward Bone injected ASM--------------
            0:  48 c7 c1 78 56 34 12    mov    rcx,0x12345678
            7:  00
            8:  ba 01 00 00 00          mov    edx,0x1
            d:  49 be e0 67 48 40 01    movabs r14,0x1404867e0
            14: 00 00 00
            17: 48 83 ec 38             sub    rsp,0x38
            1b: 41 ff d6                call   r14
            1e: 48 83 c4 38             add    rsp,0x38
            22: c3                      ret 
         */
        public static byte[] HomewardBone()
        {
            byte[] x = new byte[] {
                    0x48, 0xC7, 0xC1, 0x78, 0x56, 0x34, 0x12,
                    0xBA, 0x01, 0x00, 0x00, 0x00,
                    0x49, 0xBE, 0xE0, 0x67, 0x48, 0x40, 0x01, 0x00, 0x00, 0x00,
                    0x48, 0x83, 0xEC, 0x38,
                    0x41, 0xFF, 0xD6,
                    0x48, 0x83, 0xC4, 0x38,
                    0xC3

            };

            return x;
        }
        protected internal static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions();
        }
        
        internal static byte GetSavedSaveId()
        {
            ulong address = AddressHelper.GetSaveIdAddress();
            return Memory.ReadByte(address);
        }

        internal static void SetSavedSaveId(byte newsaveid)
        {
            ulong address = AddressHelper.GetSaveIdAddress();
            Memory.Write(address, newsaveid);
        }
        internal static ushort GetSavedSeedHash()
        {
            ulong address = AddressHelper.GetSaveSeedAddress();
            return Memory.ReadUShort(address);
        }
        internal static void SetSavedSeedHash(ushort seedhash)
        {
            ulong address = AddressHelper.GetSaveSeedAddress();
            Memory.Write(address, seedhash);
        }
        internal static ushort HashSeed(string seed)
        {
            uint result = 31719121;
            foreach (char c in seed)
            {
                result ^= c;
                result = UInt32.RotateLeft(result, 11);
            }
            return (ushort)(result % 65000 + 1); // ensure it is a short, but non-zero.
        }
        internal static ushort GetSavedSlot()
        {
            ulong address = AddressHelper.GetSaveSlotAddress();
            return Memory.ReadUShort(address);
        }
        internal static void SetSavedSlot(ushort slot)
        {
            ulong address = AddressHelper.GetSaveSlotAddress();
            Memory.Write(address, slot);
        }
        
        internal static bool CanPopupItems()
        {
            ulong ItemGetMenuMan = Memory.ReadULong(0x141c891a8);
            if (ItemGetMenuMan == 0) 
                return false;

            ulong unused_node_queue = Memory.ReadULong(ItemGetMenuMan + 0x10);
            ulong valid_node_queue = Memory.ReadULong(ItemGetMenuMan + 0x8);
            if (unused_node_queue == 0 && valid_node_queue == 0)
                return false;

            return true;
        }

        internal static void TeleportIfPlayerHasKilled(string tpCommand, string bossName, string bonfireName, Enums.Bonfires bonfireId)
        {
            // Check if player has killed the boss. If so, teleport them to the Oolacile Sanctuary.
            var lotFlags = LocationHelper.GetBossFlags();
            var baseAddress = AddressHelper.GetEventFlagsOffset();
            BossFlag bossFlag = lotFlags.Find((x) => x.Name.Contains(bossName));
            var bossLoc = new Location
            {
                Name = bossFlag.Name,
                Address = baseAddress + AddressHelper.GetEventFlagAddrAndByteOffset(bossFlag.Flag).Item1,
                AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(bossFlag.Flag).Item2,
                Id = bossFlag.Id,
            };
            if (bossLoc.Check())
            {
                if (SetLastBonfireTo(bonfireId))
                {
                    App.HomewardBoneCommand();
                    Log.Logger.Information($"DLC teleport - player sent to {bonfireName}.");
                    App.Client.AddOverlayMessage($"DLC teleport - player sent to {bonfireName}.");
                }
            }
            else
            {
                Log.Logger.Information($"{tpCommand} teleport failed - player has not killed {bossName}.");
                App.Client.AddOverlayMessage($"{tpCommand} teleport failed - player has not killed {bossName}.");
            }
        }

        /*  ----------Code To Emulate--------------
            sub rsp,0x30
            mov rcx,0
            mov edx,[entity] (12345678)
            mov r8d,[animation] (1234)
            xor r9b,r9b
            mov byte ptr [RSP + 20],0x0
            movabs r14,0x1404867e0
            call r14
            add rsp,0x30
            ret
        */
        /*  generated machine code/asm
            0:  48 83 ec 30             sub    rsp,0x30
            4:  48 c7 c1 00 00 00 00    mov    rcx,0x0
            b:  ba 00 00 00 00          mov    edx,0x0
            10: 41 b8 00 00 00 00       mov    r8d,0x0
            16: 45 30 c9                xor    r9b,r9b
            19: c6 44 24 14 00          mov    BYTE PTR [rsp+0x14],0x0
            1e: 49 be e0 67 48 40 01    movabs r14,0x140480700
            25: 00 00 00 
            28: 41 ff d6                call   r14
            2b: 48 83 c4 30             add    rsp,0x30
            2f: c3                      ret
         */
        internal static byte[] PlayAnimation(int entity, int animation)
        {
            byte[] x = new byte[] {
                0x48, 0x83, 0xec, 0x38,                   // sub    rsp,0x38
                0x48, 0xc7, 0xc1, 0x00, 0x00, 0x00, 0x00, // mov    rcx,0x0
                0xba, 0x00, 0x00, 0x00, 0x00,             // mov    edx,0x0 <- fill with entity
                0x41, 0xb8, 0x00, 0x00, 0x00, 0x00,       // mov    r8d,0x0 < - fill with animation
                0x45, 0x30, 0xc9,                         // xor    r9b,r9b
                0xc6, 0x44, 0x24, 0x14, 0x00,             // mov    BYTE PTR [rsp+0x14],0x0
                // experimental
                0x48, 0xb9, 0xb0, 0xb1, 0xc7, 0x41, 0x01, 0x00, 0x00, 0x00, // movabs RCX,0x141c7b1b0 (DbgEvent_Global_obj)
                0x48, 0x8b, 0x09,                                     // mov rcx, qword ptr [rcx]
                //
                0x49, 0xbe, 0x00, 0x07, 0x48, 0x40, 0x01, // movabs r14,0x140480700
                0x00, 0x00, 0x00,
                0x41, 0xff, 0xd6,                         // call   r14
                0x48, 0x83, 0xc4, 0x38,                   // add    rsp,0x38
                0xc3,                                     // ret
            };
            Array.Copy(BitConverter.GetBytes(entity), 0, x, 12, sizeof(int));
            Array.Copy(BitConverter.GetBytes(animation), 0, x, 18, sizeof(int));

            return x;
        }
        /*  ----------Code To Emulate--------------
            // This successfully:
            //   A) removes the item bag sfx for the location, and
            //   B) moves its interact position (at 0x30[float[3]]) to [0,-20,0] (could instead have toggled off item bag + 0x48 [x40]), 
            //  but it does NOT:
            //   1) set item pickup flag 
            //   2) open chests (or set chest flag open)
             mov rdx,0
             movabs rcx,0x141c7a0c8
             mov rcx,qword ptr[rcx]
             test rcx,rcx
             jz NOTFOUND

             mov rcx,qword ptr[rcx+0x48]

            LOOP:
             TEST rcx,rcx
             jz NOTFOUND
             cmp dword ptr [rcx + 0x28],edx
             je FOUND
             mov rcx,qword ptr[rcx+8]
             jmp LOOP
 
            FOUND:
             mov dword ptr [rcx+0x30],0x0
             mov dword ptr [rcx+0x34],0x0000A0C1
             mov dword ptr [rcx+0x38],0x0
             mov rdx,rcx
             movabs r14,0x1403f8df0
             call r14
             mov rax,0
             ret

            NOTFOUND:
             mov rax,4
             ret

        */
        /*  generated machine code/asm
            0:  48 c7 c2 00 00 00 00    mov    rdx,0x0
            7:  48 b9 c8 a0 c7 41 01    movabs rcx,0x141c7a0c8
            e:  00 00 00 
            11: 48 8b 09                mov    rcx,QWORD PTR [rcx]
            14: 48 85 c9                test   rcx,rcx
            17: 74 41                   je     5a <NOTFOUND>
            19: 48 8b 49 48             mov    rcx,QWORD PTR [rcx+0x48]

            000000000000001d <LOOP>:
            1d: 48 85 c9                test   rcx,rcx
            20: 74 38                   je     5a <NOTFOUND>
            22: 39 51 28                cmp    DWORD PTR [rcx+0x28],edx
            25: 74 06                   je     2d <FOUND>
            27: 48 8b 49 08             mov    rcx,QWORD PTR [rcx+0x8]
            2b: eb f0                   jmp    1d <LOOP>

            000000000000002d <FOUND>:
            2d: c7 41 30 00 00 00 00    mov    DWORD PTR [rcx+0x30],0x0
            34: c7 41 34 c1 a0 00 00    mov    DWORD PTR [rcx+0x34],0xa0c1
            3b: c7 41 38 00 00 00 00    mov    DWORD PTR [rcx+0x38],0x0
            42: 48 89 ca                mov    rdx,rcx
            45: 49 be f0 8d 3f 40 01    movabs r14,0x1403f8df0
            4c: 00 00 00 
            4f: 41 ff d6                call   r14
            52: 48 c7 c0 00 00 00 00    mov    rax,0x0
            59: c3                      ret

            000000000000005a <NOTFOUND>:
            5a: 48 c7 c0 04 00 00 00    mov    rax,0x4
            61: c3                      ret
         */
        internal static byte[] RemoveItemBag(int flag)
        {
            byte[] x = new byte[] { 
                0x48, 0xC7, 0xC2, 0x00, 0x00, 0x00, 0x00,
                0x48, 0xB9, 0xC8, 0xA0, 0xC7, 0x41, 0x01, 0x00, 0x00, 0x00, 
                0x48, 0x8B, 0x09, 

                0x48, 0x85, 0xC9, 
                0x74, 0x41, 
                0x48, 0x8B, 0x49, 0x48, 
                0x48, 0x85, 0xC9, 
                0x74, 0x38, 
                0x39, 0x51, 0x28, 
                0x74, 0x06, 
                0x48, 0x8B, 0x49, 0x08, 
                0xEB, 0xF0,

                0xc7, 0x41, 0x30, 0x00, 0x00, 0x00, 0x00,
                0xc7, 0x41, 0x34, 0xc1, 0xa0, 0x00, 0x00,
                0xc7, 0x41, 0x38, 0x00, 0x00, 0x00, 0x00,
                0x48, 0x89, 0xCA, 
                0x49, 0xBE, 0xf0, 0x8d, 0x3F, 0x40, 0x01, 0x00, 0x00, 0x00, 
                0x41, 0xFF, 0xD6, 
                0x48, 0xC7, 0xC0, 0x02, 0x00, 0x00, 0x00, 
                0xC3, 
                
                0x48, 0xC7, 0xC0, 0x04, 0x00, 0x00, 0x00, 
                0xC3 };

            // debugging: jmp to 1403f8e7e
            //                 0x49, 0xBE, 0x7e, 0x8e, 0x3F, 0x40, 0x01, 0x00, 0x00, 0x00, 
            //                 0x41, 0xFF, 0xD6, 

            Array.Copy(BitConverter.GetBytes(flag), 0, x, 3, sizeof(int));
            return x;
        }
    }
}
