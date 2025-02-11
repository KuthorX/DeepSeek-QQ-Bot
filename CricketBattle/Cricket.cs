namespace QQBotCSharp.CricketBattle;

public class Cricket
{
    public string Name { get; set; }
    public int Attack { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; } // 添加最大生命值属性
    public List<Skill> Skills { get; set; } = new List<Skill>();
    public bool IsImmuneNextTurn { get; set; } = false; // 用于 "翻滚" 技能
    public float DamageReductionNextTurn { get; set; } = 0f; // 用于 "磐石" 技能，伤害减免百分比
    public float AttackBuffPercentageNextTurn { get; set; } = 0f; // 用于 "磐石" 和 "万夫莫敌" 等技能，攻击力提升百分比
    public float Evasion { get; set; } = 0f; // 用于 "虚影步" 技能，闪避率

    public Cricket(string name, int attack, int health)
    {
        Name = name;
        Attack = attack;
        Health = health;
        MaxHealth = health; // 初始化最大生命值
    }

    public override string ToString()
    {
        return $"名称: {Name}, 攻击力: {Attack}, 生命值: {Health}, 技能: {string.Join(", ", Skills.Select(s => s.Name))}";
    }
}