using UnityEngine;
using AutoChess.Core;


namespace AutoChess.Configs
{
    public abstract class EffectDefSO : ScriptableObject
    {
        public enum StatKind { Atk, MoveSpeed, Range, AtkInterval }
        public abstract void Apply(BattleWorld world, Unit source, Unit target);
    }
}
