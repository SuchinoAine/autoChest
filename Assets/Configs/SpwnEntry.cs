using System;
using UnityEngine;
using AutoChess.Core;

namespace AutoChess.Configs
{
[Serializable]
public class SpawnEntry
    {
        public UnitConfig config;
        public Team team;
        public Vector3 startPos;
    }
}
