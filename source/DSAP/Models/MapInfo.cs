
namespace DSAP.Models
{
    public class MapInfo
    {
        public uint MapIdLong { get; set; } = 0;
        public uint MapId3 { get; set; } = 0;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;
        public uint World
        {
            get { return MapId3 / 10; }
            set { MapId3 = (MapId3 % 10) + 10 * value; }
        }
        public uint Area
        {
            get { return MapId3 % 10; }
            set { MapId3 = (MapId3 / 10) * 10 + value; }
        }
    }
}
