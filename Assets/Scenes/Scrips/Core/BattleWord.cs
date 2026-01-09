using System;
using System.Collections.Generic;
using System.Linq;

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

                var target = FindBestTarget(u);
                if (target == null) continue;

                var dist = Math.Abs(target.X - u.X);

                // in range -> attack
                if (dist <= u.Range)
                {
                    if (u.CanAttack())
                    {
                        target.Hp -= u.Atk;
                        u.ResetAttackCooldown();
                        Logs.Add(new BattleLog(LogType.Attack, Time, u.Id, target.Id, u.Atk));

                        if (target.IsDead)
                            Logs.Add(new BattleLog(LogType.Death, Time, target.Id, "", 0));
                    }
                }
                else
                {
                    // move toward target
                    var dir = Math.Sign(target.X - u.X);
                    var oldX = u.X;
                    u.X += dir * u.MoveSpeed * dt;

                    // avoid oscillation: clamp if we pass
                    if (Math.Sign(target.X - u.X) != dir) u.X = target.X;

                    if (Math.Abs(u.X - oldX) > 0.0001f)
                        Logs.Add(new BattleLog(LogType.Move, Time, u.Id, "", u.X));
                }
            }

            CheckEnd();
        }

        private Unit FindBestTarget(Unit self)
        {
            // MVP: nearest alive enemy
            Unit best = null;
            var bestDist = float.MaxValue;
            foreach (var u in Units)
            {
                if (u.IsDead) continue;
                if (u.Team == self.Team) continue;

                var d = Math.Abs(u.X - self.X);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = u;
                }
            }
            return best;
        }

        private void CheckEnd()
        {
            var aliveA = Units.Any(u => !u.IsDead && u.Team == Team.A);
            var aliveB = Units.Any(u => !u.IsDead && u.Team == Team.B);

            if (aliveA && aliveB) return;

            IsEnded = true;
            Winner = aliveA ? Team.A : Team.B;
            var winnerName = Winner == Team.A ? "TeamA" : "TeamB";
            Logs.Add(new BattleLog(LogType.End, Time, winnerName, "", 0));
        }
    }
}
