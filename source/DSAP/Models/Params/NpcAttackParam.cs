namespace DSAP.Models
{
    internal class NpcAttackParam : IParam
    {
        public static uint Size { get; set; } = 0x80;
        public static int spOffset = 0x210;
    }
}
