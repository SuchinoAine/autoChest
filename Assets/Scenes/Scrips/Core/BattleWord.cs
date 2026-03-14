using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoChess.Core
{
    public class BattleWorld
    {
        public readonly List<Unit> Units = new();
        public void Add(Unit u) => Units.Add(u);
        public float Time { get; private set; }
        public int TickIndex { get; private set; } = 0;
        public bool IsEnded { get; private set; }
        public Team Winner { get; private set; }
        public AIConfig AiConfig { get; set; }

        public IBattleController BattleController { get; set; }
        public readonly SystemBuff buffSystem = new();
        public readonly SystemSkill skillSystem = new();
        public readonly List<IBattleEventSink> Sinks = new();
        
        public void AddSink(IBattleEventSink s) { if (s != null) Sinks.Add(s); }
        private readonly Dictionary<string, int> _focusCount = new();

        public void Tick(float dt)
        {
            if (IsEnded) return;
            buffSystem.Update(this, dt);
            skillSystem.Update(this, dt);
            Time += dt;
            
            foreach (var u in Units) 
            {
                if (!u.IsDead) u.TickCooldown(dt);
            }
            
            _focusCount.Clear();

            int n = Units.Count;
            if (n == 0) return;

            int start = TickIndex % n;
            for (int k = 0; k < n; k++)
            {
                var u = Units[(start + k) % n];
                if (u.IsDead) continue;
                BattleController.StepUnit(this, u, dt);
            }

            // ✅ 核心升级：全局 PBD 碰撞约束！
            ResolveGlobalCollisions();

            TickIndex++;
            CheckEnd();
        }

        private void ResolveGlobalCollisions()
        {
            int iters = 3; // PBD 迭代次数，3次已经足够稳定
            float contactEps = 0.02f;

            for (int iter = 0; iter < iters; iter++)
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    var u1 = Units[i];
                    if (u1.IsDead) continue;

                    for (int j = i + 1; j < Units.Count; j++)
                    {
                        var u2 = Units[j];
                        if (u2.IsDead) continue;

                        Vector3 d = u1.Position - u2.Position;
                        d.y = 0f;
                        float distSq = d.sqrMagnitude;
                        
                        // ✅ 防呆保护：如果你在 CardDataSO 里忘了填 Radius，默认给 0.5
                        float r1 = u1.Radius > 0.1f ? u1.Radius : 0.5f;
                        float r2 = u2.Radius > 0.1f ? u2.Radius : 0.5f;

                        float minDist = r1 + r2 + contactEps;

                        if (distSq < minDist * minDist)
                        {
                            float dist = Mathf.Sqrt(distSq);
                            Vector3 n;
                            if (dist < 0.001f) // 两个棋子完全重叠时的极端处理
                            {
                                n = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
                                if (n == Vector3.zero) n = Vector3.right;
                            }
                            else
                            {
                                n = d / dist;
                            }

                            float overlap = minDist - dist;
                            
                            // 互相挤压：各承担 50% 的推力
                            Vector3 corr = n * (overlap * 0.5f);
                            u1.Position += corr;
                            u2.Position -= corr;
                        }
                    }
                }
            }

            // 碰撞解算完后，统一通知表现层更新位置
            foreach (var u in Units)
            {
                if (!u.IsDead)
                {
                    EmitMove(u, u.Position, u.Position);
                }
            }
        }

        internal void RegisterFocus(string targetId)
        {
            if (!_focusCount.ContainsKey(targetId)) _focusCount[targetId] = 0;
            _focusCount[targetId]++;
        }
        
        internal int GetFocusCount(string targetId)=> _focusCount.TryGetValue(targetId, out var c) ? c : 0;

        public void DealDamage(Unit source, Unit target, float amount)
        {
            if (target == null || target.IsDead) return;
            if (amount <= 0f) return;
            target.Hp -= amount;
            EmitAttack(source, target, amount);
            if (target.IsDead) EmitDeath(target, source);
        }

        internal void EmitMove(Unit u, Vector3 from, Vector3 to)
        {
            for (int i = 0; i < Sinks.Count; i++) Sinks[i].OnMove(this, u, from, to);
        }

        internal void EmitAttack(Unit attacker, Unit target, float damage)
        {
            for (int i = 0; i < Sinks.Count; i++) Sinks[i].OnAttack(this, attacker, target, damage);
        }

        internal void EmitDeath(Unit dead, Unit killer)
        {
            for (int i = 0; i < Sinks.Count; i++) Sinks[i].OnDeath(this, dead, killer);
        }

        internal void EmitEnd(Team winner)
        {
            for (int i = 0; i < Sinks.Count; i++) Sinks[i].OnEnd(this, winner);
        }

        public void Shutdown()
        {
            for (int i = 0; i < Sinks.Count; i++)
            {
                if (Sinks[i] is System.IDisposable d) { try { d.Dispose(); } catch {} }
            }
        }

        private void CheckEnd()
        {
            var aliveA = Units.Any(u => !u.IsDead && u.Team == Team.A);
            var aliveB = Units.Any(u => !u.IsDead && u.Team == Team.B);
            if (aliveA && aliveB) return;

            IsEnded = true;
            Winner = aliveA ? Team.A : Team.B;

            EmitEnd(Winner);
            Shutdown();
        }
    }
}