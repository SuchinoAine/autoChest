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

        [Header("经验与等级设定")]
        public int MaxLevel = 9;
        public int PlayerLevel { get; private set; } = 1;
        public int PlayerExp { get; private set; } = 0;
        public int[] expCurve = new int[] { 0, 2, 2, 6, 10, 20, 36, 56, 80 };

        [Header("卡池配置")]
        public CardPoolSO cardPoolConfig;

        private List<CardDataSO> _currentShopUnits = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (cardPoolConfig != null) cardPoolConfig.InitializePool();
        }

        private void Start() => BroadcastLevelExp();

        private void OnEnable() => GameEventBus.OnEnterPreparationPhase += OnPreparationPhaseStarted;
        private void OnDisable() => GameEventBus.OnEnterPreparationPhase -= OnPreparationPhaseStarted;

        private void OnPreparationPhaseStarted()
        {
            // 只有第 2 回合及以后，才自然增长经验
            if (GameManager.Instance != null && GameManager.Instance.CurrentRound > 1)
            {
                AddExp(2);
            }
            // 无论第几回合，都免费刷新一次商店
            RefreshShop(free: true);
        }

        // ================= 经验与升级 =================
        public void RequestBuyExp()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;
            if (PlayerLevel >= MaxLevel) return;

            // ✅ 找管家扣钱
            if (EconomyManager.Instance.SpendGold(4))
            {
                AddExp(4);
                Debug.Log("[ShopManager] 成功购买 4 点经验");
            }
        }

        private void AddExp(int amount)
        {
            if (PlayerLevel >= MaxLevel) return;
            PlayerExp += amount;

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

        // ================= 商店与购买 =================
        public void RequestReroll()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;

            // ✅ 找管家扣钱
            if (EconomyManager.Instance.SpendGold(RerollCost))
            {
                RefreshShop(free: false);
            }
        }

        public void RequestPurchase(int slotIndex)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;
            if (slotIndex < 0 || slotIndex >= _currentShopUnits.Count) return;

            CardDataSO unit = _currentShopUnits[slotIndex];
            if (unit == null) return;

            if (BenchManager.Instance != null && BenchManager.Instance.IsBenchFull())
            {
                Debug.LogWarning("[ShopManager] 备战区已满！");
                return;
            }

            // ✅ 找管家扣钱，商品按原价扣
            if (EconomyManager.Instance.SpendGold(unit.cost))
            {
                _currentShopUnits[slotIndex] = null;
                GameEventBus.OnShopRefreshed?.Invoke(_currentShopUnits);
                GameEventBus.OnUnitPurchased?.Invoke(unit);
            }
        }

        private void RefreshShop(bool free)
        {
            if (cardPoolConfig == null) return;
            foreach (var card in _currentShopUnits)
            {
                if (card != null) cardPoolConfig.ReturnCard(card);
            }
            _currentShopUnits.Clear();

            for (int i = 0; i < 5; i++)
            {
                _currentShopUnits.Add(cardPoolConfig.RollCard(PlayerLevel));
            }
            GameEventBus.OnShopRefreshed?.Invoke(_currentShopUnits);
        }

        public void SellCardToPool(CardDataSO card)
        {
            if (cardPoolConfig != null && card != null)
            {
                cardPoolConfig.ReturnCard(card);
            }
        }
    }
}