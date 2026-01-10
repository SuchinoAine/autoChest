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
        public bool IsEnded { get; private set; }
        public Team? Winner { get; private set; }
        public void Add(Unit u) => Units.Add(u);  // 添加单位
        public void ResetLogs() => Logs.Clear();  // 重置日志
        public AIConfig AiConfig { get; set; }  // AI 配置参数
        private const float SeparationWeight = 2.0f;    // 分离强度
        private const float SeparationRange = 1.5f;     // 作用范围
        private const float MaxSeparationSpeed = 3.0f;  // 分离附加速度上限


        public void Tick(float dt)
        {
            if (IsEnded) return;
            Time += dt;
            // cooldown
            foreach (var u in Units)
                if (!u.IsDead) u.TickCooldown(dt);
            // actions: simple order
            foreach (var u in Units)
            {
                if (u.IsDead) continue;
                var target = FindBestTargetByScore(u);
                if (target == null) continue;

                float dist = Vector3.Distance(target.Position, u.Position);

                // in range -> attack
                if (dist <= u.Range)
                {
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
                else
                {
                    // 带 separation 的 move toward target 移动方向
                    Vector3 toTarget = target.Position - u.Position;
                    toTarget.y = 0f;
                    Vector3 dirToTarget = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
                    Vector3 sep = ComputeSeparation(u);

                    // 合成移动方向
                    Vector3 moveDir = dirToTarget + SeparationWeight * sep;
                    if (moveDir.sqrMagnitude > 0.0001f) moveDir = moveDir.normalized;

                    // 更新位置
                    Vector3 oldPos = u.Position;
                    u.Position += moveDir * u.MoveSpeed * dt;
                    u.Position = new Vector3(u.Position.x, 0f, u.Position.z);

                    // 可选：防抖
                    if (Vector3.Distance(target.Position, u.Position) < 0.01f)
                        u.Position = target.Position;

                    // log
                    if ((u.Position - oldPos).sqrMagnitude > 0.0001f)
                        Logs.Add(new BattleLog(LogType.Move, Time, u.Id, "", u.Position, 0));
                }
            }

            CheckEnd();
        }

        private Unit FindBestTargetByScore(Unit self)
        {
            Unit best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var u in Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;

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
            float nearRef = AiConfig != null ? AiConfig.nearDistRef : 6.0f;

            float dist = Vector3.Distance(target.Position, self.Position);

            // 1. 残血优先：hp 越低分越高（用 1/(hp+1) 做简单反比）
            float lowHpScore = 1f / (target.Hp + 1f);

            // 2. 距离越近越优先：dist 越小分越高（线性映射到 [0,1]）
            float nearScore = Mathf.Clamp01(1f - (dist / nearRef));

            // 3. 可击杀：一刀能杀就加分
            float killableScore = (self.Atk >= target.Hp) ? 1f : 0f;

            return wLowHp * lowHpScore + wNear * nearScore + wKillable * killableScore;
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




        private void CheckEnd()
        {
            var aliveA = Units.Any(u => !u.IsDead && u.Team == Team.A);
            var aliveB = Units.Any(u => !u.IsDead && u.Team == Team.B);

            if (aliveA && aliveB) return;

            IsEnded = true;
            Winner = aliveA ? Team.A : Team.B;
            var winnerName = Winner == Team.A ? "TeamA" : "TeamB";
            Logs.Add(new BattleLog(LogType.End, Time, winnerName, "", Vector3.zero, 0));
        }



    }
}
