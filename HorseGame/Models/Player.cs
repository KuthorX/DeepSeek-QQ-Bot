namespace QQBotCSharp.HorseGame.Models
{
    public class Player
    {
        public uint GroupUin { get; set; }
        public uint UserUin { get; set; }
        public long Points { get; set; }
        public DateTime LastSignInDate { get; set; }
        public int Level { get; set; } = 1;
        public const long MaxPoints = 2000000000; // 20亿积分上限
        public const long LevelUpCost = 100000000; // 1亿购买1级
    }
}