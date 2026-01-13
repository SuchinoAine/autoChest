using UnityEngine;
using AutoChess.Configs;


namespace AutoChess.Core
{
    public class SystemSkill
    {
        /// <summary>
        /// Auto-cast skills in Unit.Skills list.
        /// </summary>
        public void Update(BattleWorld world, float dt)
        {
            if (world == null) return;

            foreach (var caster in world.Units)
            {
                if (caster == null || caster.IsDead) continue;

                for (int i = 0; i < caster.Skills.Count; i++)
                    caster.Skills[i].Tick(dt);

                for (int i = 0; i < caster.Skills.Count; i++)
                {
                    var skill = caster.Skills[i];
                    if (!skill.Ready) continue;

                    var target = FindNearestEnemy(world, caster, skill.Def.range);
                    if (target == null) continue;

                    CastSkill(world, caster, skill.Def, target);

                    skill.ResetCd();
                    break;
                }
            }
        }

        /// <summary>
        /// Immediate cast entry (used by controller for basic attack).
        /// </summary>
        public bool CastSkill(BattleWorld world, Unit caster, SkillDefSO def, Unit target)
        {
            if (world == null || caster == null || target == null) return false;
            if (caster.IsDead || target.IsDead) return false;

            for (int e = 0; e < def.effects.Count; e++)
                def.effects[e].Apply(world, caster, target);
            Debug.Log($"HP AFTER: {target.Id} hp={target.Hp}");
            return true;
        }

        public bool CastBasicAttack(BattleWorld world, Unit caster, Unit target)
        {
            if (caster.BasicAttack == null)
            {
                Debug.LogWarning("[SystemSkill] CastBasicAttack failed: caster has no BasicAttack skill.");
                return false;
            }
            return CastSkill(world, caster, caster.BasicAttack, target);
        }

        private Unit FindNearestEnemy(BattleWorld world, Unit caster, float range)
        {
            Unit best = null;
            float bestDist = float.MaxValue;

            foreach (var u in world.Units)
            {
                if (u == null || u.IsDead) continue;
                if (u.Team == caster.Team) continue;

                float d = Vector3.Distance(u.Position, caster.Position);
                if (d <= range && d < bestDist)
                {
                    bestDist = d;
                    best = u;
                }
            }
            return best;
        }
    }

    public class SkillRuntime
    {
        public SkillDefSO Def;
        public float CdLeft;

        public SkillRuntime(SkillDefSO def)
        {
            Def = def;
            CdLeft = 0f;
        }

        public void Tick(float dt)
        {
            if (CdLeft > 0f) CdLeft -= dt;
        }

        public bool Ready => CdLeft <= 0f;

        public void ResetCd() => CdLeft = Def.cooldown;
    }
}

