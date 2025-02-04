using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace QQBotCSharp.HorseGame
{
    public class GameManager
    {
        private readonly BotContext _context;
        private readonly Dictionary<long, HorseRace> _activeRaces = new(); // 群号 -> 比赛实例

        public GameManager(BotContext context)
        {
            _context = context;
        }

        public bool IsRunning(uint groupUin)
        {
            return _activeRaces.ContainsKey(groupUin);
        }

        public async Task StartRaceAsync(uint groupUin)
        {
            if (_activeRaces.ContainsKey(groupUin))
            {
                await SendMessageAsync(groupUin, "当前群已有一场赛马比赛正在进行中！");
                return;
            }

            var race = new HorseRace(_context, groupUin);
            _activeRaces[groupUin] = race;
            await race.StartAsync();
            _activeRaces.Remove(groupUin);
        }

        public async Task PlaceBetAsync(uint groupUin, uint userUin, int horseId, int betAmount)
        {
            if (_activeRaces.TryGetValue(groupUin, out var race))
            {
                await race.PlaceBetAsync(userUin, horseId, betAmount);
            }
            else
            {
                await SendMessageAsync(groupUin, "当前没有正在进行的赛马比赛！");
            }
        }

        private async Task SendMessageAsync(uint groupUin, string message)
        {
            var chain = MessageBuilder.Group(groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }
    }
}