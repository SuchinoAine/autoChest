using AutoChess.Core;
using UnityEngine;


namespace AutoChess.Configs.Effects
{
    /// <summary>
    /// 基于攻击力的伤害型 effect
    /// </summary>
    [CreateAssetMenu(fileName = "Damage_From_Atk", menuName = "AutoChess/Effects/Damage From Atk")]
    public class DamageFromAtkEffectSO : EffectDefSO
    {
        public float mult = 1f;
        public float flat = 0f;

        public override void Apply(BattleWorld world, Unit source, Unit target)
        {
            if (world == null || source == null || target == null) return;
            world.DealDamage(source, target, source.Atk * mult + flat);
        }
    }
}