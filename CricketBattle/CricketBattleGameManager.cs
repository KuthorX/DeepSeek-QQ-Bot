namespace QQBotCSharp.CricketBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

public class CricketBattleGameManager
{
    public static HashSet<string> CricketGameCommand = ["开始斗蛐蛐", "蛐蛐下注", "蛐蛐签到", "蛐蛐乞讨", "查询蛐蛐积分", "查询蛐蛐排名"];
    private GameState _gameState = new GameState();
    private DatabaseManager _databaseManager = new DatabaseManager();
    private Dictionary<int, Skill> _skillLibrary = new Dictionary<int, Skill>();
    private bool _isGameRunning = false; // 防止同时开始多个游戏
    private BotContext _context;
    private uint _groupUin;

    public CricketBattleGameManager(BotContext context, uint groupUin)
    {
        _context = context;
        _groupUin = groupUin;
        InitializeSkillLibrary();
    }

    private void InitializeSkillLibrary()
    {
        _skillLibrary.Add(1, new Skill(1, "嘿哈", "造成角色攻击*100%+-5伤害", SkillsEffects.HeHa));
        _skillLibrary.Add(2, new Skill(2, "飞踢", "造成角色攻击*150%+-5伤害", SkillsEffects.FeiTi));
        _skillLibrary.Add(3, new Skill(3, "裂空", "造成角色生命值*30%+-5伤害", SkillsEffects.LieKong));
        _skillLibrary.Add(4, new Skill(4, "翻滚", "免疫下一回合受到的伤害", SkillsEffects.FanGun));
        _skillLibrary.Add(5, new Skill(5, "吃个桃桃", "回复生命值*25%+-8", SkillsEffects.ChiTaoTao));
        _skillLibrary.Add(6, new Skill(6, "万夫莫敌", "攻击力+10%", SkillsEffects.WanFuMoDi));
        _skillLibrary.Add(7, new Skill(7, "磐石", "下回合受到伤害-50%，攻击力+5%", SkillsEffects.PanShi));
        _skillLibrary.Add(8, new Skill(8, "七星夺窍", "造成角色攻击*100%+-5伤害，回复攻击*50%生命", SkillsEffects.QiXingDuoQiao));
        _skillLibrary.Add(9, new Skill(9, "虚影步", "增加闪避判定，闪避+8%", SkillsEffects.XuYingBu)); // 闪避效果比较复杂，这里先简化，实际实现可以更复杂
        _skillLibrary.Add(10, new Skill(10, "天罚", "释放时，随机1名角色立即死亡", SkillsEffects.TianFa));
    }

    public async Task ProcessCommand(string command, uint uin, uint groupUin) // 修改参数类型为 uint
    {
        command = command.Trim();

        if (command == "蛐蛐签到")
        {
            await CheckIn(uin);
        }
        else if (command == "蛐蛐乞讨")
        {
            await Beg(uin);
        }
        else if (command == "开始斗蛐蛐")
        {
            await StartGame(uin, groupUin);
        }
        else if (command.StartsWith("蛐蛐下注"))
        {
            string[] parts = command.Split(' ');
            if (parts.Length == 3 && int.TryParse(parts[1], out int cricketNumber) && int.TryParse(parts[2], out int betAmount))
            {
                await Bet(uin, cricketNumber, betAmount);
            }
            else
            {
                await SendMessageAsync("下注指令格式错误，请使用：蛐蛐下注 {1或者2} {下注积分}");
            }
        }
        else if (command == "查询蛐蛐积分") // 新增指令处理
        {
            await QueryPoints(uin);
        }
        else if (command == "查询蛐蛐排名") // 新增指令处理
        {
            await QueryRanking(groupUin);
        }
        else
        {
            await SendMessageAsync("未知指令，请使用：开始斗蛐蛐, 蛐蛐下注, 蛐蛐签到, 蛐蛐乞讨");
        }
    }

    public bool IsGameStarted()
    {
        return _gameState.GameStarted;
    }
    
    private async Task QueryPoints(uint uin)
    {
        int points = _databaseManager.GetUserPoints(uin);
        await SendMessageAsync($"你的蛐蛐积分：{points}");
    }

    private async Task QueryRanking(uint groupUin)
    {
        List<Tuple<uint, string, int>> rankingList = await _databaseManager.GetAllUsersInGroupAsync(_context, groupUin);

        if (rankingList.Count == 0)
        {
            await SendMessageAsync($"暂无排名数据。");
            return;
        }

        string rankingMessage = $"";
        for (int i = 0; i < rankingList.Count; i++)
        {
            var user = rankingList[i];
            rankingMessage += $"{i + 1}. {user.Item2}: {user.Item3}\n"; // Item2 是 Name, Item3 是 Points
        }
        await SendMessageAsync(rankingMessage.TrimEnd('\n')); // 移除末尾换行符
    }

