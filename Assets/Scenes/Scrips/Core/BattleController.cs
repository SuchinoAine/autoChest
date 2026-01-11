using System.Collections.Generic;
using UnityEngine;

namespace AutoChess.Core

{
    public sealed class BattleController : IBattleController
    {
        private const float MaxSeparationSpeed = 3.0f;  // 分离附加速度上限
        private const float StrafeWeight = 0.35f;       // 侧移强度（0.2~0.5）
        private const float RangeEpsilon = 0.05f;       // 射程边界滞回，防抖
        private const float SeparationWeight = 2.0f;    // 分离强度
        private const float SeparationRange = 1.5f;     // 作用范围
        private const float ContactEpsilon = 0.02f;     // 接触距离留一点缝
        private const float SeparationResolveSpeed = 6f;// 攻击时的“挤开”速度（不影响追击）
        private const float StrafeSepThreshold = 0.12f; // sep 超过这个才允许侧移（看起来像“被挤了才走位”）
        private const float DesiredRangeFactor = 0.95f; // 希望保持在 range*0.95 附近
        private const float RadialPullWeight = 0.25f;   // 拉回强度（很小）

        public void StepUnit(BattleWorld world, Unit u, float dt)
        {
            // 判死
            if (u.IsDead) return;
            // 选目标
            var target = FindBestTargetByScore(world, u);
            if (target == null) return;
            // register focus（原来在 Tick 里做）
            world.RegisterFocus(target.Id);

            float dist = Vector3.Distance(target.Position, u.Position);
            // in range -> attack
            if (dist <= u.Range)
            {
                // 即使站桩攻击，也要解重叠，否则会粘住
                ResolveOverlap(world, u, dt);
                if (u.CanAttack())
                {
                    target.Hp -= u.Atk;  // Functional attack logic moved here
                    world.EmitAttack(u, target, u.Atk);
                    u.ResetAttackCooldown();

                    if (target.IsDead)
                    {
                        // ✅ 死亡事件：不再依赖 log 扫描
                        world.EmitDeath(target, u);
                    }
                }
            }
            else
            {
                // out of range -> move toward (melee) / advance-or-strafe (ranged)
                Vector3 toTarget = target.Position - u.Position;
                toTarget.y = 0f;
                float distToTarget = toTarget.magnitude;

                Vector3 dirToTarget;
                Vector3 strafe = Vector3.zero;
                Vector3 sep = ComputeSeparation(world, u);

                if (u.Isranged)
                {
                    // 远程：只有超出射程才前进；射程内不后撤，做轻微侧移找位
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
                            // 只有在确实挤时才侧移，避免两射手互锁定就一直漂
                            int side = SideSignFromRng(world, u, target);
                            float sepMag = sep.magnitude;
                            if (sepMag > StrafeSepThreshold)
                                strafe = perp * (StrafeWeight * side);
                            else
                                strafe = Vector3.zero;
                        }
                        // 轻微拉回：如果因为各种原因距离被拉大了，就给一点点向目标方向的分量
                        float desired = u.Range * DesiredRangeFactor;
                        if (distToTarget > desired + 0.15f) // 只在明显偏远时拉回
                        {
                            Vector3 dirTo = (distToTarget > 0.0001f) ? (toTarget / distToTarget) : Vector3.zero;
                            dirToTarget = dirTo * RadialPullWeight; // 这是一个方向贡献，后面会归一化
                        }
                    }
                }
                else
                {
                    // 近战：保持追击
                    dirToTarget = (distToTarget > 0.0001f) ? (toTarget / distToTarget) : Vector3.zero;
                }
                // 合成移动方向：追击/侧移 + 分离
                Vector3 moveDir = dirToTarget + strafe + SeparationWeight * sep;
                if (moveDir.sqrMagnitude > 0.0001f) moveDir = moveDir.normalized;
                // 先算 nextPos 再处理接触距离
                Vector3 oldPos = u.Position;
                Vector3 nextPos = u.Position + moveDir * u.MoveSpeed * dt;
                nextPos = new Vector3(nextPos.x, 0f, nextPos.z);

