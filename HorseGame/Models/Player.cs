namespace QQBotCSharp.HorseGame.Models
{
    public class Player
    {
        public uint GroupUin { get; set; }
        public uint UserUin { get; set; }
        public int Points { get; set; }
        public DateTime LastSignInDate { get; set; }
    }
}