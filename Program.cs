// See https://aka.ms/new-console-template for more information
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using System.Collections.Concurrent;
using QQBotCSharp.HorseGame;
using QQBotCSharp.CricketBattle;

namespace QQBotCSharp;

record Message(string Role, string Content);

public class QQBot
{
    // 配置信息
    private static HashSet<uint> _allowedGroupIds = [];
    private static HashSet<uint> _mlAllowedGroupIds = [];
    private static uint _botUin = 0;
    private static string _apiKey = "";
    private const string ConfigPath = "config.json";
    private static string ApiUrl = "https://api.deepseek.com/v1/chat/completions";
    private const int MaxContextChars = 3000; // 根据API限制调整
    private const string TruncateWarning = "（系统提示：因对话历史过长，已自动截断早期内容）";
    private const int MaxSegmentLength = 1000; // 单条消息最大长度
    private const int SendIntervalMs = 300;    // 分段发送间隔（毫秒）
    private static readonly ConcurrentDictionary<uint, (CancellationTokenSource cts, int retryCount)> _retryTasks = new();
    private const int MaxRetryCount = 100; // 最大重试次数
    private const int RetryInterval = 60_000; // 重试间隔60秒
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // 状态管理
    private static ConcurrentDictionary<string, List<Message>> _conversations = new();
    private static ConcurrentDictionary<string, bool> _processingFlags = new();
    private static BotKeystore _keyStore = new();
    private static readonly BotDeviceInfo _deviceInfo = new()
    {
        Guid = Guid.NewGuid(),
        MacAddress = GenRandomBytes(6),
        DeviceName = $"Lagrange-52D02F",
        SystemKernel = "Windows 10.0.19042",
        KernelVersion = "10.0.19042.0"
    };
    private static readonly string _conversationsPath = "conversations.json";
    private static readonly ConcurrentDictionary<uint, HorseGameHandler> groupHorseGameHandler = new(); 
    private static readonly ConcurrentDictionary<uint, CricketBattleGameManager> groupCricketBattleGameManager = new();
    private static MLHandler? _mlHandler = null; 
    private static bool _handleGamesMode = true; // 默认处理游戏模式

    public static byte[] GenRandomBytes(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than 0.", nameof(length));
        }

