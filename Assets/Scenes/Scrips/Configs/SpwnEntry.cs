using System;
using UnityEngine;
using AutoChess.Core;

namespace AutoChess.Configs
{
    [Serializable]
    public class SpawnEntry
    {
        // 统一使用 CardDataSO
        public CardDataSO cardData;
        public Team team;
        public Vector3 startPos;
    }
}