    private async Task CheckIn(uint uin) // 修改参数类型为 uint
    {
        if (_databaseManager.CanCheckInToday(uin))
        {
            _databaseManager.UpdateUserPoints(uin, 1000);
            _databaseManager.UpdateCheckInDate(uin);
            await SendMessageAsync($"签到成功！奖励 1000 积分，当前积分：{_databaseManager.GetUserPoints(uin)}");
        }
        else
        {
            await SendMessageAsync("今日已签到，请明日再来。");
        }
    }

    private async Task Beg(uint uin) // 修改参数类型为 uint
    {
        int currentPoints = _databaseManager.GetUserPoints(uin);
        if (currentPoints == 0)
        {
            int reward = _gameState.Random.Next(100, 201);
            _databaseManager.UpdateUserPoints(uin, reward);
            await SendMessageAsync($"乞讨成功！获得 {reward} 积分，当前积分：{_databaseManager.GetUserPoints(uin)}");
        }
        else
        {
            await SendMessageAsync("你现在不缺积分，无需乞讨。");
        }
    }


    private async Task StartGame(uint uin, uint groupUin) // 修改参数类型为 uint
    {
        if (_isGameRunning)
        {
            await SendMessageAsync("当前已有游戏正在进行，请稍后再试。");
            return;
        }

        _isGameRunning = true;
        _gameState = new GameState(); // 重置游戏状态
        _gameState.GameStarted = true;

        _gameState.Cricket1 = GenerateCricket("🪲");
        _gameState.Cricket2 = GenerateCricket("🦗");

        await SendMessageAsync($"--- 蛐蛐比赛开始 ---");
        await SendMessageAsync($"蛐蛐1: {_gameState.Cricket1}");
        await SendMessageAsync($"蛐蛐2: {_gameState.Cricket2}");

        _gameState.IsBettingPhase = true;
        _gameState.BettingEndTime = DateTime.Now.AddSeconds(60);
        await SendMessageAsync($"--- 下注阶段开始 (60秒) ---  请使用 '蛐蛐下注 {{1或2}} {{积分}}' 下注");

        // 60秒后自动开始比赛
        await Task.Delay(TimeSpan.FromSeconds(60));

        if (_gameState.IsBettingPhase) // 确保下注阶段没有手动结束
        {
            _gameState.IsBettingPhase = false;
            await StartBattle();
        }
    }

    private Cricket GenerateCricket(string name)
    {
        int attack = _gameState.Random.Next(40, 51);
        int health = 500 - attack * 5;
        Cricket cricket = new Cricket(name, attack, health);

        // 随机分配两个技能
        List<int> skillKeys = _skillLibrary.Keys.ToList();
        HashSet<int> selectedSkillIds = new HashSet<int>();
        while (selectedSkillIds.Count < 2)
        {
            selectedSkillIds.Add(skillKeys[_gameState.Random.Next(skillKeys.Count)]);
        }
        foreach (int skillId in selectedSkillIds)
        {
            cricket.Skills.Add(_skillLibrary[skillId]);
        }
        return cricket;
    }

    private async Task Bet(uint uin, int cricketNumber, int betAmount) // 修改参数类型为 uint
    {
        if (!_gameState.IsBettingPhase)
        {
            await SendMessageAsync("下注已结束，比赛即将开始。");
            return;
        }

        if (DateTime.Now > _gameState.BettingEndTime)
        {
            _gameState.IsBettingPhase = false;
            await SendMessageAsync("下注时间已结束。");
            await StartBattle(); // 如果下注阶段超时，直接开始战斗
            return;
        }

        if (betAmount < 100 || betAmount > 500)
        {
            await SendMessageAsync("下注积分必须在 100 到 500 之间。");
            return;
        }

        int currentPoints = _databaseManager.GetUserPoints(uin);
        if (currentPoints < betAmount)
        {
            await SendMessageAsync("积分不足，无法下注。");
            return;
        }

        _databaseManager.UpdateUserPoints(uin, -betAmount); // 扣除下注积分

        if (cricketNumber == 1)
        {
            if (_gameState.Cricket1Bets.ContainsKey(uin)) // Cricket1Bets 的 Key 类型已改为 uint
            {
                _gameState.Cricket1Bets[uin] += betAmount; // 玩家已下注，累加下注金额
            }
            else
            {
                _gameState.Cricket1Bets.Add(uin, betAmount); // 玩家首次下注
            }
            await SendMessageAsync($"{uin} 下注 蛐蛐1，积分 {betAmount}，当前积分：{_databaseManager.GetUserPoints(uin)}");
        }
        else if (cricketNumber == 2)
        {
            if (_gameState.Cricket2Bets.ContainsKey(uin)) // Cricket2Bets 的 Key 类型已改为 uint
            {
                _gameState.Cricket2Bets[uin] += betAmount; // 玩家已下注，累加下注金额
            }
            else
            {
                _gameState.Cricket2Bets.Add(uin, betAmount); // 玩家首次下注
            }
            await SendMessageAsync($"{uin} 下注 蛐蛐2，积分 {betAmount}，当前积分：{_databaseManager.GetUserPoints(uin)}");
        }
        else
        {
            _databaseManager.UpdateUserPoints(uin, betAmount); // 下注无效，返还积分
            await SendMessageAsync("无效的蛐蛐编号，请下注 1 或 2。积分已返还。");
        }
    }


