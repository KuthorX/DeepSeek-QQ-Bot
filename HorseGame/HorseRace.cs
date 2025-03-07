using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.VisualBasic;
using QQBotCSharp.HorseGame.Models;
using QQBotCSharp.HorseGame.Utils;

namespace QQBotCSharp.HorseGame
{
    public class HorseRace
    {
        private readonly BotContext _context;
        private readonly uint _groupUin;
        private readonly List<Horse> _horses = new();
        private readonly Dictionary<uint, Bet> _bets = new(); // 玩家 -> 下注
        private List<string> _skillMessages = new();
        private uint currentRound = 0;
        private string currentEnv = "平地";
        private readonly List<string> _envs = ["平地", "草原", "丛林", "山地", "泥地", "沙地", "河流"];

        public HorseRace(BotContext context, uint groupUin)
        {
            _context = context;
            _groupUin = groupUin;
            InitializeHorses();
        }

        private void InitializeHorses()
        {
            var emojis = EmojiHelper.GetRandomEmojis(10);
            for (int i = 0; i < 10; i++)
            {
                _horses.Add(new Horse { Id = i + 1, Emoji = emojis[i] });
            }
            currentRound = 0;
        }

        public async Task AwakeSpecialHorse()
        {
            // 出现特殊马
            var targetId = new Random().Next(0, 10);
            _horses[targetId].SepcialHorse = true;
            await SendMessageAsync($"本局出现特殊马 {targetId + 1} 号！技能发动基础概率提升至 80%！");
        }

        public async Task StartAsync()
        {
            currentEnv = _envs[new Random().Next(_envs.Count)];
            // 展示初始赛道
            await SendMessageAsync("赛马比赛开始！以下是参赛选手和赛道：");
            await SendRaceStatusAsync(true);
            if (new Random().Next(100) < 10)
            {
                await AwakeSpecialHorse();
            }
            await SendMessageAsync($"当前环境：{currentEnv}");

            // 等待30秒下注
            await SendMessageAsync("60秒内可以下注。");
            await Task.Delay(60000);

            if (_bets.Count == 0)
            {
                await SendMessageAsync("无人下注，不比了！");
                return;
            }

            await SendMessageAsync("下注时间结束，比赛开始！");
            while (!IsRaceOver())
            {
                await UpdateRaceAsync();
                if (IsRaceOver())
                {
                    break;
                }
                await Task.Delay(3000); // 每3秒更新一次
            }

            await AnnounceResultsAsync();
        }

        private bool IsRaceOver()
        {
            return _horses.Any(h => h.Position >= 20) || _horses.All(h => h.IsDead);
        }

        private async Task UpdateRaceAsync()
        {
            currentRound += 1;
            _skillMessages = [];

            // 触发环境变化
            var random = new Random();
            var realRate = 30 + currentRound * 2;
            if (random.Next(100) < realRate)
            {
                int eventType = random.Next(8); // 7 种场地技能
                switch (eventType)
                {
                    case 0:
                        TriggerLightning();
                        break;
                    case 1:
                        TriggerSwamp();
                        break;
                    case 2:
                        TriggerHurricane();
                        break;
                    case 3:
                        TriggerRainbowBridge();
                        break;
                    case 4:
                        TriggerMeteorShower();
                        break;
                    case 5:
                        TriggerVolcano();
                        break;
                    case 6:
                        TriggerAurora();
                        break;
                    case 7:
                        TriggerMirror();
                        break;
                }
            }

            // 发动马技能
            foreach (var horse in _horses)
            {
                if (!horse.IsDead)
                {
                    var hasBet = _bets.Values.Any(b => b.HorseId == horse.Id);
                    var (skillName, affectedHorses) = horse.TryActivateSkill(_horses, currentRound, hasBet);
                    if (skillName != null)
                    {
                        TriggerSkill(horse, skillName, affectedHorses);
                    }
                }
            }

            foreach (var horse in _horses)
            {
                horse.Move(currentEnv);
            }

            await SendRaceStatusAsync();
        }

        private void TriggerSkill(Horse horse, string skillName, List<Horse>? affectedHorses)
        {
            string message = $"{horse.Emoji} 使用了技能 [{skillName}]！";
            if (affectedHorses != null && affectedHorses.Any())
            {
                message += $" 影响到了：{string.Join(", ", affectedHorses.Select(h => h.Emoji))}";
            }

            _skillMessages.Add(message);
        }


