using System.Collections.Generic;
using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Core
{
    public enum Team { A, B }

    /// <summary>
    /// 纯净的逻辑层单位类，不包含任何 Unity 表现层组件 (GameObject/Transform)
    /// </summary>
    public class Unit
    {
        public string Id { get; private set; }
        public Team Team { get; private set; }
        
        // === 基础生存属性 ===
        public float MaxHp { get; private set; }
        public float Hp { get; set; }
        public bool IsDead => Hp <= 0f;

        // === 战斗面板属性 === 
        // 设为 get; set; 是为了完美配合 StatAddEffectSO 的直接加减
        public float Atk { get; set; }
        public float AtkInterval { get; set; }
        public float MoveSpeed { get; set; }
        public float Range { get; set; }
        
        // === 形态属性 ===
        public float Radius { get; private set; }
        // 注意：严格匹配 BattleController 中的 Isranged (小写r)
        public bool Isranged { get; private set; } 
        public Vector3 Position { get; set; }

        // === 技能系统 ===
        // 注意：严格匹配 SystemSkill 中的 BasicAttack
        public SkillDefSO BasicAttack { get; private set; } 
        public SkillDefSO DefaultSkillDef { get; private set; }
        
        // 技能运行时列表 (供 SystemSkill 和 UnitHud 读取)
        public List<SkillRuntime> Skills { get; private set; } = new List<SkillRuntime>();

        // === Buff系统 ===
        // 供 SystemBuff 读写和存储 Buff 实例
        public List<BuffInstance> Buffs { get; private set; } = new List<BuffInstance>();

        // === 冷却状态 ===
        public float AtkCdLeft { get; set; }
        
        // 供 UnitHud 读取的普攻冷却归一化值 (0 = 完全就绪, 1 = 刚攻击完还在最大冷却中)
        public float AtkCdNorm => AtkInterval > 0f ? (AtkCdLeft / AtkInterval) : 0f;

        /// <summary>
        /// 构造函数，签名与 SandboxRunner.CreateUnitFromConfig 严格对应
        /// </summary>
        public Unit(string id, Team team, float hp, float atk, float atkInterval, float moveSpeed, float range, Vector3 position, float radius, bool isranged, SkillDefSO basicAttack, SkillDefSO defaultSkill)
        {
            Id = id;
            Team = team;
            
            MaxHp = hp;
            Hp = hp;
            
            Atk = atk;
            AtkInterval = atkInterval;
            MoveSpeed = moveSpeed;
            Range = range;
            Radius = radius;
            Isranged = isranged;
            Position = position;

            BasicAttack = basicAttack;
            DefaultSkillDef = defaultSkill;

            // 初始化技能运行时列表 (HUD 默认读取 Skills[0])
            if (defaultSkill != null)
            {
                Skills.Add(new SkillRuntime(defaultSkill));
            }
        }
        
        /// <summary>
        /// 核心层逻辑推进：由 BattleWorld.Tick() 驱动 (只处理普攻CD)
        /// 注意：主动技能的 CD 扣减已经在 SystemSkill.Update 中处理了，这里不重复处理
        /// </summary>
        public void TickCooldown(float dt)
        {
            if (IsDead) return;

            // 1. 普攻 CD 扣减
            if (AtkCdLeft > 0f)
            {
                AtkCdLeft -= dt;
                if (AtkCdLeft < 0f) AtkCdLeft = 0f;
            }
        }

        // === 供 BattleController 调用的攻击状态判定 ===
        public bool CanAttack() => AtkCdLeft <= 0f;
        public void ResetAttackCooldown() => AtkCdLeft = AtkInterval;
    }
}