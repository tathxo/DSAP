
namespace DSAP.Models
{
    public class MapPoi // map point of interest (rooms, entrances, etc)
    {
        public string MapName { get; set; } = "";
        public string PoiName { get; set; } = "";
        public int SubMapId { get; set; } = 0;
        public int RoomId { get; set; } = 0;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;
    }
}
