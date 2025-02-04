using QQBotCSharp.HorseGame.Models;

namespace QQBotCSharp.HorseGame.Utils
{
    public static class MessageHelper
    {
        public static string BuildRaceStatus(List<Horse> horses)
        {
            return string.Join("\n", horses.Select(h => new string('_', h.Position) + h.Emoji));
        }
    }
}