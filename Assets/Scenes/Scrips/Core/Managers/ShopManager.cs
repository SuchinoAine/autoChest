using System.Collections.Generic;
using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("基础经济设定")]
        public int RerollCost = 2;
        public int PlayerCoins { get; private set; } = 0;

        [Header("经验与等级设定")]
        public int MaxLevel = 9;
        public int PlayerLevel { get; private set; } = 1;
        public int PlayerExp { get; private set; } = 0;
        [Tooltip("索引对应当前等级。例如填入: 0, 2, 2, 6, 10, 20, 36, 56, 80")]
        public int[] expCurve = new int[] { 0, 2, 2, 6, 10, 20, 36, 56, 80 }; 

        [Header("卡池配置")]
        public CardPoolSO cardPoolConfig; 

        private List<CardDataSO> _currentShopUnits = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化公共卡池库存
            if (cardPoolConfig != null)
            {
                cardPoolConfig.InitializePool();
            }
        }

        private void Start()
        {
            // 游戏启动时广播一次初始等级，用于初始化 UI
            BroadcastLevelExp();
        }

        private void OnEnable() => GameEventBus.OnEnterPreparationPhase += OnPreparationPhaseStarted;
        private void OnDisable() => GameEventBus.OnEnterPreparationPhase -= OnPreparationPhaseStarted;

        private void OnPreparationPhaseStarted()
        {
            AddCoins(500); // 每回合基础收入
            AddExp(2);   // 每回合自然增长经验
            RefreshShop(free: true); // 每回合免费刷新
        }

        // ================= 经验与升级系统 =================

        public void RequestBuyExp()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;

            if (PlayerLevel >= MaxLevel)
            {
                Debug.LogWarning("[ShopManager] 已经是最高等级了！");
                return;
            }

            if (PlayerCoins >= 4)
            {
                AddCoins(-4);
                AddExp(4); // 花4块钱买4经验
                Debug.Log("[ShopManager] 成功购买 4 点经验");
            }
            else
            {
                Debug.LogWarning("[ShopManager] 金币不足，无法购买经验");
            }
        }

        private void AddExp(int amount)
        {
            if (PlayerLevel >= MaxLevel) return;

            PlayerExp += amount;
            
            // 循环检测，防止一次性加太多经验连升两级
            while (PlayerLevel < MaxLevel && PlayerExp >= expCurve[PlayerLevel])
            {
                PlayerExp -= expCurve[PlayerLevel];
                PlayerLevel++;
                Debug.Log($"[ShopManager] 升级啦！当前等级: {PlayerLevel}");
            }

            BroadcastLevelExp();
        }

        private void BroadcastLevelExp()
        {
            int nextExp = PlayerLevel < MaxLevel ? expCurve[PlayerLevel] : 0;
            GameEventBus.OnLevelExpChanged?.Invoke(PlayerLevel, PlayerExp, nextExp);
        }

        // ================= 商店与购买系统 =================

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

            // 刷新前，将当前商店里【未购买】的卡牌退回公共卡池
            foreach (var card in _currentShopUnits)
            {
                if (card != null)
                {
                    cardPoolConfig.ReturnCard(card);
                }
            }
            _currentShopUnits.Clear();

            // 根据当前玩家等级抽取 5 张新卡
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
        
        // 供后续出售棋子使用
        public void SellCardToPool(CardDataSO card)
        {
            if (cardPoolConfig != null && card != null)
            {
                cardPoolConfig.ReturnCard(card);
                Debug.Log($"[ShopManager] {card.unitName} 被出售，已退回公共卡池。");
            }
        }
    }
}