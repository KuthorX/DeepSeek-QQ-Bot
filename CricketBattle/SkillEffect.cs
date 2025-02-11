
namespace QQBotCSharp.CricketBattle;

public static class SkillsEffects
{
    public static string HeHa(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = attacker.Attack + gameState.Random.Next(-5, 6);
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health); // 确保生命值不为负数
        return $"{attacker.Name} 对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string FeiTi(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = (int)(attacker.Attack * 1.5f) + gameState.Random.Next(-5, 6);
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        return $"{attacker.Name} 使用 飞踢！对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string LieKong(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = (int)(attacker.Health * 0.3f) + gameState.Random.Next(-5, 6);
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        return $"{attacker.Name} 使用 裂空！对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string FanGun(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 翻滚效果实现比较复杂，需要状态记录，这里简化为一次性效果，实际可以考虑回合状态
        // 这里只是简单输出描述，实际效果需要更完善的状态管理
        return $"{attacker.Name} 使用 翻滚！免疫下回合受到的伤害 (效果未完全实现)"; //  需要完善回合状态和免疫逻辑
    }

    public static string ChiTaoTao(Cricket attacker, Cricket defender, GameState gameState)
    {
        int healAmount = (int)(attacker.Health * 0.25f) + gameState.Random.Next(-8, 9);
        attacker.Health += healAmount;
        return $"{attacker.Name} 吃个桃桃！回复 {healAmount} 点生命值！";
    }

    public static string WanFuMoDi(Cricket attacker, Cricket defender, GameState gameState)
    {
        attacker.Attack = (int)(attacker.Attack * 1.1f); // 攻击力+10%
        return $"{attacker.Name} 使用 万夫莫敌！攻击力提升！";
    }

    public static string PanShi(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 磐石效果也需要回合状态，简化处理，仅输出描述
        // 实际需要回合状态来记录下回合的防御和攻击力提升
        return $"{attacker.Name} 使用 磐石！下回合受到伤害降低，攻击力提升 (效果未完全实现)"; // 需要完善回合状态
    }

    public static string QiXingDuoQiao(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = attacker.Attack + gameState.Random.Next(-5, 6);
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        int healAmount = (int)(attacker.Attack * 0.5f);
        attacker.Health += healAmount;
        return $"{attacker.Name} 使用 七星夺窍！对 {defender.Name} 造成 {damage} 伤害，并回复自身 {healAmount} 生命值！";
    }

    public static string XuYingBu(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 虚影步 闪避效果也需要状态管理，这里简化输出描述
        // 实际需要闪避判定逻辑，可以在攻击时增加闪避概率判断
        return $"{attacker.Name} 使用 虚影步！增加了闪避能力 (效果未完全实现)"; // 需要完善闪避判定
    }

    public static string TianFa(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 天罚 随机死亡，这里简单实现为直接杀死 defender，可以根据需要扩展为更复杂的随机目标
        defender.Health = 0;
        return $"{attacker.Name} 使用 天罚！{defender.Name} 立即死亡！";
    }
}