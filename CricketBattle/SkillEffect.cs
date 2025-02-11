namespace QQBotCSharp.CricketBattle;

public static class SkillsEffects
{
    public static string HeHa(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = attacker.Attack + gameState.Random.Next(-5, 6);
        // 添加闪避判定
        if (gameState.Random.NextDouble() < defender.Evasion)
        {
            return $"{defender.Name} 闪避了 {attacker.Name} 的攻击！";
        }
        damage = CalculateDamageAfterReduction(damage, defender.DamageReductionNextTurn); // 应用伤害减免
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        return $"{attacker.Name} 对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string FeiTi(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = (int)(attacker.Attack * 1.5f) + gameState.Random.Next(-5, 6);
        // 添加闪避判定
        if (gameState.Random.NextDouble() < defender.Evasion)
        {
            return $"{defender.Name} 闪避了 {attacker.Name} 的攻击！";
        }
        damage = CalculateDamageAfterReduction(damage, defender.DamageReductionNextTurn); // 应用伤害减免
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        return $"{attacker.Name} 使用 飞踢！对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string LieKong(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 修改为基于 defender.Health 计算伤害
        int damage = (int)(defender.Health * 0.3f) + gameState.Random.Next(-5, 6);
        // 添加闪避判定
        if (gameState.Random.NextDouble() < defender.Evasion)
        {
            return $"{defender.Name} 闪避了 {attacker.Name} 的攻击！";
        }
        damage = CalculateDamageAfterReduction(damage, defender.DamageReductionNextTurn); // 应用伤害减免
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        return $"{attacker.Name} 使用 裂空！对 {defender.Name} 造成 {damage} 点伤害！";
    }

    public static string FanGun(Cricket attacker, Cricket defender, GameState gameState)
    {
        attacker.IsImmuneNextTurn = true; // 标记下回合免疫
        return $"{attacker.Name} 使用 翻滚！下回合将免疫受到的伤害！";
    }

    public static string ChiTaoTao(Cricket attacker, Cricket defender, GameState gameState)
    {
        // 修正随机值范围为 -8 到 +7
        int healAmount = (int)(attacker.MaxHealth * 0.25f) + gameState.Random.Next(-8, 8);
        int actualHeal = Math.Min(healAmount, attacker.MaxHealth - attacker.Health); // 限制不超过最大生命值
        attacker.Health += actualHeal;
        return $"{attacker.Name} 吃个桃桃！回复 {actualHeal} 点生命值！";
    }

    public static string WanFuMoDi(Cricket attacker, Cricket defender, GameState gameState)
    {
        attacker.AttackBuffPercentageNextTurn += 0.10f; // 添加攻击力 buff，持续到下回合开始
        return $"{attacker.Name} 使用 万夫莫敌！下回合攻击力将提升 10%！";
    }

    public static string PanShi(Cricket attacker, Cricket defender, GameState gameState)
    {
        attacker.DamageReductionNextTurn += 0.5f; // 添加伤害减免 buff，持续到下回合
        attacker.AttackBuffPercentageNextTurn += 0.05f; // 添加攻击力 buff，持续到下回合
        return $"{attacker.Name} 使用 磐石！下回合受到伤害降低 50%，攻击力提升 5%！";
    }

    public static string QiXingDuoQiao(Cricket attacker, Cricket defender, GameState gameState)
    {
        int damage = attacker.Attack + gameState.Random.Next(-5, 6);
        // 添加闪避判定
        if (gameState.Random.NextDouble() < defender.Evasion)
        {
            return $"{defender.Name} 闪避了 {attacker.Name} 的攻击！";
        }
        damage = CalculateDamageAfterReduction(damage, defender.DamageReductionNextTurn); // 应用伤害减免
        defender.Health -= damage;
        defender.Health = Math.Max(0, defender.Health);
        int healAmount = (int)(attacker.Attack * 0.5f);
        int actualHeal = Math.Min(healAmount, attacker.MaxHealth - attacker.Health); // 限制不超过最大生命值
        attacker.Health += actualHeal;
        return $"{attacker.Name} 使用 七星夺窍！对 {defender.Name} 造成 {damage} 伤害，并回复自身 {actualHeal} 生命值！";
    }

    public static string XuYingBu(Cricket attacker, Cricket defender, GameState gameState)
    {
        attacker.Evasion += 0.08f; // 增加 8% 闪避率
        return $"{attacker.Name} 使用 虚影步！闪避率提升 8%！";
    }

    public static string TianFa(Cricket attacker, Cricket defender, GameState gameState)
    {
        Cricket target;
        string targetName;

        // 50% 概率劈到自己，50% 概率劈到对方
        if (gameState.Random.Next(2) == 0) // 0 代表劈到自己 (attacker)
        {
            target = attacker;
            targetName = attacker.Name;
        }
        else // 1 代表劈到对方 (defender)
        {
            target = defender;
            targetName = defender.Name;
        }

        target.Health = 0; // 立即死亡
        return $"{attacker.Name} 使用 天罚！{targetName} 遭受天谴，立即死亡！";
    }

    // 辅助方法：计算伤害减免后的伤害
    private static int CalculateDamageAfterReduction(int damage, float reductionPercentage)
    {
        return (int)(damage * (1 - reductionPercentage));
    }
}