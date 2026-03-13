using System.Collections.Generic;
using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public int PlayerCoins { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 1; // ✅ 新增：玩家等级
        public int RerollCost = 2;

        [Header("卡池配置")]
        [Tooltip("拖入创建好的 Card Pool 数据资产")]
        public CardPoolSO cardPoolConfig; // ✅ 替换掉原本的 flat List

        private List<CardDataSO> _currentShopUnits = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => GameEventBus.OnEnterPreparationPhase += OnPreparationPhaseStarted;
        private void OnDisable() => GameEventBus.OnEnterPreparationPhase -= OnPreparationPhaseStarted;

        private void OnPreparationPhaseStarted()
        {
            AddCoins(5);
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

            if (PlayerCoins >= unit.cost)
            {
                AddCoins(-unit.cost);
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
            _currentShopUnits.Clear();
            
            if (cardPoolConfig == null)
            {
                Debug.LogError("[ShopManager] 未配置 CardPoolConfig！");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                // ✅ 核心修改：让卡池根据玩家当前等级去计算概率并刷卡
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

        // 可以提供一个升级接口供后续扩展
        public void LevelUp()
        {
            PlayerLevel++;
            Debug.Log($"[ShopManager] 玩家升到了 {PlayerLevel} 级！商店刷新概率已改变。");
        }
    }
}