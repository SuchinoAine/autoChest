using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoChess.Core
{
    public class BattleWorld
    {
        public readonly List<Unit> Units = new();
        public readonly List<BattleLog> Logs = new();
        public float Time { get; private set; }
        public int _tickIndex { get; private set; } = 0;
        public bool IsEnded { get; private set; }
        public Team? Winner { get; private set; }
        public void Add(Unit u) => Units.Add(u);  // 添加单位
        public void ResetLogs() => Logs.Clear();  // 重置日志
        public AIConfig AiConfig { get; set; }  // AI 配置参数

        private const float MaxSeparationSpeed = 3.0f;  // 分离附加速度上限
        private const float StrafeWeight = 0.35f;   // 侧移强度（0.2~0.5）
        private const float RangeEpsilon = 0.05f;   // 射程边界滞回，防抖
        private Dictionary<string, int> _focusCount = new(); // 集火计数缓存

        private const float SeparationWeight = 2.0f;    // 分离强度
        private const float SeparationRange = 1.5f;     // 作用范围
        private const float ContactEpsilon = 0.02f;      // 接触距离留一点缝
        private const float SeparationResolveSpeed = 6f; // 攻击时的“挤开”速度（不影响追击）

        private const float StrafeSepThreshold = 0.12f;  // sep 超过这个才允许侧移（看起来像“被挤了才走位”）
        private const float DesiredRangeFactor = 0.95f;  // 希望保持在 range*0.95 附近
        private const float RadialPullWeight = 0.25f;    // 拉回强度（很小）



        public void Tick(float dt)
        {
            if (IsEnded) return;
            Time += dt;
            // cooldown
            foreach (var u in Units)
            {
                if (!u.IsDead) u.TickCooldown(dt);
            }
            // 索敌清空
            _focusCount.Clear();
            
            // 每帧换一个起点：让“谁先行动”在长期统计上均匀
            int n = Units.Count;
            if (n == 0) return;
            int start = _tickIndex % n;
            // actions: better order
            for (int k = 0; k < n; k++)
            {
                Unit u = Units[(start + k) % n];


                //  判死判空
                if (u.IsDead) continue;
                //  select target
                var target = FindBestTargetByScore(u);
                if (target == null) continue;
                // register focus
                if (!_focusCount.ContainsKey(target.Id)) _focusCount[target.Id] = 0;
                _focusCount[target.Id]++;

                float dist = Vector3.Distance(target.Position, u.Position);
                // in range -> attack
                if (dist <= u.Range)
                {
                    // 即使站桩攻击，也要解重叠，否则会粘住
                    ResolveOverlap(u, dt);
                    if (u.CanAttack())
                    {
                        target.Hp -= u.Atk;
                        u.ResetAttackCooldown();

                        Logs.Add(new BattleLog(LogType.Attack, Time, u.Id, target.Id, u.Position, u.Atk));
                        if (target.IsDead)
                        {
                            // SelfDeath log
                            Logs.Add(new BattleLog(LogType.Death, Time, target.Id, "", u.Position, 0));
                        }

                    }
                }
                // out of range -> move toward (melee) / advance-or-strafe (ranged)
                else
                {
                    Vector3 toTarget = target.Position - u.Position;
                    toTarget.y = 0f;
                    float distToTarget = toTarget.magnitude;

                    Vector3 dirToTarget = Vector3.zero;
                    Vector3 strafe = Vector3.zero;
                    Vector3 sep = ComputeSeparation(u);

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
                                int side = SideSignFromId(u, target);

                                // 只有在“确实挤”时才侧移，避免两射手互锁定就一直漂
                                float sepMag = sep.magnitude; // 注意：这里需要 sep 在此之前先算出来
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
                                dirToTarget = dirTo * RadialPullWeight; // 注意：这是一个“方向贡献”，后面会归一化
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
                        Vector3 dirTo = toTarget / distToTarget; // 指向目标的单位向量
                        float forward = Vector3.Dot(moveDir, dirTo); // moveDir 在追击方向上的分量

                        if (forward > 0.001f)
                        {
                            const float ContactEpsilon = 0.02f; // 留一点缝
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
                    u.Position = new Vector3(u.Position.x, 0f, u.Position.z);
                    if ((u.Position - oldPos).sqrMagnitude > 0.0001f)
                        Logs.Add(new BattleLog(LogType.Move, Time, u.Id, "", u.Position, 0));
                }
            }
            // tick index++
            _tickIndex++;
            CheckEnd();
        }


        private Unit FindBestTargetByScore(Unit self)
        {
            Unit best = null;
            float bestScore = float.NegativeInfinity;
            const float engageEps = 0.10f; // 小滞回：避免刚好在边界抖动

            // 先找射程内的敌人
            bool hasInRange = false;
            foreach (var u in Units)
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
            foreach (var u in Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;

                float dist = Vector3.Distance(u.Position, self.Position);
                //  有近的就不看远的 过滤不在射程里的人
                if (hasInRange && dist > self.Range + engageEps) continue; 
                //  没有近的就看所有敌人
                float score = ScoreTarget(self, u);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = u;
                }
            }

            return best;
        }

        private float ScoreTarget(Unit self, Unit target)
        {
            // 安全默认值
            float wLowHp = AiConfig != null ? AiConfig.wLowHp : 1.0f;
            float wNear = AiConfig != null ? AiConfig.wNear : 0.7f;
            float wKillable = AiConfig != null ? AiConfig.wKillable : 1.5f;
            float wFocus = AiConfig != null ? AiConfig.wFocus : 0.4f;
            float wPreferRanged = AiConfig != null ? AiConfig.wPreferRangedTarget : 0.6f;
            float nearRef = AiConfig != null ? AiConfig.nearDistRef : 6.0f;

            // decision Rng part
            int seed = (AiConfig != null) ? AiConfig.battleSeed : 12345;
            int q = (AiConfig != null && AiConfig.tickQuant > 0) ? AiConfig.tickQuant : 1;
            int jt = _tickIndex / q;
            float jitterAmp = (AiConfig != null) ? AiConfig.targetJitter : 0f;
            float jtScore = jitterAmp * DecisionRng.Signed(seed, DecisionStream.TargetJitter, self.Id, target.Id, jt);

            float dist = Vector3.Distance(target.Position, self.Position);
            // 1) 残血优先
            float lowHpScore = 1f / (target.Hp + 1f);
            // 2) 距离越近越优先
            float nearScore = Mathf.Clamp01(1f - (dist / nearRef));
            // 3) 可击杀
            float killableScore = (self.Atk >= target.Hp) ? 1f : 0f;
            // 4) 集火：已经有多少队友在打它
            int focusCnt = 0;
            if (_focusCount != null && _focusCount.TryGetValue(target.Id, out var c)) focusCnt = c;
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
                wPreferRanged * preferRangedScore+
                jtScore;

            return total;
        }

        private Vector3 ComputeSeparation(Unit self)
        {
            Vector3 sep = Vector3.zero;

            foreach (var other in Units)
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
                    float overlap = (minDist - dist); // 重叠量
                    sep += (delta / dist) * overlap;  // 推开方向 * 强度
                }
            }
            // 限幅，防止抖动或推太猛
            if (sep.sqrMagnitude > 0.0001f)
            {
                var desired = sep.normalized * MaxSeparationSpeed;
                sep = Vector3.ClampMagnitude(desired, MaxSeparationSpeed);
                sep.y = 0f; // 🔒 强制锁在地面平面
            }

            return sep;
        }
        private int SideSignFromId(Unit u, Unit target)
        {
            // 固定左右：保证同一单位每局一致（也利于复现）
            int seed = AiConfig != null ? AiConfig.battleSeed : 12345;
            // 每 30 tick 才可能换一次
            int sideTick = _tickIndex / 30; 

            int side = DecisionRng.Coin(
                seed,
                DecisionStream.StrafeSide,
                u.Id,
                target.Id,
                sideTick,
                0.5f
            ) ? 1 : -1;
            return side;
        }
        private void CheckEnd()
        {
            var aliveA = Units.Any(u => !u.IsDead && u.Team == Team.A);
            var aliveB = Units.Any(u => !u.IsDead && u.Team == Team.B);

            if (aliveA && aliveB) return;

            IsEnded = true;
            Winner = aliveA ? Team.A : Team.B;
            var winnerName = Winner == Team.A ? "TeamA" : "TeamB";
            Logs.Add(new BattleLog(LogType.End, Time, winnerName, "", Vector3.zero, 0));
            Debug.Log($"[BattleWorld] Battle ended at time {Time:F2}s, {_tickIndex}t. Winner: {winnerName}");
        }

        //  解决单位间重叠（攻击后调用）
        private void ResolveOverlap(Unit u, float dt)
        {
            Vector3 push = Vector3.zero;

            foreach (var other in Units)
            {
                if (other == u) continue;
                if (other.IsDead) continue;

                Vector3 delta = u.Position - other.Position;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.0001f) continue;

                float minDist = u.Radius + other.Radius + ContactEpsilon;

                // 只在“重叠”时推开
                if (dist < minDist)
                {
                    float overlap = minDist - dist;
                    push += (delta / dist) * overlap;
                }
            }

            if (push.sqrMagnitude > 0.000001f)
            {
                // 用一个固定速度把重叠解开（不会让单位乱跑）
                Vector3 dir = push.normalized;
                u.Position += dir * SeparationResolveSpeed * dt;
                u.Position = new Vector3(u.Position.x, 0f, u.Position.z);
            }
        }
    }
}
