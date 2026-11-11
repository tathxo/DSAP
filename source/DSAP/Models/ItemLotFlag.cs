using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Models
{
    public class ItemLotFlag : EventFlag
    {
        public bool IsEnabled { get; set; } = true;
        public int ItemLotParamId { get; set; } = 0;
        public int OriginalFlagId { get; set; } = 0;
        public int DeliveredFlagId { get; set; } = 0;
    }
}
