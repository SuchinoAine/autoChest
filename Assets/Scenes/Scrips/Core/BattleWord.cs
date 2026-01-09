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

        public void Add(Unit u) => Units.Add(u);

        public void ResetLogs() => Logs.Clear();

        public AIConfig AiConfig { get; set; }


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

                float dist = Vector2.Distance(target.Position, u.Position);

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
                            // Logs.Add(new BattleLog(LogType.Death, Time, target.Id, u.Position, 0));
                            // SelfDeath log
                            Logs.Add(new BattleLog(LogType.Death, Time, target.Id, "", u.Position, 0));
                        }
                            
                    }
                }
                else
                {
                    // move toward target
                    Vector2 dir = (target.Position - u.Position).normalized;
                    Vector2 oldPos = u.Position;
                    u.Position += dir * u.MoveSpeed * dt;
                    // avoid oscillation: if we overshoot, clamp to target
                    if (Vector2.Distance(target.Position, u.Position) < 0.01f)
                        u.Position = target.Position;

                    if ((u.Position - oldPos).sqrMagnitude > 0.0001f)
                        Logs.Add(new BattleLog(LogType.Move, Time, u.Id, "", u.Position, 0)); 
                        // Value 字段先不存坐标
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

            float dist = Vector2.Distance(target.Position, self.Position);

            // 1. 残血优先：hp 越低分越高（用 1/(hp+1) 做简单反比）
            float lowHpScore = 1f / (target.Hp + 1f);

            // 2. 距离越近越优先：dist 越小分越高（线性映射到 [0,1]）
            float nearScore = Mathf.Clamp01(1f - (dist / nearRef));

            // 3. 可击杀：一刀能杀就加分
            float killableScore = (self.Atk >= target.Hp) ? 1f : 0f;

            return wLowHp * lowHpScore + wNear * nearScore + wKillable * killableScore;
        }

        private void CheckEnd()
        {
            var aliveA = Units.Any(u => !u.IsDead && u.Team == Team.A);
            var aliveB = Units.Any(u => !u.IsDead && u.Team == Team.B);

            if (aliveA && aliveB) return;

            IsEnded = true;
            Winner = aliveA ? Team.A : Team.B;
            var winnerName = Winner == Team.A ? "TeamA" : "TeamB";
            Logs.Add(new BattleLog(LogType.End, Time, winnerName, "", Vector2.zero, 0));
        }
    }
}