    private async Task StartBattle()
    {
        if (!_gameState.GameStarted || _gameState.IsFightingPhase) return; // 防止重复开始战斗
        _gameState.IsFightingPhase = true;
        await SendMessageAsync($"--- 比赛开始 ---");

        while (_gameState.Cricket1.Health > 0 && _gameState.Cricket2.Health > 0)
        {
            _gameState.TurnCount++;
            await SendMessageAsync($"--- 回合 {_gameState.TurnCount} ---");
            await ExecuteTurn(_gameState.Cricket1, _gameState.Cricket2);
            if (_gameState.Cricket2.Health <= 0) break; // 蛐蛐2死亡，结束战斗
            await ExecuteTurn(_gameState.Cricket2, _gameState.Cricket1);
            if (_gameState.Cricket1.Health <= 0) break; // 蛐蛐1死亡，结束战斗
            await Task.Delay(1500); // 稍微等待一下，模拟战斗过程
        }

        await EndGame();
    }


    private async Task ExecuteTurn(Cricket attacker, Cricket defender)
    {
        Skill selectedSkill = null;
        string actionMessage = "";

        if (_gameState.Random.Next(100) < 30 && attacker.Skills.Any()) // 30% 技能触发概率
        {
            selectedSkill = attacker.Skills[_gameState.Random.Next(attacker.Skills.Count)];
            actionMessage = $"{attacker.Name} 使用技能：{selectedSkill.Name}！";
        }
        else
        {
            selectedSkill = _skillLibrary[1]; // 默认普通攻击 "嘿哈"
            actionMessage = $"{attacker.Name} 使用普通攻击：{selectedSkill.Name}！";
        }
        await SendMessageAsync(actionMessage);

        string skillResult = selectedSkill.Effect(attacker, defender, _gameState); // 执行技能效果
        await SendMessageAsync(skillResult);
        await SendMessageAsync($"{attacker.Name} 生命值: {attacker.Health}, {defender.Name} 生命值: {defender.Health}");
    }


    private async Task EndGame()
    {
        _gameState.IsFightingPhase = false;
        Cricket winner = null;
        Cricket loser = null;

        if (_gameState.Cricket1.Health <= 0 && _gameState.Cricket2.Health <= 0)
        {
            _gameState.GameResult = "平局！";
            await SendMessageAsync($"--- 比赛结束 --- 平局！");
        }
        else if (_gameState.Cricket1.Health > 0)
        {
            winner = _gameState.Cricket1;
            loser = _gameState.Cricket2;
            _gameState.GameResult = $"{winner.Name} 获胜！";
            await SendMessageAsync($"--- 比赛结束 --- {winner.Name} 获胜！");
            RewardPlayers(1); // 奖励下注蛐蛐1的玩家
        }
        else
        {
            winner = _gameState.Cricket2;
            loser = _gameState.Cricket1;
            _gameState.GameResult = $"{winner.Name} 获胜！";
            await SendMessageAsync($"--- 比赛结束 --- {winner.Name} 获胜！");
            RewardPlayers(2); // 奖励下注蛐蛐2的玩家
        }

        _isGameRunning = false; // 游戏结束，允许开始新的游戏
        _gameState.GameStarted = false;
    }

    private void RewardPlayers(int winningCricketNumber)
    {
        int rewardMultiplier = 2; // 奖励倍数

        Dictionary<uint, int> winningBets = null; // 修改为 Dictionary<uint, int>
        if (winningCricketNumber == 1)
        {
            winningBets = _gameState.Cricket1Bets;
        }
        else if (winningCricketNumber == 2)
        {
            winningBets = _gameState.Cricket2Bets;
        }

        if (winningBets != null && winningBets.Count > 0)
        {
            foreach (var betEntry in winningBets)
            {
                uint playerUin = betEntry.Key; // Key 类型为 uint
                int betAmount = betEntry.Value;
                int rewardAmount = betAmount * rewardMultiplier;
                _databaseManager.UpdateUserPoints(playerUin, rewardAmount);
                SendMessageAsync($"恭喜玩家 {playerUin} 下注 蛐蛐{winningCricketNumber} 获胜，获得奖励积分 {rewardAmount}！").Wait();
            }
        }
    }


    // 模拟异步发送消息 (需要替换为实际的消息发送机制)
    public async Task SendMessageAsync(string message)
    {
        var chain = MessageBuilder.Group(_groupUin).Text(message);
        await _context.SendMessage(chain.Build());
    }
}