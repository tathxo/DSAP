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

        public int equip_id;
        public int cost;
        public int event_flag;
        public int sell_quantity;
        public byte shop_type;
        public byte equip_type;

        public ShopLineupParam()
        {
            return;
        }
    }
}
