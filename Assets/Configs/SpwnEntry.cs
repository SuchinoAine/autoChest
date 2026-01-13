using System;
using UnityEngine;
using AutoChess.Core;

namespace AutoChess.Configs
{
[Serializable]
public class SpawnEntry
    {
        public UnitConfig config;
        public Team team;
        public Vector3 startPos;
        public SkillDefSO basicAttack;  // 基础攻击技能
        public SkillDefSO defaultSkill;  // 可选覆盖默认技能
    }
}
