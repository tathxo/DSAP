using Archipelago.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Models
{
    internal class DarkSoulsLocation : Location
    {
        // Delivered Item Flag Id => For local rando, keep track of which items player has gotten, on their save file, 
        // so that we don't resend on reconnect/reload.
        public int DeliveredFlagId = 0;
        public ulong Address2 { get; set; }
        public int Address2Bit { get; set; }
    }
}
