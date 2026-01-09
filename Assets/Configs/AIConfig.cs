using UnityEngine;

[CreateAssetMenu(menuName = "AutoChess/AIConfig")]
public class AIConfig : ScriptableObject
{
    [Header("Target scoring weights")]
    public float wLowHp = 1.0f;        // 越残血越优先
    public float wNear = 0.7f;         // 越近越优先
    public float wKillable = 1.5f;     // 可击杀强烈优先

    [Header("Normalization")]
    public float nearDistRef = 6.0f;   // 距离归一化参考
}