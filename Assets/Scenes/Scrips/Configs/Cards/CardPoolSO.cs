using System.Collections.Generic;
using UnityEngine;

namespace AutoChess.Configs
{
    // 定义不同等级下的卡牌刷新概率
    [System.Serializable]
    public class ShopDropRate
    {
        [Tooltip("1费到5费卡的刷新概率(百分比，总和应为100)")]
        public float[] tierRates = new float[5];
    }

    [CreateAssetMenu(fileName = "NewCardPool", menuName = "AutoChess/Card Pool")]
    public class CardPoolSO : ScriptableObject
    {
        [Header("各品质卡池分类")]
        public List<CardDataSO> whiteCards = new List<CardDataSO>();  // 1费
        public List<CardDataSO> greenCards = new List<CardDataSO>();  // 2费
        public List<CardDataSO> blueCards = new List<CardDataSO>();   // 3费
        public List<CardDataSO> violetCards = new List<CardDataSO>(); // 4费
        public List<CardDataSO> goldCards = new List<CardDataSO>();   // 5费

        [Header("各等级刷新概率表")]
        [Tooltip("列表索引对应玩家等级，例如元素0代表1级，元素1代表2级...")]
        public List<ShopDropRate> levelDropRates = new List<ShopDropRate>();

        /// <summary>
        /// 根据玩家当前等级，抽取一张卡牌
        /// </summary>
        public CardDataSO RollCard(int playerLevel)
        {
            if (levelDropRates == null || levelDropRates.Count == 0)
            {
                Debug.LogError("[CardPoolSO] 刷新概率表未配置！");
                return GetRandomCardFromTier(1); // 兜底返回1费卡
            }

            // 1. 获取当前等级的概率 (防越界保护)
            int levelIndex = Mathf.Clamp(playerLevel - 1, 0, levelDropRates.Count - 1);
            ShopDropRate rates = levelDropRates[levelIndex];

            // 2. 掷骰子决定稀有度 (0~100)
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            int rolledTier = 1; // 默认1费

            for (int i = 0; i < rates.tierRates.Length; i++)
            {
                cumulative += rates.tierRates[i];
                if (roll <= cumulative)
                {
                    rolledTier = i + 1; // 抽中的品质 (1到5)
                    break;
                }
            }

            // 3. 从对应的品质池子里拿一张卡
            return GetRandomCardFromTier(rolledTier);
        }

        private CardDataSO GetRandomCardFromTier(int tier)
        {
            List<CardDataSO> targetList = tier switch
            {
                1 => whiteCards,
                2 => greenCards,
                3 => blueCards,
                4 => violetCards,
                5 => goldCards,
                _ => whiteCards
            };

            // 容错：如果你抽到了高费卡，但卡池里还没配置高费卡，自动降级给一张1费卡
            if (targetList == null || targetList.Count == 0)
            {
                Debug.LogWarning($"[CardPoolSO] 尝试抽取 {tier} 费卡，但该卡池为空！");
                return whiteCards.Count > 0 ? whiteCards[Random.Range(0, whiteCards.Count)] : null;
            }

            return targetList[Random.Range(0, targetList.Count)];
        }
    }
}