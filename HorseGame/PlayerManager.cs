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

        public async Task GetGroupMemberRankingAsync(uint groupUin)
        {
            var uinPoints = await _database.GetGroupMemberRankingAsync(groupUin);
            if (uinPoints.Count == 0)
            {
                await SendMessageAsync(groupUin, "本群没有群友的赛马积分记录。");
                return;
            }
            var groupMembers = await _context.FetchMembers(groupUin, true);
            var uinNames = new Dictionary<uint, string>();
            foreach (var m in groupMembers)
            {
                uinNames[m.Uin] = m.MemberCard ?? m.MemberName;
            }
            var chain = MessageBuilder.Group(groupUin).Text("本群赛马积分排名\n");
            var rank = 1;
            foreach (var (UserUin, Points) in uinPoints)
            {
                chain.Text($"{rank}. ").Text($"{uinNames[UserUin]}").Text($" {Points}\n");
                rank += 1;
            }
            await SendMessageAsync(chain);
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

        public async Task BegAsync(uint groupUin, uint userUin)
        {
            var points = await _database.GetPlayerPointsAsync(groupUin, userUin);
            if (points > 0) {
                await SendMessageAsync(groupUin, "你还有积分，继续梭哈吧！");
                return;
            }
            var point = new Random().Next(100, 201);
            await _database.AddPointsAsync(groupUin, userUin, point);
            await SendMessageAsync(groupUin, $"乞讨成功！获得 {point} 积分。");
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

        private async Task SendMessageAsync(MessageBuilder chain)
        {
            await _context.SendMessage(chain.Build());
        }
    }
}