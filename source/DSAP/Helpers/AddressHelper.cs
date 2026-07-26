using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    public class AddressHelper
    {
        /* aka GameDataMan */
        public static AoBHelper BaseBAoB = new AoBHelper("BaseB",
                [0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x45, 0x33, 0xED, 0x48, 0x8B, 0xF1, 0x48, 0x85, 0xC0],
                "xxx????xxxxxxxxx", 3, 4);
        /* worlddataman? */
        public static AoBHelper BaseEAoB = new AoBHelper("BaseE",
                [0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x48, 0x8B, 0x88, 0x98, 0x0B, 0x00, 0x00, 0x8B, 0x41, 0x3C, 0xC3],
                "xxx????xxxxxxxxxxx", 3, 4);
        /* AKA "WorldChrManImp" */
        public static AoBHelper BaseXAoB = new AoBHelper("BaseX",
                [0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x48, 0x39, 0x48, 0x68, 0x0f, 0x94, 0xc0, 0xc3],
                "xxx????xxxxxxxx", 3, 4);
        /* aka 141c8adc0 */
        public static AoBHelper EmkAoB = new AoBHelper("EmkHead",
                [0x48, 0x89, 0x05, 0x00, 0x00, 0x00, 0x00, 0xeb, 0x0b, 0x48, 0xc7, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x8b, 0x5c, 0x24, 0x50],
                "xxx????xxxxx????xxxxxxxxx", 3, 4);
        public static AoBHelper SoloParamAob = new AoBHelper("SoloParam",
                [0x4C, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x48, 0x63, 0xC9, 0x48, 0x8D, 0x04, 0xC9],
                "xxx????xxxxxxx", 3, 4);
        
        public static AoBHelper EventFlagsAoB = new AoBHelper("EventFlags",
                [0x48, 0x8B, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x99, 0x33, 0xC2, 0x45, 0x33, 0xC0, 0x2B, 0xC2, 0x8D, 0x50, 0xF6],
                "xxx????xxxxxxxxxxx", 3, 4);

        public static ulong GetBaseAddress()
        {
            var address = Memory.GetBaseAddress("DarkSoulsRemastered");
            if (address == 0)
            {
                Log.Logger.Debug("Could not find Base Address");
            }
            return (ulong)address;
        }
        public static ulong GetBaseAOffset()
        {
            var baseAddress = GetBaseAddress();
            byte[] pattern = { 0x8B, 0x76, 0x0C, 0x89, 0x35, 0x00, 0x00, 0x00, 0x00, 0x33, 0xC0 };
            string mask = "xxxxx????xx";
            IntPtr getBaseAAddress = Memory.FindSignature((nint)baseAddress, 0x1000000, pattern, mask);

            int offset = BitConverter.ToInt32(Memory.ReadByteArray((ulong)(getBaseAAddress + 3), 4), 0);
            IntPtr baseAAddress = getBaseAAddress + offset + 7;

            return (ulong)baseAAddress;
        }

        public static ulong GetBaseBAddress()
        {
            return (ulong)BaseBAoB.Address;
        }
        public static ulong GetBaseCOffset()
        {
            var baseAddress = GetBaseAddress();
            byte[] pattern = { 0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x28, 0x01, 0x66, 0x0F, 0x7F, 0x80, 0x00, 0x00, 0x00, 0x00, 0xC6, 0x80 };
            string mask = "xxx????xxxxxxx??xxxx";
            IntPtr getPFAddress = Memory.FindSignature((nint)baseAddress, 0x1000000, pattern, mask);

            int offset = BitConverter.ToInt32(Memory.ReadByteArray((ulong)(getPFAddress + 3), 4), 0);
            IntPtr progressionFlagsAddress = getPFAddress + offset + 7;

            return (ulong)progressionFlagsAddress;
        }
        public static ulong GetBaseEAddress()
        {
            IntPtr baseE = BaseEAoB.Address;
            return (ulong)baseE;

        }
        public static ulong GetBaseXAddress()
        {
            IntPtr baseX = BaseXAoB.Address;
            return (ulong)baseX;

        }
        public static ulong GetEmkHeadAddress()
        {
            IntPtr emkHeadPtr = EmkAoB.Address;
            return (ulong)emkHeadPtr;

        }
        public static ulong GetChrBaseClassOffset()
        {
            var baseAddress = GetBaseAddress();
            byte[] pattern = { 0x48, 0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x45, 0x33, 0xED, 0x48, 0x8B, 0xF1, 0x48, 0x85, 0xC0 };
            string mask = "xxx????xxxxxxxxx";
            IntPtr getCBCAddress = Memory.FindSignature((nint)baseAddress, 0x1000000, pattern, mask);

            int offset = BitConverter.ToInt32(Memory.ReadByteArray((ulong)(getCBCAddress + 3), 4), 0);
            IntPtr chrBaseClassAddress = getCBCAddress + offset + 7;

            return (ulong)chrBaseClassAddress;
        }
        public static ulong GetEventFlagsOffset()
        {
            IntPtr baseAddr = EventFlagsAoB.Address;
            if (baseAddr == IntPtr.Zero)
            {
                return 0;
            }
            return (ulong)(BitConverter.ToInt32(Memory.ReadFromPointer((ulong)baseAddr, 4, 1)));
        }
        public static (ulong, int) GetEventFlagAddrAndByteOffset(int eventFlag)
        {
            string idString = eventFlag.ToString("D8");
            int tail = Int32.Parse(idString.Substring(5, 3));

            uint fourByteMask = 0x80000000 >> (tail % 32);
            int significantByte = 0;
            if ((fourByteMask & 0x000000FF) != 0) significantByte = 0;
            else if ((fourByteMask & 0x0000FF00) != 0) significantByte = 1;
            else if ((fourByteMask & 0x00FF0000) != 0) significantByte = 2;
            else if ((fourByteMask & 0xFF000000) != 0) significantByte = 3;

            int bitMask = BitOperations.TrailingZeroCount((fourByteMask >> significantByte * 8) & 0xFF);
            var offset = GetPrimaryOffsetFromFlagId(idString);
            offset += GetSecondaryOffsetFromFlagId(idString);
            offset += Int32.Parse(idString.Substring(4, 1)) * 128;
            offset += (tail - (tail % 32)) / 8;

            ulong addressOffser = Convert.ToUInt64(offset + significantByte);

            return (addressOffser, bitMask);
        }

        private static int GetPrimaryOffsetFromFlagId(string eventFlag)
        {
            return eventFlag.Substring(0, 1) switch
            {
                "0" => 0x00000,
                "1" => 0x00500,
                "5" => 0x05F00,
                "6" => 0x0B900,
                "7" => 0x11300,
                _ => throw new ArgumentException("Cannot get primary offset for GetItemFlagId: " + eventFlag),
            };
        }
        private static string GetFlagId1DigitFromSlice(int slice)
        {
            if (slice < 0)
                throw new ArgumentException($"Cannot get flag id digit 1 for offset: {slice}");
            else if (slice == 0)
                return "0";
            else if (slice <= 18)
                return "1";
            else if (slice <= 2 * 18)
                return "5";
            else if (slice <= 3 * 18)
                return "6";
            else if (slice <= 4 * 18)
                return "7";
            else
                throw new ArgumentException($"Cannot get flag id digit 1 for offset: {slice}");
            return "?";
        }

        private static int GetSecondaryOffsetFromFlagId(string eventFlag)
        {
            var num = eventFlag.Substring(1, 3) switch
            {
                "000" => 00,
                "100" => 01,
                "101" => 02,
                "102" => 03,
                "110" => 04,
                "120" => 05,
                "121" => 06,
                "130" => 07,
                "131" => 08,
                "132" => 09,
                "140" => 10,
                "141" => 11,
                "150" => 12,
                "151" => 13,
                "160" => 14,
                "170" => 15,
                "180" => 16,
                "181" => 17,
                _ => throw new ArgumentException("Cannot get secondary offset for GetItemFlagId: " + eventFlag),
            };
            return num * 1280;
        }
        private static string GetFlagId234DigitFromSlice(int slice)
        {
            if (slice < 0)
                throw new ArgumentException($"Cannot get flag id digit 234 for offset: {slice}");
            else if (slice == 0)
                return "000";
            slice = ((slice - 1) % 18);
            var result = slice switch
            {
                00 => "000",
                01 => "100",
                02 => "101",
                03 => "102",
                04 => "110",
                05 => "120",
                06 => "121",
                07 => "130",
                08 => "131",
                09 => "132",
                10 => "140",
                11 => "141",
                12 => "150",
                13 => "151",
                14 => "160",
                15 => "170",
                16 => "180",
                17 => "181",
                _ => throw new ArgumentException("Cannot get flag id digit 234 for offset: {slice}"),
            };
            return result;
        }

        private static string GetFlagId234DigitFromByteAndBit(int bytenum, int bitnum)
        {
            int significantByte = bytenum % 4;
            int sigbit = (3 - significantByte) * 8 + bitnum;
            int fours = bytenum / 4;
            int flagnum = fours * 32 + sigbit;
            return (flagnum.ToString("D4"));
        }

        internal static byte[] ReadAllEventFlags()
        {
            if (!MiscHelper.IsInGame() || !App.SaveidSet)
            {
                return [];
            }
            byte[] flagsArray = new byte[0x500 + (0x500 * 18 * 4)]; // 0x500 * 18 = 0x5a00...aka the size of 1,5,6,7-prefixed flag zones
            var baseAddress = GetEventFlagsOffset();
            flagsArray = Memory.ReadByteArray(baseAddress, flagsArray.Length);
            if (!MiscHelper.IsInGame() || !App.SaveidSet)
            {
                return [];
            }

            return flagsArray;
        }
        internal static void StartEventFlagMonitor()
        {
            Log.Logger.Information("Monitoring Event Flags");
            Task.Run(async () =>
            {
                try
                {
                    byte[] oldFlags = [];
                    while (true)
                    {
                        if (!App.Client?.IsConnected ?? false == true)
                        {
                            Log.Logger.Error("Client disconnection detected - stopping eventflag monitor");
                            return;
                        }

                        byte[] flags = ReadAllEventFlags();
                        if (flags.Length != 0 && oldFlags.Length != 0)
                        {
                            if (App.DSOptions.LimitedShopItemShuffle)
                            {
                                CheckForHintTriggers(flags);
                            }
                            if (App.monitoringEventFlags)
                                DetectEventFlagDifferences(oldFlags, flags);
                        }
                        oldFlags = flags;
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Exception in event flags watcher: {ex.Message}\n{ex.InnerException}\n{ex.Source}");
                }
            });
        }
        internal static List<(int, List<long>)> hintTriggers = [];
        internal static void BuildHintTriggers(Dictionary<long, Archipelago.MultiClient.Net.Models.ScoutedItemInfo> scoutedLocationInfo, Archipelago.MultiClient.Net.Models.Hint[] hints)
        {
            // shopflags = missing locations
            var shopflags = LocationHelper.GetShopLineupFlags()
                .Where(x => App.Client.CurrentSession.Locations.AllMissingLocations.Contains(x.Id) // location is not found yet
                        && !hints.Select(y=>y.LocationId).Contains(x.Id)); // and location has not been hinted yet
            if (App.DSOptions.ShopHints == (uint)Enums.DSShopHints.off)
                return;
            else if (App.DSOptions.ShopHints == (uint)Enums.DSShopHints.progression)
                shopflags = shopflags.Where(x => scoutedLocationInfo.ContainsKey(x.Id) && ((scoutedLocationInfo[x.Id].Flags & ItemFlags.Advancement) != 0));
            else if (App.DSOptions.ShopHints == (uint)Enums.DSShopHints.progression_and_useful)
                shopflags = shopflags.Where(x => scoutedLocationInfo.ContainsKey(x.Id) && ((scoutedLocationInfo[x.Id].Flags & (ItemFlags.Advancement | ItemFlags.NeverExclude)) != 0));
            // else it's all, so don't modify the list.

            if (shopflags.Count() > 0)
            {
                // check hint flags
                hintTriggers = new List<(int, List<long>)>
                {
                    ( 71010000, shopflags.Where(x => x.Name.StartsWith("Andre")).Select(x => (long)x.Id).ToList() ), // Andre
                    ( 71020040, shopflags.Where(x => x.Name.StartsWith("Big Hat Logan:")).Select(x =>  (long)x.Id).ToList() ), // Big Hat Logan in Firelink
                    ( 71700007, shopflags.Where(x => x.Name.StartsWith("Big Hat Logan In Duke's Archives:")).Select(x =>  (long)x.Id).ToList() ), // Big Hat Logan in DA
                    ( 71500001, shopflags.Where(x => x.Name.StartsWith("Crestfallen Merchant")).Select(x =>  (long)x.Id).ToList() ), // Crestfallen Merchant
                    ( 71320006, shopflags.Where(x => x.Name.StartsWith("Domhnall of Zena:")).Select(x =>  (long)x.Id).ToList() ), // Domhnall of Zena - but not his master key
                    ( 71000030, shopflags.Where(x => x.Name.StartsWith("Female Undead Merchant")).Select(x =>  (long)x.Id).ToList() ), // Female Undead Merchant
                    ( 71510000, shopflags.Where(x => x.Name.StartsWith("Giant Blacksmith")).Select(x =>  (long)x.Id).ToList() ), // Giant Blacksmith
                    ( 71020058, shopflags.Where(x => x.Name.StartsWith("Griggs of Vinheim:")).Select(x =>  (long)x.Id).ToList() ), // Griggs of Vinheim
                    ( 71020062, shopflags.Where(x => x.Name.StartsWith("Griggs of Vinheim After Logan Leaves:")).Select(x =>  (long)x.Id).ToList() ), // Griggs of Vinheim After Logan leaves
                    ( 71210061, shopflags.Where(x => x.Name.StartsWith("Hawkeye Gough")).Select(x =>  (long)x.Id).ToList() ), // Hawkeye Gough
                    ( 71010070, shopflags.Where(x => x.Name.StartsWith("Male Undead Merchant")).Select(x =>  (long)x.Id).ToList() ), // Male Undead Merchant
                    ( 71210010, shopflags.Where(x => x.Name.StartsWith("Marvelous Chester")).Select(x =>  (long)x.Id).ToList() ), // Marvelous Chester if you say yes
                    ( 71210009, shopflags.Where(x => x.Name.StartsWith("Marvelous Chester")).Select(x =>  (long)x.Id).ToList() ), // Marvelous Chester if you say no
                    ( 71800056, shopflags.Where(x => x.Name.StartsWith("Oswald of Carim")).Select(x =>  (long)x.Id).ToList() ), // Oswald of Carim - if you're in the Way of White covenant
                    ( 71800057, shopflags.Where(x => x.Name.StartsWith("Oswald of Carim")).Select(x =>  (long)x.Id).ToList() ), // Oswald of Carim
                    ( 71810001, shopflags.Where(x => x.Name.StartsWith("Rickert of Vinheim")).Select(x =>  (long)x.Id).ToList() ), // Rickert of Vinheim
                    ( 11300210, shopflags.Where(x => x.Name.StartsWith("Vamos")).Select(x =>  (long)x.Id).ToList() ), // Vamos - upon landing there
                }.Where(x => x.Item2.Count > 0).ToList(); // trim already hinted
            }
            else
                hintTriggers = [];
        }

        private static void CheckForHintTriggers(byte[] flags)
        {
            foreach (var trigger in hintTriggers)
            {
                if (trigger.Item2.Count > 0)
                {
                    var (shopbyte, shopbit) = AddressHelper.GetEventFlagAddrAndByteOffset(trigger.Item1);
                    if (flags[shopbyte] != 0)
                    {
                        //Log.Logger.Information($"flag byte for {trigger.Item1} at {shopbyte:x}:{shopbit} = {flags[shopbyte]:x}");
                    }
                    if (((flags[shopbyte] >> shopbit) & 0x01) == 0x01)
                    {
                        long[] plist = trigger.Item2.ToArray();
                        App.Client.CurrentSession.Hints.CreateHints(HintStatus.Unspecified, plist);
                        trigger.Item2.Clear();
                    }
                }
            }
        }

        private static void DetectEventFlagDifferences(byte[] oldFlags, byte[] newFlags)
        {
            for (int i = 0; i < (1 + 18 * 4); i++)
            {
                // each chunk is 10 "x000-x999" sets of flags, across 128 or 0x80 
                // compare in 0x500 sized chunks
                var oldSlice = oldFlags.AsSpan().Slice(i * 0x500, 0x500);
                var newSlice = newFlags.AsSpan().Slice(i * 0x500, 0x500);
                if (!oldSlice.SequenceEqual(newSlice))
                {
                    //Log.Logger.Information($"event flag slices changed at slice {i}");
                    for (int j = 0; j < 0x500; j++)
                    {   
                        if (oldSlice[j] != newSlice[j])
                        {
                            //Log.Logger.Information($"byte changed at byte {j}");
                            byte oldbyte = oldSlice[j];
                            byte newbyte = newSlice[j];
                            int changebyte = ((int)oldbyte ^ (int)newbyte) & 0x000000FF;
                            for (int k = 0; k < 8; k++)
                            {
                                int changebit = (changebyte >> (7 - k)) & 0x01;
                                if (changebit != 0)
                                {
                                    int oldbit = (oldbyte >> (7 - k)) & 0x01;
                                    int newbit = (newbyte >> (7 - k)) & 0x01;
                                    var flag = GetFlagIdFromOffset(i, j, k);
                                    var currtime = DateTime.Now;
                                    Log.Logger.Information($"{currtime.TimeOfDay}: Flag {flag} changed: {oldbit} -> {newbit}");
                                    //Log.Logger.Information($"Slice {i}, byte {j}, bit {k}, flag {flag} changed: {oldbit} -> {newbit}");

                                }
                            }
                        }
                    }
                }
            }
        }
        internal static string GetFlagIdFromOffset(int slice, int bytenum, int bitnum)
        {
            var d1 = GetFlagId1DigitFromSlice(slice);
            var d234 = GetFlagId234DigitFromSlice(slice);
            var d5678 = GetFlagId234DigitFromByteAndBit(bytenum, bitnum);

            return $"{d1}{d234}{d5678}";
        }
        internal static ulong GetPlayerHPAddress()
        {
            var baseB = GetBaseBAddress();
            var next = MiscHelper.OffsetPointer(baseB, 0x10);
            var pointer = Memory.ReadULong(next);
            next = MiscHelper.OffsetPointer(pointer, 0x14);
            return next;
        }
        /// <summary>
        /// Get the HP address to which writing will actually update the player's HP (for deathlink).
        /// </summary>
        /// <returns>The address, or 0 if any pointer value along the chain was 0.</returns>
        internal static ulong GetPlayerWritableHPAddress()
        {
            var baseX = GetBaseXAddress();
            if (baseX != 0)
            {
                var next = MiscHelper.OffsetPointer(baseX, 0x68);
                var pointer = Memory.ReadULong(next);
                if (pointer != 0)
                {
                    next = MiscHelper.OffsetPointer(pointer, 0x3e8);
                    return next;
                }
            }
            return 0;
        }
        public static ulong GetItemLotParamOffset()
        {
            var foo = SoloParamAob.Address;
            Log.Logger.Verbose($"solo param location {foo:X}");
            var next = MiscHelper.OffsetPointer(((ulong)foo), 0x570);
            var foo2 = Memory.ReadULong(next);
            next = MiscHelper.OffsetPointer(foo2, 0x38);
            var foo3 = Memory.ReadULong(next);
            return foo3;
        }
        private static ulong GetBonfireOffset()
        {
            var baseAddress = GetEventFlagsOffset();
            var baseBonfire = MiscHelper.OffsetPointer(baseAddress, 0x5B);
            return baseBonfire;
        }
        // Eventflag   Offset
        // 960-967   = 123
        // 968-975   = 122
        // 976-983   = 121
        // 984-991   = 120
        // 992-999   = 127
        // 1000-1007 = 131
        // 1008-1015 = 130
        // 1016-1023 = 129
        // 1024-1031 = 128
        // -> 3 bytes free, offset 124-126. Use [960]+1-2 for seed hash, [960]+3 for SaveId.
        // This gap happens again every 1000 flags (until 9k), for each map's flags, in each category of flags
        // -> use [1960]+1-3 for slot id
        public static ulong GetSaveIdAddress()
        {
            var initoff = AddressHelper.GetEventFlagsOffset();
            int flag = 960;
            var off = AddressHelper.GetEventFlagAddrAndByteOffset(flag).Item1 + 3; // 3rd byte after this one
            // here we have 3 bytes of memory available.
            Log.Logger.Debug($"saveid address = {(off + initoff):X}");
            return off + initoff;
        }
        public static ulong GetSaveSeedAddress()
        {
            var initoff = AddressHelper.GetEventFlagsOffset();
            int flag = 960;
            var off = AddressHelper.GetEventFlagAddrAndByteOffset(flag).Item1 + 1; // 1st and 2nd byte after this one
            // here we have 3 bytes of memory available.
            Log.Logger.Debug($"Seed address = {(off + initoff):X}");
            return off + initoff;
        }
        public static ulong GetSaveSlotAddress()
        {
            var initoff = AddressHelper.GetEventFlagsOffset();
            int flag = 1960;
            var off = AddressHelper.GetEventFlagAddrAndByteOffset(flag).Item1 + 1; // Up to 3 bytes
            // here we have 3 bytes of memory available.
            Log.Logger.Debug($"Slot address = {(off + initoff):X}");
            return off + initoff;
        }

    }
}
