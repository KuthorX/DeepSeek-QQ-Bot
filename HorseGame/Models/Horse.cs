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

        public void Move()
        {
            if (IsDead) return;

            if (IsStunned) {
                IsStunned = false;
                return;
            }

            // 如果处于沼泽中，速度减1（最低为1）
            int effectiveSpeed = SwampRounds > 0 ? Math.Max(Speed - 1, 1) : Speed;
            Position += effectiveSpeed;
            if (Position >= 20) {
                Position = 20;
            }

            // 更新沼泽剩余回合
            if (SwampRounds > 0) SwampRounds--;
        }

        public (string? skillName, List<Horse>? affectedHorses) TryActivateSkill(List<Horse> horses)
        {
            if (IsDead) return (null, null);

            var random = new Random();
            if (random.Next(100) < 20) // 20% 概率触发技能
            {
                int skill = random.Next(4); // 随机选择一个技能
                switch (skill)
                {
                    case 0:
                        var result = ActivateSwapPosition(horses);
                        if (result == null) return (null, null);
                        return ("移形换影", result);
                    case 1:
                        return ("加速", ActivateSpeedUp());
                    case 2:
                        return ("烟雾弹（无法前进）", ActivateSmokeBomb(horses));
                    case 3:
                        return ("闪光弹（随机后退）", ActivateFlashBomb(horses));
                }
            }
            return (null, null);
        }

        private List<Horse>? ActivateSwapPosition(List<Horse> horses)
        {
            var aliveHorses = horses.Where(h => !h.IsDead && h != this && h.Position > Position).ToList();
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
                .Where(h => !h.IsDead && Math.Abs(h.Id - Id) == 1)
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
                .Where(h => !h.IsDead && Math.Abs(h.Id - Id) == 1)
                .ToList();
            foreach (var horse in adjacentHorses)
            {
                int steps = new Random().Next(2, 7);
                horse.Position = Math.Max(0, horse.Position - steps);
            }
            return adjacentHorses;
        }
    }
}