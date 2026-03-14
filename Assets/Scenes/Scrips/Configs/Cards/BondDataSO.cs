using UnityEngine;
using System.Collections.Generic;

namespace AutoChess.Configs
{
    // 定义单个羁绊的数据
    [System.Serializable]
    [CreateAssetMenu(fileName = "NewBondData", menuName = "AutoChess/BondData")]
    public class BondDataSO : ScriptableObject
    {
        [Header("基础信息")]
        public string bondName;   // 羁绊名称
        public Sprite bondIcon;   // 羁绊图标
    }
}