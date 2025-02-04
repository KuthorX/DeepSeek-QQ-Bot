namespace QQBotCSharp.HorseGame.Utils
{
    public static class EmojiHelper
    {
        private static readonly List<string> Emojis = new() { 
            // 原列表
            "🐎", "🦄", "🐴", "🦓", "🐘", "🦒", "🐐", "🐖", "🐕",
            // 鸟类
            "🦜", "🐧", "🦚", "🦢", "🦩", "🦉", "🐦", "🦃", "🐓",
            // 海洋生物
            "🐙", "🐬", "🐋", "🦈", "🦀", "🦞", "🦐", "🐠", "🐡",
            // 昆虫类
            "🦋", "🐝", "🪲", "🐞", "🦗", "🕷", "🦂", "🐜",
            // 爬行与两栖类
            "🐊", "🐍", "🦎", "🐢", "🐸",
            // 特色哺乳动物
            "🐅", "🐆", "🦌", "🦏", "🦛", "🐪", "🐫", "🦘", "🦙",
            // 其他可爱动物
            "🐇", "🦝", "🦨", "🦡", "🦫", "🦦", "🐿", "🦔",
            // 农场动物
            "🐑", "🐄", "🐂", "🦤", // 渡渡鸟
            // 神话动物
            "🐉", "🐲",
            // 新增流行动物
            "🦥", "🦣", "🦭", "🦤", "🦬"
         };

        public static List<string> GetRandomEmojis(int count)
        {
            var random = new Random();
            return Emojis.OrderBy(x => random.Next()).Take(count).ToList();
        }
    }
}