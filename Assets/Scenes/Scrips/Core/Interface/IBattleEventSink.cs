using System.Collections.Generic;
using AutoChess.Configs;
using UnityEngine;


namespace AutoChess.Core
{
    public interface IBattleEventSink
    {
        void OnMove(BattleWorld w, Unit u, Vector3 from, Vector3 to);
        void OnAttack(BattleWorld w, Unit attacker, Unit target, float damage);
        void OnApplyBuff(Unit target, Unit source, BuffDefSO buffId, List<BuffDefSO> stacks);
        void OnRemoveBuff(Unit target, BuffDefSO buffId);
        void OnDeath(BattleWorld w, Unit dead, Unit killer);
        void OnEnd(BattleWorld w, Team winner);
    }
}
