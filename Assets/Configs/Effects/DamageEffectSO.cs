using AutoChess.Core;
using UnityEngine;


namespace AutoChess.Configs.Effects
{
    /// <summary>
    /// 基于攻击力的伤害型 effect
    /// </summary>
    [CreateAssetMenu(fileName = "Damage_Flat", menuName = "AutoChess/Effects/Damage Flat")]
    public class DamageEffectSO : EffectDefSO
    {
        public float amount;

        public override void Apply(BattleWorld world, Unit source, Unit target)
        {
            if (world == null || target == null) return;
            world.DealDamage(source, target, amount);
        }
    }
}