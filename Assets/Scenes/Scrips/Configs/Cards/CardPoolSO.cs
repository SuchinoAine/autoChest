using System.Collections.Generic;
using UnityEngine;

namespace AutoChess.Configs
{
    [System.Serializable]
    public class ShopDropRate
    {
        [Tooltip("1费到5费卡的刷新概率(百分比，总和应为100)")]
        public float[] tierRates = new float[5];
    }

    [CreateAssetMenu(fileName = "NewCardPool", menuName = "AutoChess/Card Pool")]
    public class CardPoolSO : ScriptableObject
    {
        [Header("各品质卡池分类 (种类)")]
        public List<CardDataSO> whiteCards = new List<CardDataSO>();  // 1费
        public List<CardDataSO> greenCards = new List<CardDataSO>();  // 2费
        public List<CardDataSO> blueCards = new List<CardDataSO>();   // 3费
        public List<CardDataSO> violetCards = new List<CardDataSO>(); // 4费
        public List<CardDataSO> goldCards = new List<CardDataSO>();   // 5费

        [Header("各品质单卡公共池数量 (张数)")]
        public int countCost1 = 30;
        public int countCost2 = 25;
        public int countCost3 = 18;
        public int countCost4 = 10;
        public int countCost5 = 9;

        [Header("各等级刷新概率表")]
        public List<ShopDropRate> levelDropRates = new List<ShopDropRate>();

        // 运行时缓存：记录每张卡牌当前在卡池中还剩余多少张
        private Dictionary<CardDataSO, int> _runtimePool = new Dictionary<CardDataSO, int>();

        /// <summary>
        /// 游戏开始时调用，根据设定的张数初始化公共卡池
        /// </summary>
        public void InitializePool()
        {
            _runtimePool.Clear();
            foreach (var card in whiteCards)  if (card != null) _runtimePool[card] = countCost1;
            foreach (var card in greenCards)  if (card != null) _runtimePool[card] = countCost2;
            foreach (var card in blueCards)   if (card != null) _runtimePool[card] = countCost3;
            foreach (var card in violetCards) if (card != null) _runtimePool[card] = countCost4;
            foreach (var card in goldCards)   if (card != null) _runtimePool[card] = countCost5;
            
            Debug.Log("[CardPoolSO] 公共卡池初始化完毕！");
        }

        /// <summary>
        /// 抽取一张卡牌，并从库存中扣除
        /// </summary>
        public CardDataSO RollCard(int playerLevel)
        {
            if (levelDropRates == null || levelDropRates.Count == 0) return null;

            int levelIndex = Mathf.Clamp(playerLevel - 1, 0, levelDropRates.Count - 1);
            ShopDropRate rates = levelDropRates[levelIndex];

            // 1. 根据概率决定出什么品质的卡
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            int rolledTier = 1; 

            for (int i = 0; i < rates.tierRates.Length; i++)
            {
                cumulative += rates.tierRates[i];
                if (roll <= cumulative)
                {
                    rolledTier = i + 1;
                    break;
                }
            }

            return DrawRandomCardFromTier(rolledTier);
        }

        private CardDataSO DrawRandomCardFromTier(int tier)
        {
            int clampedTier = Mathf.Clamp(tier, 1, 5);

            List<CardDataSO> GetTierList(int targetTier)
            {
                return targetTier switch
                {
                    1 => whiteCards,
                    2 => greenCards,
                    3 => blueCards,
                    4 => violetCards,
                    5 => goldCards,
                    _ => whiteCards
                };
            }

            // 从当前品质开始，向下逐级寻找有库存的卡。
            for (int currentTier = clampedTier; currentTier >= 1; currentTier--)
            {
                List<CardDataSO> targetList = GetTierList(currentTier);
                List<CardDataSO> availableCards = new List<CardDataSO>();

                foreach (var card in targetList)
                {
                    if (card != null && _runtimePool.TryGetValue(card, out int count) && count > 0)
                    {
                        availableCards.Add(card);
                    }
                }

                if (availableCards.Count == 0)
                {
                    continue;
                }

                if (currentTier != clampedTier)
                {
                    Debug.LogWarning($"[CardPoolSO] {clampedTier} 费卡池无库存，已回退到 {currentTier} 费抽卡。");
                }

                CardDataSO drawnCard = availableCards[Random.Range(0, availableCards.Count)];
                _runtimePool[drawnCard]--;
                return drawnCard;
            }

            Debug.LogWarning($"[CardPoolSO] 从 {clampedTier} 费向下到 1 费均无库存，本次轮空。");
            return null;
        }

        /// <summary>
        /// 将卡牌退回公共卡池（用于商店刷新、玩家售卖棋子、玩家淘汰）
        /// </summary>
        public void ReturnCard(CardDataSO card)
        {
            if (card == null) return;
            if (_runtimePool.ContainsKey(card))
            {
                _runtimePool[card]++;
            }
        }
    }
}