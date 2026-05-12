using Archipelago.Core.Json;
using System.Text.Json.Serialization;

namespace DSAP.Models
{
    public class BonfireWarp : EventFlag
    {
        public int Flag { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int DsrId { get; set; }
        public int PersistId { get; set; }
        public int EntityId { get; set; }
        public int FmgId { get; set; }
    }
}
