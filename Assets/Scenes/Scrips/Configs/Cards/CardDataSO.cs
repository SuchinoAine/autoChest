using UnityEngine;
using System.Collections.Generic;

namespace AutoChess.Configs
{
    // 定义单个羁绊的数据
    [CreateAssetMenu(fileName = "NewCardData", menuName = "AutoChess/CardData")]
    public class CardDataSO : ScriptableObject
    {
        [Header("基础信息")]
        public string unitId;
        public string unitName;
        public int cost = 1;
        
        [Header("UI 表现")]
        public Sprite cardImage;     // 对应 Card -> image
        public Sprite borderImage;   // 对应 Card -> border (不同品质不同边框)
        
        [Header("羁绊/种族职业")]
        public List<BondDataSO> bonds = new List<BondDataSO>();

        [Header("模型与战斗")]
        public GameObject prefab;
        public float maxHp = 100f;
        public float attackDamage = 10f;
        public float attackRange = 1.5f;
    }
}