                // 只要本帧有“向目标推进”的趋势，就做“停在接触距离”，避免穿模粘住
                //（对远程也安全：远程射程内 dirToTarget=0，forward≈0，不会触发）
                if (distToTarget > 0.0001f)
                {
                    Vector3 dirTo = toTarget / distToTarget;    // 指向目标的单位向量
                    float forward = Vector3.Dot(moveDir, dirTo);// moveDir 在追击方向上的分量
                    if (forward > 0.001f)
                    {
                        float minDist = u.Radius + target.Radius + ContactEpsilon;
                        float nextDist = Vector3.Distance(nextPos, target.Position);
                        if (nextDist < minDist)
                        {
                            nextPos = target.Position - dirTo * minDist;
                            nextPos = new Vector3(nextPos.x, 0f, nextPos.z);
                        }
                    }
                }
                u.Position = nextPos;
                // u.Position = new Vector3(u.Position.x, 0f, u.Position.z);
                if ((u.Position - oldPos).sqrMagnitude > 0.0001f)
                    world.EmitMove(u, oldPos, u.Position);
            }
        }

        // ======= 下面这些方法：从 BattleWorld 原样搬过来，只把字段改成 world.xxx =======
        /// <summary>
        /// 寻找评分最高的目标，优先在射程内
        /// </summary>
        private Unit FindBestTargetByScore(BattleWorld world, Unit self)
        {
            Unit best = null;
            float bestScore = float.NegativeInfinity;
            const float engageEps = 0.10f; // 小滞回：避免刚好在边界抖动

            // 先找射程内的敌人
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
            // 第二遍：根据是否有射程内目标，决定候选池
            foreach (var u in world.Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;

                float dist = Vector3.Distance(u.Position, self.Position);
                //  有近的就不看远的 过滤不在射程里的人
                if (hasInRange && dist > self.Range + engageEps) continue;
                //  没有近的就看所有敌人
                float score = ScoreTarget(world, self, u);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = u;
                }
            }

            return best;
        }


        /// <summary>
        /// 评价目标分数
        /// </summary>
        /// <param name="self">self</param>
        /// <param name="target">target</param>
        /// <returns></returns>
        private float ScoreTarget(BattleWorld world, Unit self, Unit target)
        {
            // 安全默认值
            float wLowHp = world.AiConfig != null ? world.AiConfig.wLowHp : 1.0f;
            float wNear = world.AiConfig != null ? world.AiConfig.wNear : 0.7f;
            float wKillable = world.AiConfig != null ? world.AiConfig.wKillable : 1.5f;
            float wFocus = world.AiConfig != null ? world.AiConfig.wFocus : 0.4f;
            float wPreferRanged = world.AiConfig != null ? world.AiConfig.wPreferRangedTarget : 0.6f;
            float nearRef = world.AiConfig != null ? world.AiConfig.nearDistRef : 6.0f;

            // decision Rng part
            int seed = world.AiConfig != null ? world.AiConfig.battleSeed : 12345;
            int q = (world.AiConfig != null && world.AiConfig.tickQuant > 0) ? world.AiConfig.tickQuant : 1;
            int jt = world.TickIndex / q;
            float jitterAmp = world.AiConfig != null ? world.AiConfig.targetJitter : 0f;
            float jtScore = jitterAmp * DecisionRng.Signed(seed, DecisionStream.TargetJitter, self.Id, target.Id, jt);

            float dist = Vector3.Distance(target.Position, self.Position);
            // 1) 残血优先
            float lowHpScore = 1f / (target.Hp + 1f);
            // 2) 距离越近越优先
            float nearScore = Mathf.Clamp01(1f - (dist / nearRef));
            // 3) 可击杀
            float killableScore = (self.Atk >= target.Hp) ? 1f : 0f;
            // 4) 集火：已经有多少队友在打它
            int focusCnt = world.GetFocusCount(target.Id);
            float focusScore = Mathf.Log(1f + focusCnt); // 避免无限堆叠 log/sqrt： 0, 0.69, 1.09, ...
            // 5) 后排偏好：只有当“自己是远程”时才偏好打敌方远程
            float preferRangedScore = 0f;
            if (self.Isranged && target.Isranged)
                preferRangedScore = 1f;

            float total =
                wLowHp * lowHpScore +
                wNear * nearScore +
                wKillable * killableScore +
                wFocus * focusScore +
                wPreferRanged * preferRangedScore +
                jtScore;

            return total;
        }

        /// <summary>
        /// 计算重叠分离向量
        /// </summary>
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

                // 只在一定范围内考虑
                if (dist > SeparationRange) continue;
                float minDist = self.Radius + other.Radius;

                // 只在“过近/重叠”时施加分离
                if (dist < minDist)
                {
                    float overlap = minDist - dist; // 重叠量
                    sep += delta / dist * overlap;  // 推开方向 * 强度
                }
            }
            // 限幅，防止抖动或推太猛
            if (sep.sqrMagnitude > 0.001f)
            {
                var desired = sep.normalized * MaxSeparationSpeed;
                sep = Vector3.ClampMagnitude(desired, MaxSeparationSpeed);
            }

            return sep;
        }

        /// <summary>
        /// 用 BattleRng 生成单位侧移方向
        /// </summary>
        private int SideSignFromRng(BattleWorld world, Unit u, Unit target)
        {
            // 固定左右：保证同一单位每局一致（也利于复现）
            int seed = world.AiConfig != null ? world.AiConfig.battleSeed : 12345;
            // 每 30 tick 才可能换一次
            int tickIndex = world.TickIndex;
            int sideTick = tickIndex / 30;
            int side = DecisionRng.Coin(seed, DecisionStream.StrafeSide, u.Id, target.Id, sideTick, 0.5f) ? 1 : -1;

            return side;
        }

        /// <summary>
        /// 解决单位间重叠（攻击后调用）
        /// </summary>
        private void ResolveOverlap(BattleWorld world, Unit u, float dt)
        {
            Vector3 push = Vector3.zero;

            foreach (var other in world.Units)
            {
                if (other == u) continue;
                if (other.IsDead) continue;

                Vector3 delta = u.Position - other.Position;
                float dist = delta.magnitude;
                if (dist < 0.0001f) continue;

                float minDist = u.Radius + other.Radius + ContactEpsilon;
                // 只在“重叠”时推开
                if (dist < minDist)
                {
                    float overlap = minDist - dist;
                    push += delta / dist * overlap;
                }
            }

            if (push.sqrMagnitude > 0.000001f)
            {
                // 用一个固定速度把重叠解开（不会让单位乱跑）
                Vector3 dir = push.normalized;
                u.Position += dt * SeparationResolveSpeed * dir;
                u.Position = new Vector3(u.Position.x, 0f, u.Position.z);
            }
        }
    }
}