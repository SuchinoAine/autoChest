using System.Collections.Generic;
using UnityEngine;

namespace AutoChess.Configs
{
    [CreateAssetMenu(fileName = "NewBuff", menuName = "AutoChess/Buff")]
    public class BuffDefSO : ScriptableObject
    {
        public string id;
        public float duration;
        public float tickInterval;
        public int maxStacks = 1;

        public List<EffectDefSO> onApply = new();
        public List<EffectDefSO> onTick = new();
        public List<EffectDefSO> onExpire = new();
    }
}
