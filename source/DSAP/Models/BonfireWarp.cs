using Archipelago.Core.Json;
using System.Text.Json.Serialization;

namespace DSAP.Models
{
    public class BonfireWarp : EventFlag
    {
        public int Flag { get; set; }
        public string VanillaName { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int DsrId { get; set; }
        public int PersistId { get; set; }
        public int EntityId { get; set; }
        public int VanillaFmgId { get; set; }
        public int UpdatedFmgId { get; set; }
        public int VanillaSort { get; set; }
        public int ProgressiveSort { get; set; }
    }
}
