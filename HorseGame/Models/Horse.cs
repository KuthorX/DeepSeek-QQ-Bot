namespace QQBotCSharp.HorseGame.Models
{
    public class Horse
    {
        public int Id { get; set; }
        public string Emoji { get; set; }
        public int Position { get; set; }
        public bool IsDead { get; set; }
        public int Speed { get; set; } = 2; // 默认速度
        public bool IsStunned { get; set; } // 是否被烟雾弹影响
        public int SwampRounds { get; set; } // 沼泽剩余回合
        public bool HasShield { get; set; } // 护盾状态
        public int OriginalSpeed { get; set; } = 2; // 初始速度（用于回春术）
        public bool SepcialHorse { get; set; } = false; // 是否特殊马

        public void Move()
        {
            if (IsDead) return;

            if (IsStunned)
            {
                IsStunned = false;
                return;
            }

            // 如果处于沼泽中，速度减1（最低为1）
            // 如果处于沼泽中且无护盾，速度减1
            int effectiveSpeed = SwampRounds > 0 && !HasShield
                ? Math.Max(Speed - 1, 1)
                : Speed;
            Position += effectiveSpeed;
            Position = Math.Min(20, Position);

            // 更新沼泽剩余回合
            if (SwampRounds > 0) SwampRounds--;

            // 清除护盾状态（每回合重置）
            HasShield = false;
        }

        public (string? skillName, List<Horse>? affectedHorses) TryActivateSkill(List<Horse> horses, uint currentRound, bool hasBet)
        {
            if (IsDead) return (null, null);

            var random = new Random();
            var baseRate = SepcialHorse ? 80 : 20;
            var realRate = baseRate + currentRound * 2;
            if (hasBet)
            {
                realRate += 20;
            }
            if (random.Next(100) < realRate)
            {
                // 扩展技能范围到 7 种（原4种 + 新增3种）
                int skill = random.Next(7);
                switch (skill)
                {
                    case 0:
                        var result = ActivateSwapPosition(horses);
                        if (result == null) return (null, null);
                        return ("移形换影", result);
                    case 1:
                        return ("加速[速度+1]", ActivateSpeedUp());
                    case 2:
                        return ("烟雾弹[无法前进]", ActivateSmokeBomb(horses));
                    case 3:
                        return ("闪光弹[随机后退]", ActivateFlashBomb(horses));
                    case 4:
                        return ("疾风冲刺[随机前进2-4步]", ActivateDash());
                    case 5:
                        return ("护盾[免疫马技能和沼泽]", ActivateShield());
                    case 6:
                        return ("回春术[最小速度2]", ActivateHeal());
                }
            }
            return (null, null);
        }

        private List<Horse>? ActivateSwapPosition(List<Horse> horses)
        {
            var aliveHorses = horses.Where(h => !h.IsDead && !h.HasShield && h != this && h.Position > Position).ToList();
            if (aliveHorses.Count != 0)
            {
                var target = aliveHorses[new Random().Next(aliveHorses.Count)];
                (Position, target.Position) = (target.Position, Position);
                return [target];
            }
            return null;
        }

        private List<Horse>? ActivateSpeedUp()
        {
            Speed += 1;
            return null; // 加速只影响自己，不需要播报其他马
        }

        private List<Horse> ActivateSmokeBomb(List<Horse> horses)
        {
            var adjacentHorses = horses
                .Where(h => !h.IsDead && !h.HasShield && Math.Abs(h.Id - Id) == 1)
                .ToList();
            foreach (var horse in adjacentHorses)
            {
                horse.IsStunned = true;
            }
            return adjacentHorses;
        }

        private List<Horse> ActivateFlashBomb(List<Horse> horses)
        {
            var adjacentHorses = horses
                .Where(h => !h.IsDead && !h.HasShield && Math.Abs(h.Id - Id) == 1)
                .ToList();
            foreach (var horse in adjacentHorses)
            {
                int steps = new Random().Next(2, 7);
                horse.Position = Math.Max(0, horse.Position - steps);
            }
            return adjacentHorses;
        }

        private List<Horse>? ActivateDash()
        {
            int steps = new Random().Next(2, 5);
            Position = Math.Min(20, Position + steps);
            return null; // 只影响自己
        }

        private List<Horse>? ActivateShield()
        {
            HasShield = true;
            return null; // 只影响自己
        }

        private List<Horse>? ActivateHeal()
        {
            Speed = Math.Max(OriginalSpeed, Speed + 1);
            return null; // 只影响自己
        }
    }
}