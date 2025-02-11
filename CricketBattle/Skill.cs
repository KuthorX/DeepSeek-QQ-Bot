namespace QQBotCSharp.CricketBattle;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Func<Cricket, Cricket, GameState, string> Effect { get; set; } // 使用委托 (Func) 表示技能效果

    public Skill(int id, string name, string description, Func<Cricket, Cricket, GameState, string> effect)
    {
        Id = id;
        Name = name;
        Description = description;
        Effect = effect;
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}