using System.Collections.Generic;
using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public int PlayerCoins { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 1;
        public int RerollCost = 2;

        [Header("卡池配置")]
        public CardPoolSO cardPoolConfig; 

        private List<CardDataSO> _currentShopUnits = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ✅ 初始化公共卡池库存
            if (cardPoolConfig != null)
            {
                cardPoolConfig.InitializePool();
            }
        }

        private void OnEnable() => GameEventBus.OnEnterPreparationPhase += OnPreparationPhaseStarted;
        private void OnDisable() => GameEventBus.OnEnterPreparationPhase -= OnPreparationPhaseStarted;

        private void OnPreparationPhaseStarted()
        {
            AddCoins(80); // 每回合初始金币奖励
            RefreshShop(free: true);
        }

        public void RequestReroll()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;

            if (PlayerCoins >= RerollCost)
            {
                AddCoins(-RerollCost);
                RefreshShop(free: false);
            }
            else
            {
                Debug.LogWarning("[ShopManager] 金币不足，无法D牌");
            }
        }

        public void RequestPurchase(int slotIndex)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;
            if (slotIndex < 0 || slotIndex >= _currentShopUnits.Count) return;

            CardDataSO unit = _currentShopUnits[slotIndex];
            if (unit == null) return; 

            // 检查备战区是否已满
            if (BenchManager.Instance != null && BenchManager.Instance.IsBenchFull())
            {
                Debug.LogWarning("[ShopManager] 备战区已满！请先出售或上阵棋子。");
                return;
            }

            if (PlayerCoins >= unit.cost)
            {
                AddCoins(-unit.cost);
                
                // 挖空该槽位。注意：因为槽位变成了 null，所以在下次刷新时，这张卡不会被退回卡池！
                _currentShopUnits[slotIndex] = null; 
                
                GameEventBus.OnShopRefreshed?.Invoke(_currentShopUnits);
                GameEventBus.OnUnitPurchased?.Invoke(unit); 
                
                Debug.Log($"[ShopManager] 成功购买: {unit.unitName}");
            }
            else
            {
                Debug.LogWarning("[ShopManager] 金币不足，买不起这张牌");
            }
        }

        private void RefreshShop(bool free)
        {
            if (cardPoolConfig == null) return;

            // ✅ 核心逻辑：刷新前，将当前商店里【未购买】的卡牌退回公共卡池
            foreach (var card in _currentShopUnits)
            {
                if (card != null)
                {
                    cardPoolConfig.ReturnCard(card);
                }
            }
            _currentShopUnits.Clear();

            // 抽取 5 张新卡
            for (int i = 0; i < 5; i++)
            {
                CardDataSO drawnCard = cardPoolConfig.RollCard(PlayerLevel);
                _currentShopUnits.Add(drawnCard);
            }
            
            GameEventBus.OnShopRefreshed?.Invoke(_currentShopUnits);
        }

        private void AddCoins(int amount)
        {
            PlayerCoins += amount;
            GameEventBus.OnCoinChanged?.Invoke(PlayerCoins);
        }

        public void LevelUp()
        {
            PlayerLevel++;
            Debug.Log($"[ShopManager] 玩家升到了 {PlayerLevel} 级！");
        }
        
        // 后续当你开发“出售棋子”功能时，可以直接调用这个方法将卖掉的棋子退回卡池
        public void SellCardToPool(CardDataSO card)
        {
            if (cardPoolConfig != null)
            {
                cardPoolConfig.ReturnCard(card);
                Debug.Log($"[ShopManager] {card.unitName} 被出售，已退回公共卡池。");
            }
        }
    }
}