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
        const int REPEAT_TIMER_MS = 1000;
        private static MapInfo cached_mapInfo { get; set; } = new MapInfo();
        internal static void StartMapAutoTracking()
        {
            // Every second, if player is in game. If so, validate that they are in the same save 
            Task.Run(async () =>
            {
                try
                {
                    string MapKey = $"map_{App.Client.CurrentSession.ConnectionInfo.Team}_{App.Client.CurrentSession.ConnectionInfo.Slot}";
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
                            if (mapInfo.MapId != 0 && mapInfo.MapId != cached_mapInfo.MapId)
                            {
                                App.Client.CurrentSession.DataStorage[MapKey] = mapInfo.MapId;
                                cached_mapInfo.MapId = mapInfo.MapId;
                            }
                            else
                            {
                                Log.Logger.Verbose($"unchanged map: {mapInfo.MapId} from {cached_mapInfo.MapId}");
                            }
                        }
                        await Task.Delay(REPEAT_TIMER_MS);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Exception in ingame listener: {ex.Message}\n{ex.InnerException}\n{ex.Source}");
                }
            });
        }
        public static MapInfo GetPosition()
        {
            Log.Logger.Verbose("Getting position");
            var MapInfo = new MapInfo();
            if (MiscHelper.IsInGame())
            {
                // map = worldnumber + area number. e.g. 10 + 02 => m10_02 = firelink shrine
                ulong eoffset = AddressHelper.GetBaseEAddress();
                if (eoffset != 0)
                {
                    uint worldnumber = GetWorldNumber();
                    uint areanumber = GetAreaNumber();
                    (float x, float y, float z) xyzPos = GetXyzCoords();
                    Log.Logger.Verbose($"Position: got w/a {worldnumber} {areanumber}");
                    if (worldnumber > 9 && worldnumber < 19 && areanumber >= 0 && areanumber < 3)
                    {

                        int tempMapId = (int)(100000 * worldnumber + 1000 * areanumber);
                        Dictionary<int, string> mapDict = new List<(int, string)>([
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
                            (1801000, "Northern Undead Asylum")
                            ]).ToDictionary();

                        MapInfo.X = xyzPos.x;
                        MapInfo.Y = xyzPos.y;
                        MapInfo.Z = xyzPos.z;
                        Log.Logger.Verbose($"tempMapId = {tempMapId}");
                        if (mapDict.TryGetValue(tempMapId, out var mapName))
                        {
                            if (mapName.StartsWith("* ")) // exceptions
                            {
                                MapInfo.MapId = tempMapId;
                                // compare to list of "points of interest"
                                // get points of interest whose map = current

                                var pois = MiscHelper.GetMapPois();
                                if (pois.Contains(mapName))
                                {
                                    double min_distance = float.MaxValue;
                                    MapPoi best_poi = pois[mapName].First();
                                    foreach (var poi in pois[mapName])
                                    {
                                        double distance = Math.Sqrt(Math.Pow(poi.X - MapInfo.X, 2) + Math.Pow(poi.Y - MapInfo.Y, 2) + Math.Pow(poi.Z - MapInfo.Z, 2));
                                        if (distance < min_distance)
                                        {
                                            min_distance = distance;
                                            best_poi = poi;
                                        }
                                    }
                                    // if there's a submap, update map id to it
                                    if (best_poi.SubMapId > 0)
                                        MapInfo.MapId += 100 * best_poi.SubMapId;
                                    
                                    Log.Logger.Verbose($"Best Poi: {best_poi.PoiName}, distance={min_distance}, submap={best_poi.SubMapId}");
                                }
                            }
                            else
                                MapInfo.MapId = tempMapId;


                            
                            Log.Logger.Debug($"Map: {MapInfo.MapId}, {mapName}, \nx={xyzPos.x}, \ny={xyzPos.y}, \nz={xyzPos.z}");
                        }
                        return MapInfo;
                    }
                }
            }
            Log.Logger.Debug($"Got position: {MapInfo.MapId} (no update)");
            return MapInfo;
        }
        public static uint GetWorldNumber(ulong eOffset = 0) // E + A23
        {
            if (eOffset == 0)
                eOffset = AddressHelper.GetBaseEAddress();
            if (eOffset != 0)
            {
                var next = MiscHelper.OffsetPointer(eOffset, 0xA23);
                return Memory.ReadByte(next);
            }
            return 0;
        }
        public static uint GetAreaNumber(ulong eOffset = 0) // E + A22
        {
            if (eOffset == 0)
                eOffset = AddressHelper.GetBaseEAddress();
            if (eOffset != 0)
            {
                var next = MiscHelper.OffsetPointer(eOffset, 0xA22);
                return Memory.ReadByte(next);
            }
            return 0;
        }
        public static (float, float, float) GetXyzCoords(ulong eOffset = 0) // chr pos data + 0x10, 0x14, 0x18
        {
            if (eOffset == 0)
                eOffset = AddressHelper.GetBaseEAddress();
            if (eOffset != 0)
            {
                // chr pos data = chr map data + 0x28
                // chr map data = chrdata1 + 0x48
                // chrdata1 = [world chr base + 68]
                var next = MiscHelper.OffsetPointer(eOffset, 0xA28);
                byte[] pos12 = Memory.ReadByteArray(next, 12);
                float x = BitConverter.ToSingle(pos12, 0);
                float y = BitConverter.ToSingle(pos12, 4);
                float z = BitConverter.ToSingle(pos12, 8);
                return (x, y, z);
            }
            return (0, 0, 0);
        }
    }
}
