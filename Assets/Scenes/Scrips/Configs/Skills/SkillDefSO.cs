using System.Collections.Generic;
using UnityEngine;

namespace AutoChess.Configs
{
    [CreateAssetMenu(menuName="AutoChess/Skill")]
    public class SkillDefSO : ScriptableObject
    {
        public string id;       // skill id
        public float cooldown;  // seconds
        public float range;     // skill range
        public List<EffectDefSO> effects = new();
    }
}
