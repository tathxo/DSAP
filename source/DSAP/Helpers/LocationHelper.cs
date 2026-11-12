using Archipelago.Core.Models;
using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace DSAP.Helpers
{
    internal class LocationHelper
    {
        #region Location Helpers
        private static List<DarkSoulsLocation> CachedItemLotLocations = null;
        private static List<DarkSoulsLocation> CachedExtraBossItemLotLocations = null;
        private static List<DarkSoulsLocation> CachedExtraItemLotLocations = null;
        
        public static List<DarkSoulsLocation> GetItemLotLocations()
        {
            if (CachedItemLotLocations != null)
                return CachedItemLotLocations;

            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetItemLotFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    DeliveredFlagId = lot.DeliveredFlagId,
                    Id = lot.Id,
                });
            }
            CachedItemLotLocations = locations;
            return locations;
        }
        public static List<DarkSoulsLocation> GetBossExtraItemLotLocations()
        {
            if (CachedExtraBossItemLotLocations != null)
                return CachedExtraBossItemLotLocations;

            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetBossExtraItemLotFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    DeliveredFlagId = lot.DeliveredFlagId,
                    Id = lot.Id,
                });
            }
            CachedExtraBossItemLotLocations = locations;
            return locations;
        }
        public static List<DarkSoulsLocation> GetExtraItemLotLocations()
        {
            if (CachedExtraItemLotLocations != null)
                return CachedExtraItemLotLocations;

            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetExtraItemLotFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    DeliveredFlagId = lot.DeliveredFlagId,
                    Id = lot.Id,
                });
            }
            CachedExtraItemLotLocations = locations;
            return locations;
        }
        
        public static List<DarkSoulsLocation> GetBossFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetBossFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    Id = lot.Id,
                });
            }
            return locations;
        }
        public static List<DarkSoulsLocation> GetBonfireFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var bonfireFlags = GetBonfireFlags().Where(x => x.Flag != 0);
            
            foreach (var bonfire in bonfireFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = bonfire.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(bonfire.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(bonfire.Flag).Item2,
                    Id = bonfire.Id
                });
            }
            return locations;
        }
        public static List<DarkSoulsLocation> GetDoorFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetDoorFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    Id = lot.Id,
                });
            }
            return locations;
        }
        public static List<DarkSoulsLocation> GetFogWallFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetFogWallFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    Id = lot.Id,
                });
            }
            return locations;
        }
        public static List<DarkSoulsLocation> GetShopLineupFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags = GetShopLineupFlags().Where(x => x.Flag != -1 && x.Id != 0); // ignore "infinite" slots
            var baseAddress = AddressHelper.GetEventFlagsOffset();
            foreach (var lot in lotFlags)
            {
                if (lot.Preflag == 0)
                {
                    locations.Add(new DarkSoulsLocation
                    {
                        Name = lot.Name,
                        Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                        AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                        Id = lot.Id,
                    });
                }
                else
                {
                    locations.Add(new DarkSoulsLocation
                    {
                        Name = lot.Name,
                        Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                        AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                        Address2 = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Preflag).Item1,
                        Address2Bit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Preflag).Item2,
                        CheckType = LocationCheckType.AND, // technically this should be a composite (to be compatible with the memory library, but this is easier for our purposes
                        Id = lot.Id,
                    });
                }

            }
            return locations;
        }
        public static List<DarkSoulsLocation> GetMiscFlagLocations()
        {
            List<DarkSoulsLocation> locations = new List<DarkSoulsLocation>();
            var lotFlags =  GetMiscFlags();
            foreach (var lot in lotFlags)
            {
                locations.Add(new DarkSoulsLocation
                {
                    Name = lot.Name,
                    Address = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item1,
                    AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(lot.Flag).Item2,
                    Id = lot.Id,
                });
            }
            return locations;
        }
        #endregion Location Helpers
        #region Flag Helpers
        public static bool ReadBonfireFlag(string name)
        {
            string result = "";
            var lotFlags = LocationHelper.GetBonfireFlagLocations();
            Location thisLoc = (Location)lotFlags.FirstOrDefault(x => x.Name == name);

            ulong Address = thisLoc.Address;
            int AddressBit = thisLoc.AddressBit;

            int wholebyte = Memory.ReadByte(Address);
            int bit = 0;
            if ((wholebyte & (1 << AddressBit)) != 0)
                bit = 1;
            Log.Logger.Debug($"Read Bonfire Flag for {name} at {Address}/0x{Address:X} = {wholebyte}, bit [{AddressBit}]={bit}");
            return bit == 1;
        }

        public static List<ItemLotFlag> GetItemLotFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.ItemLots.json");
            var list = JsonSerializer.Deserialize<List<ItemLotFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }

        public static List<ItemLotFlag> GetBossExtraItemLotFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.BossExtraItemLots.json");
            var list = JsonSerializer.Deserialize<List<ItemLotFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<ItemLotFlag> GetExtraItemLotFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.ExtraItemLots.json");
            var list = JsonSerializer.Deserialize<List<ItemLotFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        
        public static List<BossFlag> GetBossFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.BossFlags.json");
            var list = JsonSerializer.Deserialize<List<BossFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<EventFlag> GetBonfireFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.Bonfires.json");
            var list = JsonSerializer.Deserialize<List<EventFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<DoorFlag> GetDoorFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.Doors.json");
            var list = JsonSerializer.Deserialize<List<DoorFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<FogWallFlag> GetFogWallFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.DsrEvents.json");
            List<Enums.DsrEventType> fogwalltypes = [Enums.DsrEventType.FOGWALL, Enums.DsrEventType.BOSSFOGWALL, Enums.DsrEventType.EARLYFOGWALL];
            var list = JsonSerializer.Deserialize<List<DsrEvent>>(json, MiscHelper.GetJsonOptions()).Where(x => fogwalltypes.Contains(x.Type));
            List<FogWallFlag> newlist = list.Select(x => new FogWallFlag()
            {
                Name = x.Locname,
                Id = x.Locid, // apid of event location
                Flag = x.Flag
            }
            ).ToList();
            return newlist;
        }
        public static List<ShopLineupEntry> GetShopLineupFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.ShopLineups.json");
            var list = JsonSerializer.Deserialize<List<ShopLineupEntry>>(json, MiscHelper.GetJsonOptions());
            return list;
        }
        public static List<EventFlag> GetMiscFlags()
        {
            var json = MiscHelper.OpenEmbeddedResource("DSAP.Resources.MiscFlags.json");
            var list = JsonSerializer.Deserialize<List<EventFlag>>(json, MiscHelper.GetJsonOptions());
            return list;
        }

        public static ulong FlagToOffset(EventFlag flag)
        {
            var offset = AddressHelper.GetEventFlagAddrAndByteOffset(flag.Flag).Item1;
            return offset;
        }

        public static void SetEventFlag(int flagnum, byte newvalue)
        {
            var baseAddress = AddressHelper.GetEventFlagsOffset();
            Location newloc = new DarkSoulsLocation()
            {
                Address = baseAddress + AddressHelper.GetEventFlagAddrAndByteOffset(flagnum).Item1,
                AddressBit = AddressHelper.GetEventFlagAddrAndByteOffset(flagnum).Item2
            };
            Memory.WriteBit(newloc.Address, newloc.AddressBit, newvalue == 0 ? false : true);
            return;
        }
        #endregion
    }
}
