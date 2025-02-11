namespace QQBotCSharp.CricketBattle;
using System;
using System.Collections.Generic;

public class GameState
{
    public Cricket Cricket1 { get; set; }
    public Cricket Cricket2 { get; set; }
    // 使用字典存储每个玩家的下注信息，Key为玩家Uin (uint)，Value为下注积分
    public Dictionary<uint, int> Cricket1Bets { get; set; } = new Dictionary<uint, int>(); // 修改 Key 类型为 uint
    public Dictionary<uint, int> Cricket2Bets { get; set; } = new Dictionary<uint, int>(); // 修改 Key 类型为 uint
    public bool IsBettingPhase { get; set; } = false;
    public bool IsFightingPhase { get; set; } = false;
    public DateTime BettingEndTime { get; set; }
    public bool GameStarted { get; set; } = false; // 标记游戏是否已经开始
    public string GameResult { get; set; } = ""; // 记录游戏结果
    public int TurnCount { get; set; } = 0; // 回合计数
    public Random Random { get; } = new Random(); // 统一的随机数生成器

    public GameState()
    {
        // 初始化一些默认状态
    }
}