using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using AutoChess.Configs;
using AutoChess.View;

namespace AutoChess.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI 面板")]
        public GameObject shopPanel;
        public Text coinText;
        public Text timerText;
        
        [Header("动态商店卡池")]
        public Transform shopContainer; // 卡牌们的父节点，建议挂个 Horizontal Layout Group
        public GameObject cardPrefab;   // 拖入做好的 Card 预制体
        
        // 缓存已经生成的卡牌 UI
        private List<ShopCardUI> _spawnedCards = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEventBus.OnEnterPreparationPhase += ShowShop;
            GameEventBus.OnEnterCombatPhase += HideShop;
            GameEventBus.OnCoinChanged += UpdateCoinUI;
            GameEventBus.OnShopRefreshed += UpdateShopUI;
        }

        private void OnDisable()
        {
            GameEventBus.OnEnterPreparationPhase -= ShowShop;
            GameEventBus.OnEnterCombatPhase -= HideShop;
            GameEventBus.OnCoinChanged -= UpdateCoinUI;
            GameEventBus.OnShopRefreshed -= UpdateShopUI;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Preparation)
            {
                if (timerText != null) 
                    timerText.text = Mathf.CeilToInt(GameManager.Instance.PhaseTimer).ToString();
            }
        }

        private void ShowShop() => shopPanel.SetActive(true);
        private void HideShop() => shopPanel.SetActive(false);
        private void UpdateCoinUI(int coins)
        {
            if (coinText != null) coinText.text = $"Coins: {coins}";
        }

        // ✅ 核心逻辑：动态生成和刷新卡牌
        private void UpdateShopUI(List<CardDataSO> shopUnits)
        {
            // 1. 数量不够时，实例化新的卡牌
            while (_spawnedCards.Count < shopUnits.Count)
            {
                GameObject newCardObj = Instantiate(cardPrefab, shopContainer);
                ShopCardUI cardUI = newCardObj.GetComponent<ShopCardUI>();
                _spawnedCards.Add(cardUI);
            }

            // 2. 遍历并刷新每一张卡牌的数据
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                if (i < shopUnits.Count)
                {
                    _spawnedCards[i].gameObject.SetActive(true);
                    _spawnedCards[i].Setup(shopUnits[i], i);
                }
                else
                {
                    // 容错：隐藏多余的卡牌
                    _spawnedCards[i].gameObject.SetActive(false); 
                }
            }
        }

        public void OnRerollButtonClicked() => ShopManager.Instance.RequestReroll();
        public void ToggleShopPanel() => shopPanel?.SetActive(!shopPanel.activeSelf);
    }
}