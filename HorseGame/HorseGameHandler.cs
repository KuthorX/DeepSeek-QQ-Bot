using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace QQBotCSharp.HorseGame
{
    public class HorseGameHandler
    {
        public static HashSet<string> HorseGameCommand = ["赛马游戏", "开始赛马", "赛马下注", "查询积分", "赛马签到", "查询排名", "赛马乞讨", "觉醒超级马", "赛马购买等级"];
        private readonly BotContext _context;
        private readonly GameManager _gameManager;
        private readonly PlayerManager _playerManager;

        public HorseGameHandler(BotContext context)
        {
            _context = context;
            _gameManager = new GameManager(context);
            _playerManager = new PlayerManager(context);
        }

        public bool CheckIsRunning(uint groupUin)
        {
            return _gameManager.IsRunning(groupUin);
        }

        public async Task HandleCommandAsync(string command, string[] args, uint groupUin, uint userUin)
        {
            switch (command)
            {
                case "赛马游戏":
                    await ExplainRace(groupUin);
                    break;
                case "开始赛马":
                    await _gameManager.StartRaceAsync(groupUin);
                    break;
                case "赛马下注":
                    if (args.Length == 2 && int.TryParse(args[0], out int horseId) && int.TryParse(args[1], out int betAmount))
                    {
                        await _gameManager.PlaceBetAsync(groupUin, userUin, horseId, betAmount);
                    }
                    else
                    {
                        await SendMessageAsync(groupUin, "指令格式错误，正确格式：赛马下注 {马编号} {赌注}");
                    }
                    break;
                case "查询积分":
                    await _playerManager.QueryPointsAsync(groupUin, userUin);
                    break;
                case "赛马签到":
                    await _playerManager.SignInAsync(groupUin, userUin);
                    break;
                case "赛马乞讨":
                    await _playerManager.BegAsync(groupUin, userUin);
                    break;
                case "查询排名":
                    await _playerManager.GetGroupMemberRankingAsync(groupUin);
                    break;
                case "觉醒超级马":
                    await _gameManager.AwakeSpecialHorse(groupUin);
                    break;
                case "赛马购买等级":
                    if (args.Length == 1 && int.TryParse(args[0], out int levelCount))
                    {
                        await _playerManager.BuyLevelAsync(groupUin, userUin, levelCount);
                    }
                    else
                    {
                        await SendMessageAsync(groupUin, "指令格式错误，正确格式：赛马购买等级 {等级数}");
                    }
                    break;
                default:
                    await SendMessageAsync(groupUin, "未知指令");
                    break;
            }
        }
        
        public async Task ExplainRace(uint groupUin)
        {
            var commandsMsg = string.Join(", ", HorseGameCommand);
            await SendMessageAsync(groupUin, $"赛马游戏\n支持指令：{commandsMsg}\n最近更新：\n闪电、火山范围加强：范围增大；疾风冲刺3-5改为2-4");
        }

        private async Task SendMessageAsync(uint groupUin, string message)
        {
            var chain = MessageBuilder.Group(groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }
    }
}