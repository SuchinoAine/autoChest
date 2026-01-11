using UnityEngine;

namespace AutoChess.Core
{
    public interface IBattleEventSink
    {
        void OnMove(BattleWorld w, Unit u, Vector3 from, Vector3 to);
        void OnAttack(BattleWorld w, Unit attacker, Unit target, float damage);
        void OnDeath(BattleWorld w, Unit dead, Unit killer);
        void OnEnd(BattleWorld w, Team winner);
    }
}