        private void TriggerLightning()
        {
            var targets = _horses
                .Where(h => !h.IsDead)
                .OrderBy(x => new Random().Next())
                .Take(new Random().Next(1, 5))
                .ToList();
            var emojis = "";
            foreach (var target in targets)
            {
                target.IsDead = true;
                emojis += $"{target.Emoji}";
            }
            if (emojis != "")
            {
                _skillMessages.Add($"⚡ 闪电击中了 {emojis}，倒下了！");
            }
        }

        private void TriggerSwamp()
        {
            var target = _horses[new Random().Next(_horses.Count)];
            target.SwampRounds = 3;
            _skillMessages.Add($"🌊 {target.Emoji} 的赛道变成了沼泽，速度减1，持续3回合！");
        }

        private void TriggerHurricane()
        {
            foreach (var horse in _horses.Where(h => !h.IsDead))
            {
                horse.Speed = Math.Max(1, horse.Speed - 1);
            }
            _skillMessages.Add("🌪️ 狂风来袭，所有马匹速度-1！");
        }

        private void TriggerRainbowBridge()
        {
            foreach (var horse in _horses.Where(h => !h.IsDead))
            {
                horse.Speed += 1;
            }
            _skillMessages.Add("🌈 彩虹桥出现，所有马匹速度+1！");
        }

        private void TriggerMeteorShower()
        {
            var random = new Random();
            var affectedHorses = _horses
                .Where(h => !h.IsDead)
                .OrderBy(x => random.Next())
                .Take(3)
                .ToList();

            int steps = random.Next(2, 5);
            foreach (var horse in affectedHorses)
            {
                horse.Position = Math.Max(0, horse.Position - steps);
            }
            _skillMessages.Add($"🌠 流星雨降临，{string.Join(", ", affectedHorses.Select(h => h.Emoji))} 后退了{steps}步！");
        }

        private void TriggerVolcano()
        {
            var targets = _horses
                .Where(h => !h.IsDead)
                .OrderBy(x => new Random().Next())
                .Take(new Random().Next(1, 11))
                .ToList();
            var emojis = "";
            foreach (var target in targets)
            {
                target.Position = 0;
                target.Speed = Math.Max(1, target.Speed - 1);
                emojis += $"{target.Emoji}";
            }
            if (emojis != "")
            {
                _skillMessages.Add($"🌋 火山喷发，{emojis} 被击退到起点，速度-1！");
            }
        }

        private void TriggerAurora()
        {
            var random = new Random();
            var messages = new List<string>();
            int delta = random.Next(-2, 3); // -2, -1, 0, 1, 2
            foreach (var horse in _horses.Where(h => !h.IsDead))
            {
                horse.Speed = Math.Max(1, horse.Speed + delta);
            }
            _skillMessages.Add($"🌌 极光出现，所有选手速度变化：{(delta > 0 ? "+" : "")}{delta}");
        }

        private void TriggerMirror()
        {
            foreach (var horse in _horses.Where(h => !h.IsDead))
            {
                horse.Position = 20 - horse.Position;
            }
            _skillMessages.Add($"🪞 魔镜出现，所有选手位置两极反转！");
        }

        private async Task AnnounceResultsAsync()
        {
            // 获取所有存活的马
            var sortedHorses = _horses
                .Where(h => !h.IsDead)
                .OrderByDescending(h => h.Position) // 按位置排序
                .ToList();
            foreach (var item in sortedHorses)
            {
                Console.Write($"{item.Emoji}");
            }
            Console.Write($"\n");
            var rankDetail = new Dictionary<uint, List<Horse>>();
            uint currentRank = 1;
            foreach (var h in sortedHorses)
            {
                var detail = rankDetail.GetValueOrDefault(currentRank, []);
                if (detail.Count == 0)
                {
                    detail.Add(h);
                    rankDetail[currentRank] = detail;
                    continue;
                }
                if (detail[0].Position > h.Position)
                {
                    currentRank += 1;
                    if (currentRank > 3)
                    {
                        break;
                    }
                    rankDetail[currentRank] = [h];
                }
                else
                {
                    detail.Add(h);
                }
            }
            foreach (var item in rankDetail)
            {
                Console.WriteLine($"{item.Key} + {item.Value}");

            }
            if (sortedHorses.Count != 0)
            {
                // 播报比赛结果
                var messages = "比赛结束！以下是排名：\n";
                for (uint rank = 1; rank <= 3; rank++)
                {
                    var emojis = new List<string>();
                    if (rankDetail.GetValueOrDefault(rank, []).Count == 0) continue;
                    foreach (var h in rankDetail[rank])
                    {
                        emojis.Add(h.Emoji);
                    }
                    messages += $"第 {rank} 名：{string.Join(',', emojis)}\n";
                }
                await SendMessageAsync(messages);
                // 发放奖励
                await DistributeRewardsAsync(rankDetail);
            }
            else
            {
                await SendMessageAsync("比赛结束！所有马匹都死亡，没有赢家。");
            }
        }

