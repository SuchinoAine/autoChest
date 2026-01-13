using System.Collections.Generic;
using AutoChess.Core;
using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.View
{
    /// <summary>
    /// 战斗事件的视图层实现，不能放核心逻辑层
    /// </summary>
    public sealed class BattleViewSink : IBattleEventSink
    {
        private readonly Dictionary<string, UnitView> _views;

        public BattleViewSink(Dictionary<string, UnitView> views) => _views = views;

        public void OnMove(BattleWorld w, Unit u, Vector3 from, Vector3 to)
        {
            if (_views.TryGetValue(u.Id, out var v))
                v.SetPos(to);
        }

        public void OnAttack(BattleWorld w, Unit attacker, Unit target, float damage)
        {
            Debug.Log($"VIEW-Unit {attacker.Id} attacked {target.Id} for {attacker.Atk} damage.");
            // nope for now
        }

        public void OnDeath(BattleWorld w, Unit dead, Unit killer)
        {
            if (_views.TryGetValue(dead.Id, out var v))
            {
                // 方案A：直接隐藏（推荐，后面方便做对象池）
                // view.gameObject.SetActive(false);
                // 方案B：播放死亡动画
                v.PlayDeathFade(0.25f);
                _views.Remove(dead.Id);
                Debug.Log($"Unit {dead.Id} died.");
            }
        }

        public void OnEnd(BattleWorld w, Team winner)
        {
            Debug.Log($"Battle ended. Winner: {winner}");
        }

        public void OnApplyBuff(Unit target, Unit source, BuffDefSO buffId, List<BuffDefSO> stacks)
        {
            // buff 视效暂不实现
            Debug.Log($"Buff {buffId.id} applied to Unit {target.Id} from Unit {source.Id}");
        }

        public void OnRemoveBuff(Unit target, BuffDefSO buffId)
        {
            // buff 移除视效暂不实现
            Debug.Log($"Buff {buffId.id} removed from Unit {target.Id}");
        }
    }
}
