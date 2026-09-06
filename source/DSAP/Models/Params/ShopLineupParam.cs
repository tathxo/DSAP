namespace DSAP.Models
{
    internal class ShopLineupParam : IParam
    {
        public static uint Size { get; set; } = 0x20;
        public static int spOffset = 0x720;
        
        public const int EQUIP_ID = 0x0;
        public const int COST = 0x4;
        public const int EVENT_FLAG = 0xc;
        public const int SELL_QUANTITY = 0x14;
        public const int SHOP_TYPE = 0x16;
        public const int EQUIP_TYPE = 0x17;

        public uint Id { get; set; } = 0;
        public string Name { get; set; } = "unknown";
        public int ItemId { get; set; } = 0;
        public int Cost { get; set; } = 0;
        public int MatCost { get; set; } = 0;
        public int EventFlag { get; set; } = -1;
        public int Qwc { get; set; } = 0;
        public int SellQuantity { get; set; } = 0;
        public byte ShopType { get; set; } = 0;
        public byte EquipType { get; set; } = 0;
        public ShopLineupParam()
        {
            return;
        }
    }
}
