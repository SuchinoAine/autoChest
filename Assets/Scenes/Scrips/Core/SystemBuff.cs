using System.Collections.Generic;
using AutoChess.Configs;

namespace AutoChess.Core
{
    public class SystemBuff
    {
        private readonly Dictionary<Unit, List<BuffInstance>> _buffs = new();
        public void AddBuff(BattleWorld world, Unit source, Unit target, BuffDefSO def)
        {
            if (world == null || target == null || def == null) return;
            if (target.IsDead) return;

            if (!_buffs.TryGetValue(target, out var list))
            {
                list = new List<BuffInstance>();
                _buffs[target] = list;
            }

            // ✅ 简单叠层：同 Id 刷新时长/叠 stacks
            var existing = list.Find(b => b.Def != null && b.Def.id == def.id);
            if (existing != null)
            {
                existing.Stacks = System.Math.Min(def.maxStacks, existing.Stacks + 1);
                existing.TimeLeft = System.Math.Max(existing.TimeLeft, def.duration);
                existing.NextTickTime = System.Math.Min(existing.NextTickTime, def.tickInterval);
                return;
            }

            var inst = new BuffInstance
            {
                Def = def,
                Source = source,
                Target = target,
                TimeLeft = def.duration,
                NextTickTime = def.tickInterval,
                Stacks = 1
            };

            list.Add(inst);
            target.Buffs.Add(inst);

            // OnApply effects
            foreach (var e in def.onApply)
                e.Apply(world, source, target);
        }

        public void Update(BattleWorld world, float dt)
        {
            if (world == null) return;

            foreach (var pair in _buffs)
            {
                var target = pair.Key;
                var list = pair.Value;

                if (target == null || target.IsDead) continue;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var b = list[i];
                    b.TimeLeft -= dt;

                    if (b.Def.tickInterval > 0)
                    {
                        b.NextTickTime -= dt;
                        if (b.NextTickTime <= 0)
                        {
                            b.NextTickTime += b.Def.tickInterval;
                            foreach (var e in b.Def.onTick)
                                e.Apply(world, b.Source, target);
                        }
                    }

                    if (b.TimeLeft <= 0)
                    {
                        foreach (var e in b.Def.onExpire)
                            e.Apply(world, b.Source, target);

                        list.RemoveAt(i);
                        target.Buffs.Remove(b);
                    }
                }
            }
        }
    }

    public class BuffInstance
    {
        public BuffDefSO Def;
        public Unit Source;
        public Unit Target;

        public float TimeLeft;
        public float NextTickTime;
        public int Stacks;
    }
}