using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    public class MapHelper
    {
        internal static Dictionary<uint, int> MapToIndex = new List<(uint, int)>([
                (1000000, 11), //  "Depths"), 
                (1001000, 27), //  "* Undead Burg  / Undead Parish"), -> Undead Burg Upper
                (1001100, 28), //  "* Undead Burg  / Undead Parish"), -> Undead Parish
                (1001200, 26), //  "* Undead Burg  / Undead Parish"), -> Undead Burg Lower
                (1002000, 14), // "Firelink Shrine"), 
                (1100000, 19), // "Painted World"), 
                (1200000, 9),  // "* Darkroot Garden / Darkroot Basin"), -> Darkroot Garden
                (1200100, 8),  // "* Darkroot Garden / Darkroot Basin"), -> Darkroot Basin
                (1201000, 30), // "* Oolacile"), -> Sanctuary Garden & Sanctuary
                (1201100, 31), // "* Oolacile"), -> Royal wood
                (1201200, 32), // "* Oolacile"), -> Oolacile Township
                (1201300, 33), // "* Oolacile"), -> Chasm of the Abyss
                (1300000, 6),  // "Catacombs"), 
                (1301000, 24), // "* Tomb of the Giants"), -> Upper
                (1301100, 23), // "* Tomb of the Giants"), -> Lower
                (1302000, 15), // "* Great Hollow / Ash Lake"), -> Great Hollow
                (1302100, 3),  // "* Great Hollow / Ash Lake"), -> Ash Lake
                (1400000, 5),  // "* Blighttown"), -> Upper
                (1400100, 4),  // "* Blighttown"), -> Lower
                (1401000, 10), // "* Demon Ruins / Lost Izalith"), -> Demon Ruins
                (1401100, 17), // "* Demon Ruins / Lost Izalith"), -> Lost Izalith
                (1500000, 21), // "* Sen's Fortress"), -> Main
                (1500100, 20), // "* Sen's Fortress"), -> Basement
                (1500200, 22), // "* Sen's Fortress"), -> Roof
                (1501000, 1),  // "* Anor Londo"), -> Exterior
                (1501100, 2),  // "* Anor Londo"), -> Interior
                (1600000, 18), // "* New Londo Ruins / Valley of Drakes"), -> New Londo Ruins
                (1600100, 29), // "* New Londo Ruins / Valley of Drakes"), -> Valley of Drakes
                (1700000, 12), // "* Duke's Archives / Crystal Cave"), -> Main
                (1700100, 13), // "* Duke's Archives / Crystal Cave"), -> Big Room
                (1700200, 7),  // "* Duke's Archives / Crystal Cave"), -> Crystal Cave
                (1800000, 16), // "Kiln of the First Flame"), 
                (1801000, 25)  // "Undead Asylum")]]
            ]).ToDictionary();
        const int REPEAT_TIMER_MS = 1000;
        private static MapInfo cached_mapInfo { get; set; } = new MapInfo();
        internal static void StartMapAutoTracking()
        {
            // Every second, if player is in game. If so, validate that they are in the same save 
            Task.Run(async () =>
            {
                try
                {
                    string MapKey = $"DSR_current_map_{App.Client.CurrentSession.ConnectionInfo.Team}_{App.Client.CurrentSession.ConnectionInfo.Slot}";
                    App.Client.CurrentSession.DataStorage[MapKey].Initialize(0);

                    while (true)
                    {
                        if (!App.Client.IsConnected)
                        {
                            Log.Logger.Error("Client disconnection detected - stopping map listener");
                            return;
                        }
                        bool isInGame = MiscHelper.IsInGame();
                        if (App.ControlsContext.TrackerMapTabSwitching && isInGame && App.SaveidSet)
                        {
                            var mapInfo = GetPosition();
                            if (mapInfo.MapIdLong != 0 && mapInfo.MapIdLong != cached_mapInfo.MapIdLong)
                            {
                                int mapindex = MapToIndex[mapInfo.MapIdLong];
                                App.Client.CurrentSession.DataStorage[MapKey] = mapindex;
                                cached_mapInfo.MapIdLong = mapInfo.MapIdLong;
                            }
                            else
                            {
                                Log.Logger.Verbose($"unchanged map: {mapInfo.MapIdLong}/idx{(mapInfo.MapIdLong == 0 ? 0 : MapToIndex[mapInfo.MapIdLong])} from {cached_mapInfo.MapIdLong}/idx{(mapInfo.MapIdLong == 0 ? 0 : MapToIndex[cached_mapInfo.MapIdLong])}");
                            }
                        }
                        await Task.Delay(REPEAT_TIMER_MS);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Exception in ingame map listener: {ex.Message}\n{ex.InnerException}\n{ex.Source}");
                }
            });
        }
        internal static void StartDisconnectedMapAutoTracking()
        {
            // Every second, if player is in game. If so, validate that they are in the same save 
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        bool isInGame = MiscHelper.IsInGame();
                        if (App.ControlsContext.TrackerMapTabSwitching && isInGame)
                        {
                            var mapInfo = GetPosition();
                            if (mapInfo.MapIdLong != 0 && mapInfo.MapIdLong != cached_mapInfo.MapIdLong)
                            {
                                int mapindex = MapToIndex[mapInfo.MapIdLong];
                                cached_mapInfo.MapIdLong = mapInfo.MapIdLong;
                                Log.Logger.Debug($"written map: {mapindex}");
                            }
                            else
                            {
                                Log.Logger.Verbose($"unchanged map: {mapInfo.MapIdLong} from {cached_mapInfo.MapIdLong}");
                            }
                        }
                        await Task.Delay(REPEAT_TIMER_MS);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Exception in ingame map listener: {ex.Message}\n{ex.InnerException}\n{ex.Source}");
                }
            });
        }
        public static MapInfo GetPosition()
        {
            Log.Logger.Verbose("Getting position");
            var mapInfo = new MapInfo();
            if (MiscHelper.IsInGame())
            {
                // map = worldnumber + area number. e.g. 10 + 02 => m10_02 = firelink shrine
                ulong eoffset = AddressHelper.GetBaseEAddress();
                if (eoffset != 0)
                {
                    mapInfo = GetMapAndXyzCoords();
                    Log.Logger.Verbose($"Position: got w/a {mapInfo.World} {mapInfo.Area}");
                    if (mapInfo.World > 9 && mapInfo.World < 19 && mapInfo.Area >= 0 && mapInfo.Area < 3)
                    {
                        uint tempMapIdLong = (100000 * mapInfo.World + 1000 * mapInfo.Area);
                        Dictionary<uint, string> mapDict = new List<(uint, string)>([
                            (1000000, "Depths"),
                            (1001000, "* Undead Burg / Undead Parish"),
                            (1002000, "Firelink Shrine"),
                            (1100000, "Painted World"),
                            (1200000, "* Darkroot Garden / Darkroot Basin"), // +1
                            (1201000, "* Oolacile"), // +3
                            (1300000, "Catacombs"),
                            (1301000, "* Tomb of the Giants"), // +1
                            (1302000, "* Great Hollow / Ash Lake"), // +1
                            (1400000, "* Blighttown"), // +1
                            (1401000, "* Demon Ruins / Lost Izalith"), // +1
                            (1500000, "* Sen's Fortress"), // +2
                            (1501000, "* Anor Londo"), // +1
                            (1600000, "* New Londo Ruins / Valley of Drakes"), // +1
                            (1700000, "* Duke's Archives / Crystal Cave"), // +2
                            (1800000, "Kiln of the First Flame"),
                            (1801000, "Undead Asylum")
                            ]).ToDictionary();

                        Log.Logger.Verbose($"tempMapId = {tempMapIdLong}");
                        if (mapDict.TryGetValue(tempMapIdLong, out var mapName))
                        {
                            if (mapName.StartsWith("* ")) // exceptions
                            {
                                mapInfo.MapIdLong = tempMapIdLong;
                                // compare to list of "points of interest"
                                // get points of interest whose map = current

                                var pois = MiscHelper.GetMapPois();
                                if (pois.Contains(mapName))
                                {
                                    double min_distance = float.MaxValue;
                                    MapPoi best_poi = pois[mapName].First();
                                    foreach (var poi in pois[mapName])
                                    {
                                        double distance = Math.Sqrt(Math.Pow(poi.X - mapInfo.X, 2) + Math.Pow(poi.Y - mapInfo.Y, 2) + Math.Pow(poi.Z - mapInfo.Z, 2));
                                        if (distance < min_distance)
                                        {
                                            min_distance = distance;
                                            best_poi = poi;
                                        }
                                    }
                                    // if there's a submap, update map id to it
                                    if (best_poi.SubMapId > 0)
                                        mapInfo.MapIdLong += (uint)(100 * best_poi.SubMapId);

                                    Log.Logger.Verbose($"Best Poi: {best_poi.PoiName}, distance={min_distance:F2}, submap={best_poi.SubMapId}");
                                }
                            }
                            else
                                mapInfo.MapIdLong = tempMapIdLong;



                            
                            Log.Logger.Verbose($"Map: {mapInfo.MapId3}, {mapName}, \nx={mapInfo.X:F2}, \ny={mapInfo.Y:F2}, \nz={mapInfo.Z:F2}");
                        }
                        return mapInfo;
                    }
                }
            }
            Log.Logger.Debug($"Got position: {mapInfo.MapId3} (no update)");
            return mapInfo;
        }
        // consolidate to one read for performance
        public static MapInfo GetMapAndXyzCoords(ulong eOffset = 0) // chr pos data + 0x10, 0x14, 0x18
        {
            MapInfo mapInfo = new MapInfo();

            if (eOffset == 0)
                eOffset = AddressHelper.GetBaseEAddress();
            if (eOffset != 0)
            {
                var startpos = MiscHelper.OffsetPointer(eOffset, 0xA22);
                byte[] pos18 = Memory.ReadByteArray(startpos, 18);
                byte world = pos18[1]; // 0xA23
                byte area = pos18[0]; // 0xA22
                mapInfo.MapId3 = (uint)world * 10 + (uint)area;
                string str = "";
                Log.Logger.Verbose($"building mapid3={mapInfo.MapId3}, w={world}, a={area}, pos18={BitConverter.ToString(pos18)}");
                // unused comments
                // chr pos data = chr map data + 0x28
                // chr map data = chrdata1 + 0x48
                // chrdata1 = [world chr base + 68]
                mapInfo.X = BitConverter.ToSingle(pos18, 6);
                mapInfo.Y = BitConverter.ToSingle(pos18, 10);
                mapInfo.Z = BitConverter.ToSingle(pos18, 14);
            }
            return mapInfo;
        }
    }
}
