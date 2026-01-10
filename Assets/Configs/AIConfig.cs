using UnityEngine;

[CreateAssetMenu(menuName = "AutoChess/AIConfig")]
public class AIConfig : ScriptableObject
{
    [Header("Target scoring weights")]
    public float wLowHp = 1.0f;     // 血量权重
    public float wNear = 0.7f;      // 距离权重
    public float wKillable = 1.5f;  // 可击杀权重 0.6

    [Header("Normalization")]
    public float nearDistRef = 6.0f;    // 距离归一化参考

    [Header("Targeting Policy")]
    public float wFocus = 0f;         // 集火权重（0.2~0.8）
    public float wPreferRangedTarget = 0.6f; // 远程打远程权重

}