        private async Task DistributeRewardsAsync(Dictionary<uint, List<Horse>> rankDetail)
        {
            var rewardMultipliers = new Dictionary<uint, long>
            {
                { 1, 5 }, // 第一名奖励 *5
                { 2, 3 }, // 第二名奖励 *3
                { 3, 2 }  // 第三名奖励 *2
            };

            var hasWinner = false;
            var chain = MessageBuilder.Group(_groupUin);
            for (uint rank = 1; rank <= 3; rank++)
            {
                var emojis = new List<string>();
                if (rankDetail.GetValueOrDefault(rank, []).Count == 0) continue;
                long multiplier = rewardMultipliers[rank];
                foreach (var h in rankDetail[rank])
                {
                    var betsOnHorse = _bets.Values.Where(b => b.HorseId == h.Id).ToList();
                    foreach (var bet in betsOnHorse)
                    {
                        long reward = bet.Amount * multiplier;
                        
                        // 使用using语句确保Database实例被正确释放
                        using (var database = new Database())
                        {
                            var (prePoints, preLevel) = await database.GetPlayerInfoAsync(_groupUin, bet.UserUin);
                            await PlayerManager.AddPointsAsync(_groupUin, bet.UserUin, reward);
                            var (points, level) = await database.GetPlayerInfoAsync(_groupUin, bet.UserUin);
                            var levelUpCount = level - preLevel;
                            if (levelUpCount > 0)
                            {
                                chain.Mention(bet.UserUin).Text($"下注了 {h.Emoji}，获得第 {rank} 名，奖励 {reward} 积分并自动升 {levelUpCount} 级！当前等级 {level}，剩余积分 {points}。");
                            }
                            else 
                            {
                                chain.Mention(bet.UserUin).Text($"下注了 {h.Emoji}，获得第 {rank} 名，奖励 {reward} 积分！当前等级 {level}，积分 {points}。");
                            }
                        }
                        chain.Text("\n");
                        hasWinner = true;
                    }
                }
            }
            if (!hasWinner)
            {
                await SendMessageAsync($"没有玩家获胜");
            }
            else
            {
                await SendMessageAsync(chain);
            }
        }

        private async Task SendRaceStatusAsync(bool sendTrackNumber = false)
        {
            var currentRoundMessage = $"当前回合：{currentRound}";
            var skillMessages = string.Join("\n", _skillMessages);
            var trackNumber = 1;
            var status = string.Join("\n", _horses.Select(h =>
            {
                var emoji = h.IsDead ? "💀" : h.Emoji;
                var track = new string('_', 20 - h.Position) + emoji + new string('_', h.Position);
                if (sendTrackNumber)
                {
                    track += $" {trackNumber}";
                    trackNumber += 1;
                }
                return $"{track}";
            }));
            await SendMessageAsync($"{currentRoundMessage}\n{skillMessages}\n{status}");
        }

        public async Task PlaceBetAsync(uint userUin, int horseId, long amount)
        {
            if (currentRound > 0)
            {
                await SendMessageAsync("比赛已经开始，无法继续下注！");
                return;
            }
            if (horseId < 1 || horseId > 10)
            {
                await SendMessageAsync("马编号无效，请输入1-10之间的数字。");
                return;
            }

            if (amount <= 0 || !await PlayerManager.DeductPointsAsync(_groupUin, userUin, amount))
            {
                await SendMessageAsync("下注金额无效或积分不足！");
                return;
            }

            _bets[userUin] = new Bet { UserUin = userUin, HorseId = horseId, Amount = amount };
            await SendMessageAsync($"下注成功！你已为{_horses[horseId - 1].Emoji}下注{amount}积分。");
        }

        private async Task SendMessageAsync(string message)
        {
            var chain = MessageBuilder.Group(_groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }

        private async Task SendMessageAsync(MessageBuilder chain)
        {
            await _context.SendMessage(chain.Build());
        }
    }
}