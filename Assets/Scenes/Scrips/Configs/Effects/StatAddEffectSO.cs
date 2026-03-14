using AutoChess.Core;
using UnityEngine;

namespace AutoChess.Configs.Effects
{
    /// <summary>
    /// 属性增减型 effect
    /// </summary>
    [CreateAssetMenu(fileName = "Stat_Add", menuName = "AutoChess/Effects/Stat Add")]
    public class StatAddEffectSO : EffectDefSO
    {
        public StatKind stat;
        public float delta;

        public override void Apply(BattleWorld world, Unit source, Unit target)
        {
            if (target == null) return;

            switch (stat)
            {
                case StatKind.Atk: target.Atk += delta; break;
                case StatKind.MoveSpeed: target.MoveSpeed += delta; break;
                case StatKind.Range: target.Range += delta; break;
                case StatKind.AtkInterval:
                    target.AtkInterval = Mathf.Max(0.05f, target.AtkInterval + delta);
                    break;
            }
        }
    }
}