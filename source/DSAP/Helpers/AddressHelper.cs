using Archipelago.Core;
using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using DSAP.Models;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static DSAP.Enums;

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

        private static string GetFlagId5678DigitFromByteAndBit(int bytenum, int bitnum)
        {
            int thous = bytenum / 128;
            int rem_bytenum = bytenum % 128;
            int significantByte = rem_bytenum % 4;
            int sigbit = (3 - significantByte) * 8 + bitnum;
            int fours = rem_bytenum / 4;
            int flagnum = (1000 * thous) + fours * 32 + sigbit;
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
            Log.Logger.Debug("Event Flag Monitor started");
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
                            MapInfo mapInfo = MapHelper.GetMapAndXyzCoords();
                            if (App.DSOptions.LimitedShopItemShuffle)
                            {
                                CheckForHintTriggers(flags, mapInfo);
                                ShopSafety(flags);
                            }
                            if (App.DSOptions.LizardShuffle)
                            {
                                GhLizardSafety(flags);
                            }
                            PisacaSafety(flags);
                            PWDAWarpSafety(flags);
                            CollectSafety(flags);
                            BonfireInjectorHelper.PollBonfires(flags);
                            PollBosses(flags);
                            if (App.monitoringEventFlags)
                                DetectEventFlagDifferences(oldFlags, flags);
                        }
                        else
                        {
                            cached_AllLocationsChecked_count = -1;
                            BonfireInjectorHelper.ResetKnownBonfires();
                            cached_local_boss_pflags = 0;
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
        internal static void StartDisconnectedEventFlagMonitor()
        {
            Log.Logger.Information("Monitoring Event Flags");
            Task.Run(async () =>
            {
                try
                {
                    App.SaveidSet = true;
                    byte[] oldFlags = [];
                    while (true)
                    {
                        byte[] flags = ReadAllEventFlags();
                        if (flags.Length != 0 && oldFlags.Length != 0)
                        {
                            GhLizardSafety(flags);
                            ShopSafety(flags);
                            PisacaSafety(flags);
                            PWDAWarpSafety(flags);
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
        internal static List<ShopHintTrigger> hintTriggers = [];
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
                MapPoi domhnallDepthPosition = new MapPoi((float)-205.654, (float)-95.177, (float)-21.164);
                MapPoi domhnallAqueductPosition = new MapPoi((float)-39.53, (float)-50.000, (float)-0.860);
                // check hint flags
                hintTriggers = new List<ShopHintTrigger>
                {
                    new ShopHintTrigger(71010000, shopflags, "Andre"), // Andre
                    new ShopHintTrigger(71020040, shopflags, "Big Hat Logan:"), // Big Hat Logan in Firelink
                    new ShopHintTrigger(71700007, shopflags, "Big Hat Logan In Duke's Archives:"), // Big Hat Logan in DA
                    new ShopHintTrigger(71500001, shopflags, "Crestfallen Merchant"), // Crestfallen Merchant
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena:"), // Domhnall of Zena - but not his master key
                    new ShopHintTrigger(71000030, shopflags, "Female Undead Merchant"), // Female Undead Merchant
                    new ShopHintTrigger(71510000, shopflags, "Giant Blacksmith"), // Giant Blacksmith
                    new ShopHintTrigger(71020058, shopflags, "Griggs of Vinheim:"), // Griggs of Vinheim
                    new ShopHintTrigger(71020062, shopflags, "Griggs of Vinheim After Logan Leaves:"), // Griggs of Vinheim After Logan leaves
                    new ShopHintTrigger(71210061, shopflags, "Hawkeye Gough"), // Hawkeye Gough
                    new ShopHintTrigger(71000022, shopflags, "Laurentius of the Great Swamp"), // Laurentius of the Great Swamp
                    new ShopHintTrigger(71010070, shopflags, "Male Undead Merchant"), // Male Undead Merchant
                    new ShopHintTrigger(71210010, shopflags, "Marvelous Chester"), // Marvelous Chester if you say yes
                    new ShopHintTrigger(71210009, shopflags, "Marvelous Chester"), // Marvelous Chester if you say no
                    new ShopHintTrigger(71800056, shopflags, "Oswald of Carim"), // Oswald of Carim - if you're in the Way of White covenant
                    new ShopHintTrigger(71800057, shopflags, "Oswald of Carim"), // Oswald of Carim
                    new ShopHintTrigger(71300091, shopflags, "Petrus of Thorolund"), // Petrus of Thorolund
                    new ShopHintTrigger(71810001, shopflags, "Rickert of Vinheim"), // Rickert of Vinheim
                    new ShopHintTrigger(11300210, shopflags, "Vamos"), // Vamos - upon landing there
                    // also add Domhnall positional hints
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena Under Aqueduct: Master Key", [(5, domhnallAqueductPosition, [1431])]), // Master Key only under aquedeuct
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena After Iron Golem:", [(5, domhnallDepthPosition, [11500001]), (5, domhnallAqueductPosition, [11500001, 1431])]), // after Iron Golem, in depth or aqueduct
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena After O+S:", [(5, domhnallDepthPosition, [61323998]), (5, domhnallAqueductPosition, [61323998, 1431])]), // after O+S
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena After Gwyndolin:", [(5, domhnallDepthPosition,[11510900]),(5, domhnallAqueductPosition, [11510900, 1431])]), // after Gwyndolin
                    new ShopHintTrigger(71320006, shopflags, "Domhnall of Zena Under Aqueduct After Artorias:", [(5, domhnallAqueductPosition, [11210001, 1431])]), // After Artorias, only appears Under Aqueduct
                }.Where(x => x.HintLocs.Count > 0).ToList(); // trim already hinted
            }
            else
                hintTriggers = [];
        }

        private static void CheckForHintTriggers(byte[] flags, MapInfo mapInfo)
        {
            foreach (var trigger in hintTriggers)
            {
                if (trigger.HintLocs.Count > 0)
                {
                    if (isFlagOnInBuffer(flags, trigger.ConditionFlag))
                    {
                        // If there are no position conditions, or 1 position condition is satisfied, send the hints
                        if (trigger.PositionConditionList.Count == 0 || 
                            trigger.PositionConditionList.Any(condition =>
                            {
                                if (condition.flagList.All(y => isFlagOnInBuffer(flags, y)))
                                {
                                    if (Math.Pow((mapInfo.X - condition.poi.X), 2) + Math.Pow((mapInfo.Y - condition.poi.Y), 2) + Math.Pow((mapInfo.Z - condition.poi.Z), 2) < Math.Pow(condition.proximity, 2))
                                        return true;
                                }
                                return false;
                            }))
                        {
                            long[] plist = trigger.HintLocs.ToArray();
                            App.Client.CurrentSession.Hints.CreateHints(HintStatus.Unspecified, plist);
                            trigger.HintLocs.Clear();
                        }
                    }
                }
            }
        }
        public static bool isFlagOnInBuffer(byte[] flagsBuffer, int flagnum)
        {
            var (flagbyte, flagbit) = AddressHelper.GetEventFlagAddrAndByteOffset(flagnum);
            if (((flagsBuffer[flagbyte] >> flagbit) & 0x01) == 0x01)
            {
                return true;
            }
            return false;
        }
        private static void ShopSafety(byte[] flags)
        {
            // shopflags = missing locations
            var shopflags = LocationHelper.GetShopLineupFlags()
                .Where(x => App.Client.CurrentSession.Locations.AllMissingLocations.Contains(x.Id)); // location is not found yet
            //var shopflags = LocationHelper.GetShopLineupFlags(); // for debugging
            if (isFlagOnInBuffer(flags, 11020103)) // laurentius "move on to BT" flag
            {
                // if there are still shop locs to buy from him, restore the flags
                if (shopflags.Where(x => x.Name.StartsWith("Laurentius of the Great Swamp")).Count() > 0)
                {
                    Log.Logger.Information("Shop locations still unchecked - Preventing Laurentius from moving on.");
                    // delay this for 500 ms so that any events can finish running (in case we checked flags before event processing completed)
                    Task.Run(() =>
                    {
                        Task.Delay(500);
                        // order of the flag changes below is important - otherwise the event that resets the flags can just re-run.
                        App.SetEventFlag(11020103, false); // turn off "Laurentius was told about Quelana" flag
                        App.SetEventFlag(11020575, false); // turn off "Laurentius moves on event ran" flag 
                        App.SetEventFlag(1256, false);  // turn off "Laurentius will go to blighttown"
                        App.SetEventFlag(1252, true);  // turn back on "Laurentius should be in firelink"
                    });
                }
            }
            // if O+S were killed, make both sets available for purchase, then set the "we've made both sets available" flag on.
            if (!isFlagOnInBuffer(flags, 61323998) && isFlagOnInBuffer(flags, 11510001))
            {
                var os_shop_flags = shopflags.Where(x => x.Name.StartsWith("Domhnall of Zena After O+S")).Select(x => x.Flag);
                if (os_shop_flags.Count() != 0)
                {
                    Log.Logger.Information("Making both O+S armor sets available for purchase at Domhnall.");
                    Task.Run(() =>
                    {
                        Task.Delay(500);
                        foreach (var flag in os_shop_flags)
                        {
                            App.SetEventFlag(flag, false);
                        }
                        App.SetEventFlag(61323998, true);
                    });
                }
            }
            // for Griggs, normally he moves on if you buy all his spells.
            // Since we added his catalyst as a check, he could move on without us getting all checks.
            // So we swapped the flagid 11027240 to something else, and check if both non-vanilla flags are set.
            // If so, set 11027240
            if (!isFlagOnInBuffer(flags, 11027240) && isFlagOnInBuffer(flags, 61322520) && isFlagOnInBuffer(flags, 61322640))
            {
                Log.Logger.Information("All Griggs item purchased - allowing him to move on.");
                Task.Run(() =>
                {
                    Task.Delay(500);
                    App.SetEventFlag(11027240, true);
                });
            }
        }
        private static void GhLizardSafety(byte[] flags) // based on 11320300 events
        {
            List<int> requiredLizardFlags = [];
            // Check the flags for each lizard event. 1 of 3 flags are randomly turned on for each lizard.
            // We could check "is the player in the GH map", instead.
            for (int lizard = 0; lizard < 10; lizard++)
            {
                bool flag1 = isFlagOnInBuffer(flags, 11325203 + 3 * lizard);
                bool flag2 = isFlagOnInBuffer(flags, 11325204 + 3 * lizard);
                bool flag3 = isFlagOnInBuffer(flags, 11325205 + 3 * lizard);
                if ((flag2 || flag3) && !(flag1))
                {
                    requiredLizardFlags.Add(11325203 + 3 * lizard); // lizard on flag
                }
            }

            if (requiredLizardFlags.Count() > 0)
            {
                Log.Logger.Information("Great Hollow Lizard spawning forced.");
                // delay this for 500 ms so that any events can finish running (in case we checked flags before event processing completed)
                Task.Run(() =>
                {
                    Task.Delay(500);
                    for (int lizard = 0; lizard < 10; lizard++)
                    {
                        App.SetEventFlag(11325203 + 3 * lizard, true);
                    }
                });
            }
        }
        private static void PisacaSafety(byte[] flags) // based on 11705101 event
        {
            bool eventRan = isFlagOnInBuffer(flags, 11700133); // prison break event "ran" (persistent flag)
            bool cutscenePlayed = isFlagOnInBuffer(flags, 11700002); // cutscene played and pisaca gate opened
            bool cellKeyLooted = isFlagOnInBuffer(flags, 51700990); // cell key looted
            if (eventRan && cellKeyLooted && (!cutscenePlayed))
            {
                Log.Logger.Information("Reseting DA Prison Jailbreak flags.");
                // delay this for 500 ms so that any events can finish running (in case we checked flags before event processing completed)
                Task.Run(() =>
                {
                    Task.Delay(500);
                    App.SetEventFlag(11700133, false); // reset "prison break event ran" (persistent)
                    App.SetEventFlag(11705101, false); // reset "prison break event ran" (temporary)
                });
            }
        }
        public static bool added_warping_emk = false;
        private static void PWDAWarpSafety(byte[] flags) // based on 11705101 event
        {
            if (!added_warping_emk && !App.EmkControllers.Any(x => x.Eventid == 706)) // if not yet added, add the emk.
            {
                var newemk = new EmkController("Warping", "none", Enums.DsrEventType.GENERIC, 706, 0, 0);
                newemk.MapId3 = 170;
                App.EmkControllers.Add(newemk);
                added_warping_emk = true;
            }

            bool hasLordvessel = isFlagOnInBuffer(flags, 710); // "got lordvessel" flag
            bool canWarp = isFlagOnInBuffer(flags, 706); // "can warp" flag (turned off in PW / DA prison)
            if (hasLordvessel && !canWarp)
            {
                Log.Logger.Information("Removed warp-lock in DA prison / Painted World");
                // delay this for 500 ms so that any events can finish running (in case we checked flags before event processing completed)
                Task.Run(() =>
                {
                    Task.Delay(500);
                    App.SetEventFlag(706, true); // reset "can warp" to true
                });
            }
        }

        // for "collect" and for seamless co-op (and for other saves) items can be collected from the world "for you".
        // requires more testing for grouped items and chained items (e.g. armor sets, armor+weapon lots, etc)
        static int cached_AllLocationsChecked_count = 0;
        private static void CollectSafety(byte[] flags)
        {
            var checkedLocs = App.Client.CurrentSession.Locations.AllLocationsChecked;
            if (checkedLocs.Count() == cached_AllLocationsChecked_count) // end early if no change
                return;
            cached_AllLocationsChecked_count = checkedLocs.Count();

            var uncheckedLocs = App.Client.CurrentSession.Locations.AllMissingLocations;

            var checkedItemlots = LocationHelper.GetItemLotFlags()
                .Where(x => checkedLocs.Contains(x.Id)).GroupBy(x => x.Flag).ToDictionary(x => x.First().Flag); // location is checked

            var uncheckedItemLots = LocationHelper.GetItemLotFlags()
                .Where(x => uncheckedLocs.Contains(x.Id)).ToList().GroupBy(x=>x.Flag).ToDictionary(x => x.First().Flag); // location is "missing"

            int numchecked = 0;
            int numremoved = 0;
            //var shopflags = LocationHelper.GetShopLineupFlags(); // for debugging
            foreach (var location in checkedItemlots)
            {
                int thisflag = location.Key;
                if (!isFlagOnInBuffer(flags, thisflag))
                {
                    numchecked++;
                    foreach(var loc1 in location.Value)
                        Log.Logger.Verbose($"{loc1.Name} collected by server or another player - checking eligibility.");

                    if (uncheckedItemLots.ContainsKey(thisflag)) // unchecked item on same flag, don't remove
                        continue;

                    if (uncheckedItemLots.ContainsKey(thisflag + 1)) // unchecked item on next flag, don't remove
                        continue;
                    if (uncheckedItemLots.ContainsKey(thisflag - 1)) // unchecked item on prev flag, don't remove
                        continue;

                    if (checkedItemlots.ContainsKey(thisflag + 1) && uncheckedItemLots.ContainsKey(thisflag + 2)) // unchecked item on next +2 flag, don't remove
                        continue;
                    if (checkedItemlots.ContainsKey(thisflag - 1) && uncheckedItemLots.ContainsKey(thisflag - 2)) // unchecked item on prev -2 flag, don't remove
                        continue;

                    numremoved++;
                    Task.Run(() =>
                    {
                        Task.Delay(100);
                        App.RemoveItemBag(thisflag);
                        App.SetEventFlag(thisflag, true);
                    });
                }
            }
            Log.Logger.Debug($"collect: # checked: {numchecked}, # removed: {numremoved}");


            // now do the same thing for shop items
            var checkedShoplots = LocationHelper.GetShopLineupFlags()
                .Where(x => checkedLocs.Contains(x.Id)).GroupBy(x => x.Flag).ToDictionary(x => x.First().Flag); // location is checked

            numchecked = 0;
            numremoved = 0;
            foreach (var location in checkedShoplots)
            {
                int thisflag = location.Key;
                if (!isFlagOnInBuffer(flags, thisflag))
                {
                    numchecked++;
                    foreach (var loc1 in location.Value)
                        Log.Logger.Verbose($"{loc1.Name} collected by server or another player - checking eligibility.");

                    if (uncheckedItemLots.ContainsKey(thisflag)) // unchecked item on same flag, don't remove
                        continue;

                    numremoved++;
                    Task.Run(() =>
                    {
                        Task.Delay(100);
                        App.SetEventFlag(thisflag, true);
                    });
                }
            }
            Log.Logger.Debug($"shop collect: # checked: {numchecked}, # removed: {numremoved}");
        }
        static string BossStorageKey = "";
        public static long serverBosses = 0;
        /// <summary>
        /// Start tracking boss statuses
        /// </summary>
        /// <returns></returns>
        public static void TrackBossDefeatsAsync()
        {
            if (App.Client?.CurrentSession?.ConnectionInfo == null)
                return;
            ArchipelagoClient Client = App.Client;
            BossStorageKey = $"dsr_bosses_{Client.CurrentSession.ConnectionInfo.Team}_{Client.CurrentSession.ConnectionInfo.Slot}";
            Client.CurrentSession.DataStorage[BossStorageKey].Initialize(0);
            Client.CurrentSession.DataStorage[BossStorageKey].OnValueChanged -= UpdateBossesFromServer;
            Client.CurrentSession.DataStorage[BossStorageKey].OnValueChanged += UpdateBossesFromServer;

            Client.CurrentSession.DataStorage[BossStorageKey].GetAsync().ContinueWith(t => {
                serverBosses = (long)t.Result;
            });
        }
        private static void UpdateBossesFromServer(JToken originalValue, JToken newValue, Dictionary<string, JToken> additionalArguments)
        {
            UpdateBossesFromServer((long)newValue);
        }
        private static void UpdateBossesFromServer(long newValue)
        {
            serverBosses = (long)newValue;
        }
        // Bosses -> polling method
        static long cached_local_boss_pflags = 0;
        private static void PollBosses(byte[] flags)
        {
            // Get 'cached local bonfire long', and check the '0' fields' flags; turn on if they are set.
            // Then, compare it to the server bonfires. For any on in server and not in local, turn it on
            var originalServerBosses = serverBosses;

            long local_boss_pflags = cached_local_boss_pflags;

            if (App.DSOptions.Goal == DSGoal.all_bosses)
            {
                var bossLocs = LocationHelper.GetBossFlags();
                Dictionary<int, BossFlag> bossmap = bossLocs.ToDictionary(x => x.PersistId, x => x);

                for (int i = 1; i < 32; i++)
                {
                    if (((local_boss_pflags >> (i - 1)) & 0x00000001) == 0) // if (i-1) bit is off
                    {
                        if (bossmap.TryGetValue(i, out var boss)) // get the corresponding bonfire
                        {
                            // if the bonfire flag is on, mark the pflag on
                            if (isFlagOnInBuffer(flags, boss.Flag))
                            {
                                local_boss_pflags |= (long)1 << (i - 1);
                                Log.Logger.Information($"Boss defeated: {boss.Name}");
                                continue;
                            }

                            // if the pflag is on in the server flags, turn on the flag and or it
                            if (((originalServerBosses >> (i - 1)) & 0x00000001) == 1) // if server i-1 bit is on
                            {
                                //App.SetEventFlag(boss.Flag, true);
                                local_boss_pflags |= (long)1 << (i - 1);
                                Log.Logger.Information($"Boss defeated remotely: {boss.Name}");
                            }
                        }
                    }
                }
                if (local_boss_pflags != cached_local_boss_pflags) // if flags changed
                {
                    // if goal was reached
                    if ((local_boss_pflags & App.DSOptions.RequiredBosses) == App.DSOptions.RequiredBosses)
                    {
                        Log.Logger.Information($"Sending Goal for All Bosses");
                        App.SendGoal();
                    }
                    // if goal wasn't reached, calculate how close they are
                    else
                    {
                        int bosses_completed = 0;
                        int bosses_total = 0;
                        for (int i = 1; i < 32; i++)
                        {
                            if (((App.DSOptions.RequiredBosses >> (i-1)) & 0x01) == 1)
                            {
                                bosses_total++;
                                if (((local_boss_pflags >> (i-1)) & 0x01) == 1)
                                    bosses_completed++;
                            }
                        }
                        Log.Logger.Information($"Bosses completed/total = {bosses_completed}/{bosses_total}");
                    }
                }
                // if server doesn't match local, "or" them.
                if (local_boss_pflags != originalServerBosses)
                {
                    Task.Run(() =>
                    {
                        if (BossStorageKey != "")
                            App.Client.CurrentSession.DataStorage[BossStorageKey] += Bitwise.Or(local_boss_pflags);
                    });
                }
                cached_local_boss_pflags = local_boss_pflags;
            }
            
        }
        // just goalcheck all bosses
        internal static bool GoalCheckAllBosses()
        {
            var bossLocs = LocationHelper.GetBossFlags();
            Dictionary<int, BossFlag> bossmap = bossLocs.ToDictionary(x => x.PersistId, x => x);
            // if goal was reached
            if ((cached_local_boss_pflags & App.DSOptions.RequiredBosses) == App.DSOptions.RequiredBosses)
            {
                Log.Logger.Information($"Sending Goal for All Bosses");
                App.SendGoal();
                return true;
            }
            // if goal wasn't reached, calculate how close they are
            else
            {
                int bosses_completed = 0;
                int bosses_total = 0;
                for (int i = 1; i < 32; i++)
                {
                    if (((App.DSOptions.RequiredBosses >> (i - 1)) & 0x01) == 1)
                    {
                        bosses_total++;
                        if (((cached_local_boss_pflags >> (i - 1)) & 0x01) == 1)
                        {
                            bosses_completed++;
                            Log.Logger.Information($"[x] {bossmap[i].Name}");
                        }
                        else
                            Log.Logger.Information($"[_] {bossmap[i].Name}");
                    }
                }
                Log.Logger.Information($"Bosses completed/total = {bosses_completed}/{bosses_total}");
                return false;
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
            var d5678 = GetFlagId5678DigitFromByteAndBit(bytenum, bitnum);

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
