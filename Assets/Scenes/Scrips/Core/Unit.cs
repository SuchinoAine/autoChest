using System;
using UnityEngine;

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
        public Vector3 Position;    // 3D position
        public float Radius;        // unit size

        private float _atkCooldown;

        public bool IsDead => Hp <= 0;

        public Unit(string id, Team team, float hp, float atk, float atkInterval, float moveSpeed, 
                    float range, Vector3 startPos, float radius)
        {
            Id = id;
            Team = team;
            Hp = hp;
            Atk = atk;
            AtkInterval = atkInterval;
            MoveSpeed = moveSpeed;
            Range = range;
            Position = startPos;
            _atkCooldown = 0f;
            Radius = radius;
        }


        public void TickCooldown(float dt)
        {
            _atkCooldown = Math.Max(0f, _atkCooldown - dt);
        }

        public bool CanAttack() => _atkCooldown <= 0f;

        public void ResetAttackCooldown() => _atkCooldown = AtkInterval;
    }
}
