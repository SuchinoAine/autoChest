using System;

namespace AutoChess.Core
{
    public enum Team { A, B }

    public class Unit
    {
        public readonly string Id;
        public readonly Team Team;

        public float Hp;
        public float Atk;
        public float AtkInterval;   // seconds per attack
        public float MoveSpeed;     // units per second
        public float Range;         // attack range
        public float X;             // 1D position for MVP

        private float _atkCooldown;

        public bool IsDead => Hp <= 0;

        public Unit(string id, Team team, float hp, float atk, float atkInterval, float moveSpeed, float range, float startX)
        {
            Id = id;
            Team = team;
            Hp = hp;
            Atk = atk;
            AtkInterval = atkInterval;
            MoveSpeed = moveSpeed;
            Range = range;
            X = startX;
            _atkCooldown = 0f;
        }

        public void TickCooldown(float dt)
        {
            _atkCooldown = Math.Max(0f, _atkCooldown - dt);
        }

        public bool CanAttack() => _atkCooldown <= 0f;

        public void ResetAttackCooldown() => _atkCooldown = AtkInterval;
    }
}
