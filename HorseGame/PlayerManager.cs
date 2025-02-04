using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace QQBotCSharp.HorseGame
{
    public class PlayerManager
    {
        private readonly BotContext _context;
        private readonly Database _database;

        public PlayerManager(BotContext context)
        {
            _context = context;
            _database = new Database();
        }

        public async Task QueryPointsAsync(uint groupUin, uint userUin)
        {
            var points = await _database.GetPlayerPointsAsync(groupUin, userUin);
            await SendMessageAsync(groupUin, $"你的当前积分为：{points}");
        }

        public async Task SignInAsync(uint groupUin, uint userUin)
        {
            if (await _database.IsSignedInTodayAsync(groupUin, userUin))
            {
                await SendMessageAsync(groupUin, "你今天已经签到过了！");
                return;
            }

            await _database.UpdateSignInAsync(groupUin, userUin);
            await SendMessageAsync(groupUin, "签到成功！获得1000积分。");
        }

        public static async Task<bool> DeductPointsAsync(uint groupUin, uint userUin, int amount)
        {
            var database = new Database();
            return await database.DeductPointsAsync(groupUin, userUin, amount);
        }

        public static async Task AddPointsAsync(uint groupUin, uint userUin, int amount)
        {
            var database = new Database();
            await database.AddPointsAsync(groupUin, userUin, amount);
        }

        private async Task SendMessageAsync(uint groupUin, string message)
        {
            var chain = MessageBuilder.Group(groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }
    }
}