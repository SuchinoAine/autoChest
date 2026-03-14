using UnityEngine;

namespace AutoChess.Core
{
    public sealed class BattleController : IBattleController
    {
        private const float MaxSeparationSpeed = 3.0f;  
        private const float StrafeWeight = 0.35f;       
        private const float RangeEpsilon = 0.05f;       
        private const float SeparationWeight = 2.0f;    
        private const float SeparationRange = 1.5f;     
        private const float StrafeSepThreshold = 0.12f; 
        private const float DesiredRangeFactor = 0.95f; 
        private const float RadialPullWeight = 0.25f;   

        public void StepUnit(BattleWorld world, Unit u, float dt)
        {
            if (u.IsDead) return;

            var target = FindBestTargetByScore(world, u);
            if (target == null) return;
            
            world.RegisterFocus(target.Id);

            float dist = Vector3.Distance(target.Position, u.Position);
            
            if (dist <= u.Range)
            {
                // 在射程内 -> 原地攻击
                if (u.CanAttack())
                {
                    if (world.skillSystem.CastBasicAttack(world, u, target))
                        u.ResetAttackCooldown();
                }
            }
            else
            {
                // 不在射程内 -> 移动追击或走位
                Vector3 toTarget = target.Position - u.Position;
                toTarget.y = 0f;
                float distToTarget = toTarget.magnitude;

                Vector3 dirToTarget;
                Vector3 strafe = Vector3.zero;
                
                // 计算群聚分离倾向 (Boids 柔软避让，物理硬防穿模交给 PBD)
                Vector3 sep = ComputeSeparation(world, u);

                if (u.Isranged) 
                {
                    if (distToTarget > u.Range + RangeEpsilon)
                    {
                        dirToTarget = (distToTarget > 0.0001f) ? (toTarget / distToTarget) : Vector3.zero;
                        strafe = Vector3.zero;
                    }
                    else
                    {
                        dirToTarget = Vector3.zero;
                        Vector3 perp = new Vector3(-toTarget.z, 0f, toTarget.x);
                        if (perp.sqrMagnitude > 0.0001f)
                        {
                            perp.Normalize();
                            int side = SideSignFromRng(world, u, target);
                            float sepMag = sep.magnitude;
                            strafe = (sepMag > StrafeSepThreshold) ? (perp * (StrafeWeight * side)) : Vector3.zero;
                        }
                        float desired = u.Range * DesiredRangeFactor;
                        if (distToTarget > desired + 0.15f) 
                        {
                            Vector3 dirTo = (distToTarget > 0.0001f) ? (toTarget / distToTarget) : Vector3.zero;
                            dirToTarget = dirTo * RadialPullWeight; 
                        }
                    }
                }
                else
                {
                    dirToTarget = (distToTarget > 0.0001f) ? (toTarget / distToTarget) : Vector3.zero;
                }
                
                Vector3 moveDir = dirToTarget + strafe + SeparationWeight * sep;
                if (moveDir.sqrMagnitude > 0.0001f) moveDir = moveDir.normalized;
                
                Vector3 nextPos = u.Position + moveDir * u.MoveSpeed * dt;
                nextPos = new Vector3(nextPos.x, 0f, nextPos.z);

                u.Position = nextPos;
            }
        }

        private Unit FindBestTargetByScore(BattleWorld world, Unit self)
        {
            Unit best = null;
            float bestScore = float.NegativeInfinity;
            const float engageEps = 0.10f; 

            bool hasInRange = false;
            foreach (var u in world.Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;
                float dist = Vector3.Distance(u.Position, self.Position);
                if (dist <= self.Range + engageEps)
                {
                    hasInRange = true;
                    break;
                }
            }
            
            foreach (var u in world.Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;

                float dist = Vector3.Distance(u.Position, self.Position);
                if (hasInRange && dist > self.Range + engageEps) continue;
                
                float score = ScoreTarget(world, self, u);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = u;
                }
            }

            return best;
        }

        private float ScoreTarget(BattleWorld world, Unit self, Unit target)
        {
            float wLowHp = world.AiConfig != null ? world.AiConfig.wLowHp : 1.0f;
            float wNear = world.AiConfig != null ? world.AiConfig.wNear : 0.7f;
            float wKillable = world.AiConfig != null ? world.AiConfig.wKillable : 1.5f;
            float wFocus = world.AiConfig != null ? world.AiConfig.wFocus : 0.4f;
            float wPreferRanged = world.AiConfig != null ? world.AiConfig.wPreferRangedTarget : 0.6f;
            float nearRef = world.AiConfig != null ? world.AiConfig.nearDistRef : 6.0f;

            int seed = world.AiConfig != null ? world.AiConfig.battleSeed : 12345;
            int q = (world.AiConfig != null && world.AiConfig.tickQuant > 0) ? world.AiConfig.tickQuant : 1;
            int jt = world.TickIndex / q;
            float jitterAmp = world.AiConfig != null ? world.AiConfig.targetJitter : 0f;
            float jtScore = jitterAmp * DecisionRng.Signed(seed, DecisionStream.TargetJitter, self.Id, target.Id, jt);

            float dist = Vector3.Distance(target.Position, self.Position);
            float lowHpScore = 1f / (target.Hp + 1f);
            float nearScore = Mathf.Clamp01(1f - (dist / nearRef));
            float killableScore = (self.Atk >= target.Hp) ? 1f : 0f;
            int focusCnt = world.GetFocusCount(target.Id);
            float focusScore = Mathf.Log(1f + focusCnt); 
            float preferRangedScore = 0f;
            
            if (self.Isranged && target.Isranged) preferRangedScore = 1f;

            float total =
                wLowHp * lowHpScore +
                wNear * nearScore +
                wKillable * killableScore +
                wFocus * focusScore +
                wPreferRanged * preferRangedScore +
                jtScore;

            return total;
        }

        private Vector3 ComputeSeparation(BattleWorld world, Unit self)
        {
            Vector3 sep = Vector3.zero;
            foreach (var other in world.Units)
            {
                if (other == self) continue;
                if (other.IsDead) continue;

                Vector3 delta = self.Position - other.Position;
                float dist = delta.magnitude;
                if (dist < 0.0001f) continue;

                if (dist > SeparationRange) continue;
                
                float r1 = self.Radius > 0.1f ? self.Radius : 0.5f;
                float r2 = other.Radius > 0.1f ? other.Radius : 0.5f;
                float minDist = r1 + r2;

                if (dist < minDist)
                {
                    float overlap = minDist - dist; 
                    sep += delta / dist * overlap;  
                }
            }
            if (sep.sqrMagnitude > 0.001f)
            {
                var desired = sep.normalized * MaxSeparationSpeed;
                sep = Vector3.ClampMagnitude(desired, MaxSeparationSpeed);
            }

            return sep;
        }

        private int SideSignFromRng(BattleWorld world, Unit u, Unit target)
        {
            int seed = world.AiConfig != null ? world.AiConfig.battleSeed : 12345;
            int tickIndex = world.TickIndex;
            int sideTick = tickIndex / 30;
            int side = DecisionRng.Coin(seed, DecisionStream.StrafeSide, u.Id, target.Id, sideTick, 0.5f) ? 1 : -1;

            return side;
        }
    }
}