using System.Text.Json.Serialization;

namespace QQBotCSharp;

public class MyBotConfig
{
    [JsonPropertyName("botUin")]
    public uint BotUin { get; set; }

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("allowedGroupIds")]
    public List<uint> AllowedGroupIds { get; set; } = new();
}