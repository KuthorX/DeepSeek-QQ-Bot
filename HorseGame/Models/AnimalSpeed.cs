using Newtonsoft.Json;

namespace QQBotCSharp.HorseGame;
public class AnimalSpeedData
{
    public static Dictionary<string, Dictionary<string, int>> AnimalSpeedDict { get; private set; }
    public static void LoadDataFromJson(string jsonFilePath = "AnimalSpeed.json") { string json = File.ReadAllText(jsonFilePath); AnimalSpeedDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json); }
}