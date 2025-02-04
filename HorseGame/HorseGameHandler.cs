using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace QQBotCSharp.HorseGame
{
    public class HorseGameHandler
    {
        public static HashSet<string> HorseGameCommand = ["开始赛马", "赛马下注", "查询积分", "赛马签到"];
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
                default:
                    await SendMessageAsync(groupUin, "未知指令");
                    break;
            }
        }

        private async Task SendMessageAsync(uint groupUin, string message)
        {
            var chain = MessageBuilder.Group(groupUin).Text(message);
            await _context.SendMessage(chain.Build());
        }
    }
}