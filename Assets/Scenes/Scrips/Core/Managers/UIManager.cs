using UnityEngine;
using System.Collections.Generic;
using TMPro;
using AutoChess.Configs;
using AutoChess.View;

namespace AutoChess.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI 面板文字")]
        public GameObject shopPanel;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI levelText;

        [Header("动态商店卡池")]
        public Transform[] cardContainers = new Transform[5];
        public GameObject cardPrefab;

        // ✅ 新增：左侧羁绊面板配置
        [Header("羁绊 UI (左侧面板)")]
        public Transform bondsContainer;  // UI容器 (挂了 Vertical Layout Group)
        public GameObject bondItemPrefab; // 刚才写的单条羁绊 UI 预制体

        private ShopCardUI[] _spawnedCards = new ShopCardUI[5];

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
            GameEventBus.OnLevelExpChanged += UpdateLevelUI;
            // ✅ 订阅羁绊刷新事件
            GameEventBus.OnSynergyChanged += UpdateSynergyUI;
        }

        private void OnDisable()
        {
            GameEventBus.OnEnterPreparationPhase -= ShowShop;
            GameEventBus.OnEnterCombatPhase -= HideShop;
            GameEventBus.OnCoinChanged -= UpdateCoinUI;
            GameEventBus.OnShopRefreshed -= UpdateShopUI;
            GameEventBus.OnLevelExpChanged -= UpdateLevelUI;
            // ✅ 取消订阅
            GameEventBus.OnSynergyChanged -= UpdateSynergyUI;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Preparation)
            {
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(GameManager.Instance.PhaseTimer).ToString();

                if (Input.GetKeyDown(KeyCode.D)) OnRerollButtonClicked();
                if (Input.GetKeyDown(KeyCode.F)) OnBuyExpButtonClicked();
            }
        }

        private void ShowShop() => shopPanel.SetActive(true);
        private void HideShop() => shopPanel.SetActive(false);

        private void UpdateCoinUI(int coins)
        {
            if (coinText != null) coinText.text = $"{coins}";
        }

        private void UpdateLevelUI(int level, int exp, int nextExp)
        {
            if (levelText != null)
            {
                if (level >= ShopManager.Instance.MaxLevel)
                    levelText.text = $"Lv {level} (MAX)";
                else
                    levelText.text = $"Lv {level}  [{exp}/{nextExp}]";
            }
        }

        private void UpdateShopUI(List<CardDataSO> shopUnits)
        {
            for (int i = 0; i < cardContainers.Length; i++)
            {
                if (i >= shopUnits.Count) break;

                if (_spawnedCards[i] == null)
                {
                    GameObject newCardObj = Instantiate(cardPrefab, cardContainers[i]);
                    newCardObj.transform.localPosition = Vector3.zero;
                    newCardObj.transform.localScale = Vector3.one;
                    _spawnedCards[i] = newCardObj.GetComponent<ShopCardUI>();
                }

                CardDataSO cardData = shopUnits[i];
                if (cardData != null)
                {
                    _spawnedCards[i].gameObject.SetActive(true);
                    _spawnedCards[i].Setup(cardData, i);
                }
                else
                {
                    _spawnedCards[i].gameObject.SetActive(false);
                }
            }
        }

        // ✅ 新增：渲染左侧羁绊面板的核心逻辑
        private void UpdateSynergyUI(Dictionary<BondDataSO, int> activeBonds)
        {
            if (bondsContainer == null || bondItemPrefab == null) return;


            // 1. 清理旧的羁绊条目
            foreach (Transform child in bondsContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. 根据字典数据生成新的条目
            foreach (var kvp in activeBonds)
            {
                // kvp.Key 是 BondDataSO
                // kvp.Value 是该羁绊在场上的不重复英雄数量
                GameObject go = Instantiate(bondItemPrefab, bondsContainer);
                BondUIItem uiItem = go.GetComponent<BondUIItem>();

                if (uiItem != null)
                {
                    // ✅ 这里把整个 SO 对象和统计数量传过去
                    uiItem.Setup(kvp.Key, kvp.Value);
                }
            }
        }
        public void OnRerollButtonClicked() => ShopManager.Instance.RequestReroll();
        public void OnBuyExpButtonClicked() => ShopManager.Instance.RequestBuyExp();
        public void ToggleShopPanel() => shopPanel?.SetActive(!shopPanel.activeSelf);
    }
}