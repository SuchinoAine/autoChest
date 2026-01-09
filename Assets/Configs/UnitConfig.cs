using UnityEngine;

[CreateAssetMenu(menuName = "AutoChess/UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public string id;
    public float hp;
    public float atk;
    public float atkInterval;
    public float moveSpeed;
    public float range;
}
