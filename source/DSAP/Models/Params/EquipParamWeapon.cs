namespace DSAP.Models
{
    internal class EquipParamWeapon : IParam
    {
        public static uint Size { get; set; } = 0x10b;
        public static int spOffset = 0x18;

        public const int WEIGHT_OFFSET = 0xC;
    }
}
