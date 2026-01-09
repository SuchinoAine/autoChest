using AutoChess.Core;
using UnityEngine;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

        public void SetX(float x)
        {
            var p = transform.position;
            p.x = x;
            transform.position = p;
        }
    }
}
