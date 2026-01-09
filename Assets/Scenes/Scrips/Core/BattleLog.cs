using UnityEngine;

namespace AutoChess.Core
{
    public enum LogType { Move, Attack, Death, End }

    public readonly struct BattleLog
    {
        public readonly LogType Type;
        public readonly float Time;
        public readonly string A;
        public readonly string B;
        public readonly float Value;
        public readonly float X_Value;
        public readonly float Y_Value;

        public BattleLog(LogType type, float time, string a, string b, Vector2 pos, float value)
        {
            Type = type;
            Time = time;
            A = a;
            B = b;
            Value = value;
            X_Value = pos[0];
            Y_Value = pos[1];
        }

        public override string ToString()
        {
            return Type switch
            {
                LogType.Move => $"[{Time:F2}] {A} moves to {X_Value:F2},{Y_Value:F2}",
                LogType.Attack => $"[{Time:F2}] {A} hits {B} for {Value:F1}",
                LogType.Death  => $"[{Time:F2}] {A} died",
                LogType.End    => $"[{Time:F2}] Battle end. Winner: {A}",
                _ => $"[{Time:F2}] {Type}"
            };
        }
    }
}
