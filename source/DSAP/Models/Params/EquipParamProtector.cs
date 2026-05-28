namespace DSAP.Models
{
    internal class EquipParamProtector : IParam
    {
        public static uint Size { get; set; } = 0xe3;
        public static int spOffset = 0x60;

        public const int WEIGHT_OFFSET = 0x20;
    }
}
