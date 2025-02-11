namespace QQBotCSharp.CricketBattle;

public class Cricket
{
    public string Name { get; set; } // 蛐蛐名称 (可以随机生成，或者玩家自定义)
    public int Attack { get; set; }
    public int Health { get; set; }
    public List<Skill> Skills { get; set; } = new List<Skill>(); // 技能列表

    public Cricket(string name, int attack, int health)
    {
        Name = name;
        Attack = attack;
        Health = health;
    }

    public override string ToString()
    {
        return $"名称: {Name}, 攻击力: {Attack}, 生命值: {Health}, 技能: {string.Join(", ", Skills.Select(s => s.Name))}";
    }
}