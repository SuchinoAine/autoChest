using AutoChess.Core;
using UnityEngine;


namespace AutoChess.Configs.Effects
{    
    /// <summary>
    /// 应用 BuffDefSO 的 effect
    /// </summary>
    [CreateAssetMenu(fileName = "Apply_Buff", menuName = "AutoChess/Effects/Apply Buff")]
    public class ApplyBuffEffectSO : EffectDefSO
    {
        public BuffDefSO buff;

        public override void Apply(BattleWorld world, Unit source, Unit target)
        {
            if (world == null || target == null || buff == null) return;
            world.buffSystem.AddBuff(world, source, target, buff);
        }
    }
}