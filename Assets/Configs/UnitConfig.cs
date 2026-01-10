using UnityEngine;

[CreateAssetMenu(menuName = "AutoChess/UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public string id;
    public float hp;
    public float atk;
    public float atkInterval;  // seconds per attack
    public float moveSpeed;
    public float range;  // attack range
    public float redius; // unit size
    public bool isranged;  // is ranged unit
}
