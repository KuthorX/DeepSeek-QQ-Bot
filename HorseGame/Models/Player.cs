namespace QQBotCSharp.HorseGame.Models
{
    public class Player
    {
        public uint GroupUin { get; set; }
        public uint UserUin { get; set; }
        public int Points { get; set; }
        public DateTime LastSignInDate { get; set; }
        public int Level { get; set; } = 1;
        public const int MaxPoints = 2000000000; // 20亿积分上限
        public const int LevelUpCost = 100000000; // 1亿购买1级
    }
}