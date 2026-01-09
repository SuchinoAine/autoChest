using AutoChess.Core;
using UnityEngine;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

    public void SetPos(Vector2 pos)
    {
        var p = transform.position;
        p.x = pos.x;
        p.z = pos.y; // 注意：Vector2 的 y 映射到 Unity 的 z
        transform.position = p;
    }
    }
}
