using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Lagrange.Core.Common.Entity;

namespace QQBotCSharp;

public class MLHandler
{
    private readonly BotContext _context;
    private readonly string _mlHost;
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // 静态计数器，用于跟踪当前正在进行的下载任务数量
    private static int _currentDownloadTasks = 0;
    
    // 最大并发下载任务数
    private const int MAX_CONCURRENT_DOWNLOADS = 3;
    
    // 静态集合，用于跟踪正在下载的manga_id
    private static readonly HashSet<int> _downloadingMangaIds = new HashSet<int>();
    
    public MLHandler(BotContext context, string mlHost)
    {
        _context = context;
        _mlHost = mlHost;
    }
    
    /// <summary>
    /// 处理ML指令
    /// </summary>
    /// <param name="args">指令参数</param>
    /// <param name="groupUin">群号</param>
    /// <param name="senderUin">发送者QQ号</param>
    /// <returns>处理结果</returns>
    public async Task HandleMLCommandAsync(string[] args, uint groupUin, uint senderUin)
    {
        // 检查参数
        if (args.Length < 1 || !int.TryParse(args[0], out int mlId))
        {
            await SendMessageAsync(groupUin, "指令格式错误，正确格式：ML {数字编号}");
            return;
        }
        
        try
        {
            var hasDownloading = false;
            // 检查是否已经有相同ID的下载任务
            lock (_downloadingMangaIds)
            {
                if (_downloadingMangaIds.Contains(mlId))
                {
                    hasDownloading = true;
                    return;
                }
            }
            if (hasDownloading)
            {
                await SendMessageAsync(groupUin, $"已有相同ID({mlId})的下载任务正在进行中，请勿重复下载");
            }

            // 检查当前下载任务数量是否已达到最大值
            if (System.Threading.Interlocked.CompareExchange(ref _currentDownloadTasks, 0, 0) >= MAX_CONCURRENT_DOWNLOADS)
            {
                await SendMessageAsync(groupUin, $"当前下载任务数已达到上限({MAX_CONCURRENT_DOWNLOADS}个)，请稍后再试");
                return;
            }
            
            // 增加当前下载任务计数并添加到正在下载的ID集合中
            System.Threading.Interlocked.Increment(ref _currentDownloadTasks);
            lock (_downloadingMangaIds)
            {
                _downloadingMangaIds.Add(mlId);
            }
            
            // 通知用户开始下载
            var startChain = MessageBuilder.Group(groupUin)
                .Mention(senderUin)
                .Text("开始下载，如果下载时间超过120秒则会自动终止下载；如果图片超过 100 张，可能实际并没有上传成功，可以多试几次");
            await _context.SendMessage(startChain.Build());
            
            // 创建WebSocket客户端
            using var ws = new ClientWebSocket();
            // 确保URL格式正确，不要在_mlHost后面加多余的斜杠
            var serverUri = new Uri($"ws://{_mlHost.TrimEnd('/')}/ws/ml");
            
            Console.WriteLine($"正在连接到WebSocket服务器: {serverUri}");
            
            // 连接到WebSocket服务器
            await ws.ConnectAsync(serverUri, CancellationToken.None);
            
            // 发送下载请求
            var requestData = JsonSerializer.Serialize(new { id = mlId });
            var requestBytes = Encoding.UTF8.GetBytes(requestData);
            await ws.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            // 接收服务器响应
            var buffer = new byte[4096];
            string filePath = null;
            bool downloadCompleted = false;
            
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
                
                // 处理接收到的消息
                var jsonResponse = Encoding.UTF8.GetString(buffer, 0, result.Count);
                using var responseDoc = JsonDocument.Parse(jsonResponse);
                var root = responseDoc.RootElement;
                
                // 获取状态
                if (root.TryGetProperty("status", out var statusElement))
                {
                    string status = statusElement.GetString();
                    string message = "";
                    
                    if (root.TryGetProperty("message", out var messageElement))
                    {
                        message = messageElement.GetString();
                    }
                    
                    // 根据状态处理
                    switch (status)
                    {
                        case "error":
                            // 发送错误消息
                            await SendMessageAsync(groupUin, $"下载错误: {message}");
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return;
                        
                        case "already_downloading":
                            // 处理已经在下载中的情况
                            await SendMessageAsync(groupUin, $"已有相关下载任务，请勿重复下载");
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return;
                            
                        case "success":
                            // 获取文件路径
                            if (root.TryGetProperty("filePath", out var filePathElement))
                            {
                                filePath = filePathElement.GetString();
                                downloadCompleted = true;
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            }
                            break;
                            
                        case "downloading":
                        case "compressing":
                            // 可以选择发送进度消息，但为了避免刷屏，这里不发送
                            Console.WriteLine($"下载状态: {status}, 消息: {message}");
                            break;
                    }
                }
            }
            
            // 如果下载完成，上传文件到群
            if (downloadCompleted && !string.IsNullOrEmpty(filePath))
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    await SendMessageAsync(groupUin, $"文件不存在");
                    return;
                }
                
                var uploadOk = await _context.GroupFSUpload(groupUin, new FileEntity(filePath), "/");
                var chain = MessageBuilder.Group(groupUin)
                        .Mention(senderUin);

                if (!uploadOk)
                {
                    chain.Text("上传失败");
                }
                else
                {
                    chain.Text("下载完成。如果图片超过 100 张，可能实际并没有上传成功，可以多试几次");
                }

                await _context.SendMessage(chain.Build());
            }
            else if (!downloadCompleted)
            {
                // 如果WebSocket连接关闭但没有成功下载
                await SendMessageAsync(groupUin, "下载未完成，可能是超时或服务器错误");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ML指令处理异常: {ex.Message}");
            await SendMessageAsync(groupUin, $"处理请求时发生错误：{ex.Message}");
        }
        finally
        {
            // 无论成功还是失败，都减少当前下载任务计数
            System.Threading.Interlocked.Decrement(ref _currentDownloadTasks);
            
            // 从正在下载的ID集合中移除
            lock (_downloadingMangaIds)
            {
                _downloadingMangaIds.Remove(mlId);
            }
        }
    }
    
    private async Task SendMessageAsync(uint groupUin, string message)
    {
        var chain = MessageBuilder.Group(groupUin).Text(message);
        await _context.SendMessage(chain.Build());
    }
    
}