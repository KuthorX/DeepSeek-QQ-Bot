using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace QQBotCSharp.HorseGame
{
    public class PlayerManager
    {
        private readonly BotContext _context;
        private readonly Database _database;

        public async Task BuyLevelAsync(uint groupUin, uint userUin, int levelCount)
        {
            if (levelCount <= 0)
            {
                await SendMessageAsync(groupUin, "购买等级数必须大于0！");
                return;
            }
            else if (levelCount >= 100)
            {
                await SendMessageAsync(groupUin, "购买等级数必须小于等于100！");
                return;
            }

            var totalCost = levelCount * Models.Player.LevelUpCost;
            var (currentPoints, currentLevel) = await _database.GetPlayerInfoAsync(groupUin, userUin);

            if (currentPoints < totalCost)
            {
                await SendMessageAsync(groupUin, $"积分不足！购买{levelCount}级需要{totalCost}积分，你当前有{currentPoints}积分。");
                return;
            }

            if (!await _database.DeductPointsAsync(groupUin, userUin, totalCost))
            {
                await SendMessageAsync(groupUin, "购买失败，请稍后重试。");
                return;
            }

            await _database.UpdatePlayerLevelAsync(groupUin, userUin, currentLevel + levelCount);
            await SendMessageAsync(groupUin, $"购买成功！消耗{totalCost}积分，等级提升{levelCount}级，当前等级：{currentLevel + levelCount}");
        }

        public PlayerManager(BotContext context)
        {
            _context = context;
            _database = new Database();
        }

        public async Task QueryPointsAsync(uint groupUin, uint userUin)
        {
            var (points, level) = await _database.GetPlayerInfoAsync(groupUin, userUin);
            await SendMessageAsync(groupUin, $"你的当前等级为：{level}，积分为：{points}");
        }

        public async Task GetGroupMemberRankingAsync(uint groupUin)
        {
            var players = new List<(uint UserUin, long Points, int Level)>();
            using (var database = new Database())
            {
                var uinPoints = await database.GetGroupMemberRankingAsync(groupUin);
                if (uinPoints.Count == 0)
                {
                    await SendMessageAsync(groupUin, "本群没有群友的赛马积分记录。");
                    return;
                }
                foreach (var (userUin, points) in uinPoints)
                {
                    var (_, level) = await database.GetPlayerInfoAsync(groupUin, userUin);
                    players.Add((userUin, points, level));
                }
            }

            var groupMembers = await _context.FetchMembers(groupUin, true);
            var uinNames = new Dictionary<uint, string>();
            foreach (var m in groupMembers)
            {
                uinNames[m.Uin] = m.MemberCard ?? m.MemberName;
            }

            // 按等级降序，等级相同时按积分降序排序
            players = players.OrderByDescending(p => p.Level)
                            .ThenByDescending(p => p.Points)
                            .ToList();

            var chain = MessageBuilder.Group(groupUin).Text("本群赛马排名\n");
            var rank = 1;
            foreach (var (userUin, points, level) in players)
            {
                chain.Text($"{rank}. ").Text($"{uinNames[userUin]}").Text($" Lv.{level}({points})\n");
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

        public static async Task<bool> DeductPointsAsync(uint groupUin, uint userUin, long amount)
        {
            using (var database = new Database())
            {
                return await database.DeductPointsAsync(groupUin, userUin, amount);
            }
        }

        public static async Task AddPointsAsync(uint groupUin, uint userUin, long amount)
        {
            using (var database = new Database())
            {
                await database.AddPointsAsync(groupUin, userUin, amount);
            }
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