        byte[] randomBytes = new byte[length];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return randomBytes;
    }

    // 新增配置文件加载方法
    private static MyBotConfig LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            CreateSampleConfig();
            throw new FileNotFoundException($"配置文件 {ConfigPath} 不存在，已生成示例配置文件，请修改后重新运行");
        }

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<MyBotConfig>(json) 
            ?? throw new InvalidDataException("配置文件格式不正确");

        ValidateConfig(config);
        return config;
    }

    // 创建示例配置文件
    private static void CreateSampleConfig()
    {
        var sample = new MyBotConfig
        {
            BotUin = 123456789,
            ApiKey = "your-api-key-here",
            AllowedGroupIds = new List<uint> { 987654321, 123456789 }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(sample, options));
    }

    // 配置验证
    private static void ValidateConfig(MyBotConfig config)
    {
        if (config.BotUin == 0)
            throw new ArgumentException("BotUin 不能为0");

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new ArgumentException("ApiKey 不能为空");

        if (config.AllowedGroupIds.Count == 0)
            throw new ArgumentException("至少需要配置一个允许的群组ID");
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("正在加载配置文件...");
        var config = LoadConfig();

        try
        {
            _botUin = config.BotUin;
            _apiKey = config.ApiKey;
            _allowedGroupIds = new HashSet<uint>(config.AllowedGroupIds);
            _mlAllowedGroupIds = new HashSet<uint>(config.MlAllowedGroupIds);

            Console.WriteLine($"已加载配置：BotUin={_botUin}, 允许群组={string.Join(",", _allowedGroupIds)}, ML主机={config.MlHost}, _mlAllowedGroupIds={string.Join(",", _mlAllowedGroupIds)}");

            AnimalSpeedData.LoadDataFromJson();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置文件加载失败: {ex.Message}");
            return;
        }

        Console.WriteLine("机器人准备启动");

        // 加载会话历史
        LoadConversations();

        var filePath = "keystore.json";
        if (File.Exists(filePath))
        {
            var readString = File.ReadAllText(filePath);
            var keystore = JsonSerializer.Deserialize<BotKeystore>(readString);
            _keyStore = keystore!;
        }

        var bot = BotFactory.Create(new BotConfig(), _deviceInfo, _keyStore);
        if (File.Exists(filePath))
        {
            Console.WriteLine("自动登录中");
            await bot.LoginByPassword();
        }
        else
        {
            Console.WriteLine("请先扫码登录");
            var qrCode = await bot.FetchQrCode();
            await QrCodeHandler.SaveQrCodeAsPng(qrCode.Value.QrCode, "generate_qrcode.png");
            await bot.LoginByQrCode();
        }

        _keyStore = bot.UpdateKeystore();
        var jsonString = JsonSerializer.Serialize(_keyStore, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("keystore.json", jsonString);

        Console.WriteLine("登录成功，注册事件");

        // 初始化ML处理器
        _mlHandler = new MLHandler(bot, config.MlHost);

        bot.Invoker.OnGroupMessageReceived += HandleGroupMessageEvent;
        bot.Invoker.OnFriendMessageReceived += HandlePrivateMessage;
    }

    private static void LoadConversations()
    {
        try
        {
            if (File.Exists(_conversationsPath))
            {
                var json = File.ReadAllText(_conversationsPath);
                var data = JsonSerializer.Deserialize<ConcurrentDictionary<string, List<Message>>>(json);
                _conversations = data ?? new ConcurrentDictionary<string, List<Message>>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载会话历史失败: {ex.Message}");
        }
    }

    private static void SaveConversations()
    {
        try
        {
            var snapshot = _conversations.ToArray();
            var dict = snapshot.ToDictionary(pair => pair.Key, pair => pair.Value);
            var json = JsonSerializer.Serialize(dict);
            File.WriteAllText(_conversationsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存会话历史失败: {ex.Message}");
        }
    }

    private static async void HandlePrivateMessage(BotContext bot, FriendMessageEvent e)
    {
        var chain = new MessageChain(e.Chain.FriendUin, bot.BotName!, e.Chain.FriendInfo!.Uid, bot.BotUin)
        {
            new TextEntity("暂不支持私聊，请在群组中@我使用")
        };
        await bot.SendMessage(chain);
    }

    private static async void HandleGroupMessageEvent(BotContext bot, GroupMessageEvent e)
    {
        var chain = e.Chain;
        var groupUin = chain.GroupUin;
        var userUin = chain.FriendUin;

        // 群聊白名单检查
        if (!groupUin.HasValue || !_allowedGroupIds.Contains(groupUin.Value))
        {
            return;
        }

        var isMention = false;
        foreach (var entity in chain)
        {
            if (entity is MentionEntity mentionEntity)
            {
                if (mentionEntity.Uin == _botUin)
                {
                    isMention = true;
                    break;
                }
            }
        }
        if (!isMention)
        {
            return;
        }

        // 提取消息文本
        var message = ExtractMessageText(chain);
        Console.WriteLine("收到指令：" + message);

        if (string.IsNullOrWhiteSpace(message)) return;


        // 处理ML指令
        if (message.StartsWith("ML ") || message.StartsWith("ml "))
        {
            // 群聊白名单检查
            if (!groupUin.HasValue || !_mlAllowedGroupIds.Contains(groupUin.Value))
            {
                await SendTempMessage(bot, chain, "不支持的指令");
                return;
            }
            if (_mlHandler != null)
            {
                string[] args = message.Split(' ', 2)[1].Trim().Split(' ');
                await _mlHandler.HandleMLCommandAsync(args, groupUin!.Value, userUin);
                return;
            }
        }

        foreach (var command in HorseGameHandler.HorseGameCommand)
        {
            if (message.StartsWith(command))
            {
                var handler = groupHorseGameHandler.GetOrAdd(groupUin!.Value, new HorseGameHandler(bot));
                await handler.HandleCommandAsync(command, message.Split(" ").Skip(1).ToArray(), groupUin!.Value, userUin);
                return;
            }
        }
        var curHandler = groupHorseGameHandler.GetOrAdd(groupUin!.Value, new HorseGameHandler(bot));
        if (curHandler.CheckIsRunning(groupUin!.Value))
        {
            await SendTempMessage(bot, chain, "赛马中，暂不支持其他模式。");
            return;
        }

        foreach (var command in CricketBattleGameManager.CricketGameCommand)
        {
            if (message.StartsWith(command))
            {
                var handler = groupCricketBattleGameManager.GetOrAdd(groupUin!.Value, new CricketBattleGameManager(bot, groupUin!.Value));
                await handler.ProcessCommand(message, userUin, groupUin!.Value);
                return;
            }
        }

        var curC = groupCricketBattleGameManager.GetOrAdd(groupUin!.Value, new CricketBattleGameManager(bot, groupUin!.Value));
        if (curC.IsGameStarted())
        {
            await SendTempMessage(bot, chain, "斗蛐蛐中，暂不支持其他模式。");
            return;
        }

        await SendTempMessage(bot, chain, "不支持的指令");
        return;

        // 新增重试状态检查
        if (IsGroupInRetry(groupUin.Value))
        {
            await SendTempMessage(bot, chain, "服务暂时不可用，自动恢复中请稍候...");
            return;
        }

        // 获取上下文信息
        var sessionKey = $"{groupUin}_{userUin}";
        Console.WriteLine($"sessionKey {sessionKey}");

        // 处理并发请求
        if (_processingFlags.TryGetValue(sessionKey, out var processing) && processing)
        {
            await SendResponse(bot, chain, "请等待上一个请求处理完成");
            return;
        }

        // 处理重置指令
        if (message.Trim().Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            _conversations.TryRemove(sessionKey, out _);
            SaveConversations();
            await SendResponse(bot, chain, "会话已重置");
            return;
        }

        // 开始处理
        Console.WriteLine($"请求 api");
        _processingFlags[sessionKey] = true;
        try
        {
            // 构建会话历史
            var history = _conversations.GetOrAdd(sessionKey, _ => new List<Message>());
            history.Add(new Message("user", message));

            // 调用API
            var (response, code) = await CallDeepSeekApi(history);
            if (code == 0)
            {
                history.Add(new Message("assistant", response!));
                SaveConversations();
            }

            // 发送响应
            await SendResponse(bot, chain, response!);
        }
        catch
        {
            StartRetryMonitor(bot, (uint)groupUin);
            await SendResponse(bot, chain, "服务暂时不可用，正在重试访问...");
        }
        finally
        {
            _processingFlags[sessionKey] = false;
        }
    }

    // 新增状态检查方法
    private static bool IsGroupInRetry(uint groupUin)
    {
        return _retryTasks.TryGetValue(groupUin, out var entry) &&
            !entry.cts.IsCancellationRequested &&
            entry.retryCount < MaxRetryCount;
    }


    // 启动重试监控
    private static void StartRetryMonitor(BotContext bot, uint groupUin)
    {
        _retryTasks.AddOrUpdate(groupUin,
            key =>
            {
                var cts = new CancellationTokenSource();
                Task.Run(() => RetryLoop(bot, key, cts.Token));
                Console.WriteLine($"启动重试监控：群{groupUin}");
                return (cts, 0);
            },
            (key, existing) =>
            {
                if (existing.retryCount >= MaxRetryCount)
                {
                    Console.WriteLine($"已达最大重试次数：群{groupUin}");
                    return existing;
                }

                if (existing.cts.IsCancellationRequested)
                {
                    existing.cts.Dispose();
                    var newCts = new CancellationTokenSource();
                    Task.Run(() => RetryLoop(bot, key, newCts.Token));
                    Console.WriteLine($"重启重试监控：群{groupUin}");
                    return (newCts, existing.retryCount);
                }

                Console.WriteLine($"已有进行中的重试任务：群{groupUin}");
                return existing;
            });
    }

    // 重试循环逻辑
    private static async Task RetryLoop(BotContext bot, uint groupUin, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(RetryInterval, ct);

                // 更新重试计数器
                _retryTasks.AddOrUpdate(groupUin,
                    _ => throw new InvalidOperationException("Entry should exist"),
                    (_, existing) => (existing.cts, existing.retryCount + 1));

                Console.WriteLine($"第{_retryTasks[groupUin].retryCount}次重试：群{groupUin}");

                // 构造测试消息
                var testMessage = new List<Message>
            {
                new("user", "PING_" + Guid.NewGuid().ToString("N"))
            };

                try
                {
                    var response = await CallDeepSeekApi(testMessage);

                    // 发送恢复通知
                    var chain = MessageBuilder.Group(groupUin)
                        .Text("服务已恢复，可以继续使用");
                    await bot.SendMessage(chain.Build());

                    StopRetryMonitor(groupUin);
                    return;
                }
                catch
                {
                    if (_retryTasks[groupUin].retryCount >= MaxRetryCount)
                    {
                        Console.WriteLine($"达到最大重试次数：群{groupUin}");
                        var chain = MessageBuilder.Group(groupUin)
                            .Text("服务恢复失败，请稍后再试");
                        await bot.SendMessage(chain.Build());
                        StopRetryMonitor(groupUin);
                        return;
                    }
                }
            }
        }
        finally
        {
            StopRetryMonitor(groupUin);
        }
    }

    // 新增发送临时消息方法
    private static async Task SendTempMessage(BotContext bot, MessageChain originChain, string message)
    {
        try
        {
            var chain = MessageBuilder.Group(originChain.GroupUin!.Value)
                .Text(message);
            await bot.SendMessage(chain.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送临时消息失败: {ex.Message}");
        }
    }

    // 停止重试监控
    private static void StopRetryMonitor(uint groupUin)
    {
        if (_retryTasks.TryRemove(groupUin, out var entry))
        {
            entry.cts.Cancel();
            entry.cts.Dispose();
        }
    }


    // 辅助方法：提取消息文本
    private static string ExtractMessageText(MessageChain chain)
    {
        var builder = new StringBuilder();
        foreach (var entity in chain)
        {
            switch (entity)
            {
                case TextEntity text:
                    builder.Append(text.Text);
                    break;
            }
        }
        return builder.ToString().Trim(); // 替换实际的机器人名称
    }

    // API调用方法
    private static async Task<(string?, int)> CallDeepSeekApi(List<Message> messages)
    {
        // 创建消息副本并进行截断处理
        var (processedMessages, isTruncated) = ProcessMessages(messages);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var request = new
        {
            model = "deepseek-chat",
            messages = processedMessages,
            temperature = 0.5,
            max_tokens = 1024,
            stream = false,
        };

        var json = JsonSerializer.Serialize(request, options);

        try
        {
            var response = await client.PostAsync(ApiUrl,
            new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return ($"请求失败：{response.StatusCode}", -1);

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
            if (result.Length == 0)
            {
                return ("未获取到有效响应", -1);
            }
            if (isTruncated)
            {
                result = $"{TruncateWarning}\n{result}";
            }
            return (result, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API请求异常: {ex.Message}");
            throw; // 抛出异常供上层处理
        }
    }

    // 新增智能分段方法
    private static List<string> SplitLongMessage(string text)
    {
        if (text.Length <= MaxSegmentLength) return new List<string> { text };

        var segments = new List<string>();
        int currentIndex = 0;

        while (currentIndex < text.Length)
        {
            int endIndex = Math.Min(currentIndex + MaxSegmentLength, text.Length);

            // 优先在段落结尾分割
            int splitIndex = text.LastIndexOf('\n', endIndex - 1, Math.Min(endIndex - currentIndex, MaxSegmentLength));
            splitIndex = splitIndex == -1 ?
                FindNaturalSplitPosition(text, currentIndex, endIndex) :
                splitIndex;

            // 找不到合适位置则硬分割
            if (splitIndex <= currentIndex)
            {
                splitIndex = currentIndex + MaxSegmentLength;
                splitIndex = Math.Min(splitIndex, text.Length);
            }

            var segment = text.Substring(currentIndex, splitIndex - currentIndex).Trim();
            segments.Add(segment);
            currentIndex = splitIndex;
        }

        return segments;
    }

    // 查找自然断句位置
    private static int FindNaturalSplitPosition(string text, int start, int end)
    {
        // 优先查找句子结束符号
        var punctuation = new[] { '。', '！', '？', '；', '\n', '.', '!', '?', ';' };
        for (int i = end - 1; i > start; i--)
        {
            if (punctuation.Contains(text[i]))
            {
                return i + 1;
            }
        }

        // 其次查找逗号
        for (int i = end - 1; i > start; i--)
        {
            if (text[i] == '，' || text[i] == ',')
            {
                return i + 1;
            }
        }

        // 最后查找空格
        for (int i = end - 1; i > start; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        return end;
    }

    // 新增消息处理方法
    private static (List<Message> messages, bool isTruncated) ProcessMessages(List<Message> messages)
    {
        var processed = new List<Message>(messages);
        var totalChars = processed.Sum(m => m.Content.Length);
        bool isTruncated = false;

        // 分步截断策略
        while (ShouldTruncate(totalChars) && processed.Count > 1)
        {
            isTruncated = true;
            var removed = processed[0];
            processed.RemoveAt(0);
            totalChars -= removed.Content.Length;
        }

        // 最后一条消息处理
        if (ShouldTruncate(totalChars) && processed.Count == 1)
        {
            isTruncated = true;
            var last = processed[0];
            var allowed = MaxContextChars - (totalChars - last.Content.Length);
            processed[0] = last with
            {
                Content = TruncateString(last.Content, Math.Max(allowed, 0))
            };
        }

        return (processed, isTruncated);
    }

    private static bool ShouldTruncate(int totalChars) => totalChars > MaxContextChars;

    private static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input[..Math.Max(maxLength - 3, 0)] + "...";
    }

    // 发送响应消息
    private static async Task SendResponse(BotContext bot, MessageChain originChain, string response)
    {
        try
        {
            var segments = SplitLongMessage(response);
            var groupUin = originChain.GroupUin!.Value;

            foreach (var segment in segments)
            {
                var chain = MessageBuilder.Group(groupUin);
                chain.Add(new ForwardEntity(originChain));
                chain.Add(new TextEntity(segment));
                await bot.SendMessage(chain.Build());

                // 非最后一条消息时添加等待提示
                if (segment != segments.Last())
                {
                    await Task.Delay(SendIntervalMs);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"消息发送失败: {ex.Message}");
            var errorChain = MessageBuilder.Group(originChain.GroupUin!.Value)
                .Text("消息发送失败，请稍后重试");
            await bot.SendMessage(errorChain.Build());
        }
    }
}


