using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static DSAP.Enums;


namespace DSAP.Models
{
    public class ItemLotFlag : EventFlag
    {
        public bool IsEnabled { get; set; } = true;
        public int ItemLotParamId { get; set; } = 0;
        public int OriginalItemId { get; set; } = 0;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DSItemCategory OriginalItemCategory { get; set; } = DSItemCategory.INVALID;
        public int OriginalFlagId { get; set; } = 0;
        public int DeliveredFlagId { get; set; } = 0;
    }
}
