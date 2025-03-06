using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core;
using Lagrange.Core.Message;

namespace QQBotCSharp.PokemonGame
{
    public class GameManager
    {
        private readonly BotContext _context;

        public GameManager(BotContext context)
        {
            _context = context;
        }

        public async Task StartGameAsync(uint groupUin)
        {
            await SendMessageAsync(groupUin, "宝可梦游戏开始！");
        }

        private async Task SendMessageAsync(uint groupUin, string message)
        {
            var chain = MessageBuilder.Group(groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }
    }
}