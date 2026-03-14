using UnityEngine;
using System.Collections.Generic;

namespace AutoChess.Configs
{
    [CreateAssetMenu(menuName = "AutoChess/Card Data")]
    public class CardDataSO : ScriptableObject
    {
        [Header("商店与表现 (Shop & UI)")]
        public string unitName;
        public int cost;
        public Sprite cardImage;
        public Sprite borderImage;
        public GameObject prefab;
        public List<BondDataSO> bonds;

        [Header("战斗基础属性 (Combat Stats)")]
        public float hp;
        public float atk;
        public float atkInterval;   // 攻击间隔 (秒)
        public float moveSpeed;
        public float range;         // 攻击距离
        public float radius;        // 碰撞/体积半径
        public bool isranged;       // 是否为远程
        
        [Header("技能配置 (Skills)")]
        public SkillDefSO basicAttack; // 普攻
        public SkillDefSO defaultSkill; // 默认主动技能 (可空)

        // 高星星数值

    }
}