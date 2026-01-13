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

        // Controller（行动逻辑）
        public IBattleController BattleController { get; set; }
        
        // 系统组件：由 BattleWorld 统一调度
        public readonly SystemBuff buffSystem = new();
        public readonly SystemSkill skillSystem = new();

        // 事件 sinks（表现层/记录层都挂这）
        public readonly List<IBattleEventSink> Sinks = new();
        public void AddSink(IBattleEventSink s) { if (s != null) Sinks.Add(s); }

        // focus：调度策略（每帧清空）
        private readonly Dictionary<string, int> _focusCount = new();


        public void Tick(float dt)
        {
            if (IsEnded) return;
            // 1) 状态系统（可能会造成伤害/位移/增益等）
            buffSystem.Update(this, dt);
            // 2) 技能系统（释放技能 -> effect 结算）
            skillSystem.Update(this, dt);
            // 3) 时间推进
            Time += dt;
            // 4) 普攻CD
            foreach (var u in Units) if (!u.IsDead) u.TickCooldown(dt);
            // 5) 集火索敌清空
            _focusCount.Clear();

            int n = Units.Count;
            if (n == 0) return;

            // 每帧换一个起点：让'谁先行动'在长期统计上均匀
            int start = TickIndex % n;
            for (int k = 0; k < n; k++)
            {
                var u = Units[(start + k) % n];
                if (u.IsDead) continue;
                BattleController.StepUnit(this, u, dt);
            }

            // tick index++
            TickIndex++;
            CheckEnd();
        }

        // ===== focus 原语 =====
        internal void RegisterFocus(string targetId)
        {
            if (!_focusCount.ContainsKey(targetId)) _focusCount[targetId] = 0;
            _focusCount[targetId]++;
        }
        internal int GetFocusCount(string targetId)=> _focusCount.TryGetValue(targetId, out var c) ? c : 0;

        // ===== 统一结算入口 =====
        public void DealDamage(Unit source, Unit target, float amount)
        {
            if (target == null || target.IsDead) return;
            if (amount <= 0f) return;
            target.Hp -= amount;
            // 复用现有事件：OnAttack 表示一次“造成伤害”
            EmitAttack(source, target, amount);
            if (target.IsDead)
            {
                EmitDeath(target, source);
            }
        }

        // ===== Emit：事件出口 =====
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
                if (Sinks[i] is System.IDisposable d)
                {
                    try { d.Dispose(); } catch {}
                }
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
