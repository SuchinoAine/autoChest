using UnityEngine;
using System.Collections.Generic;
using TMPro; // 引入 TextMeshPro
using AutoChess.Configs;
using AutoChess.View;

namespace AutoChess.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI 面板文字 (全部使用 TextMeshPro)")]
        public GameObject shopPanel;
        public TextMeshProUGUI coinText; 
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI levelText; // 显示等级和经验
        
        [Header("动态商店卡池 (严格对应5个槽位)")]
        [Tooltip("请按顺序将 Hierarchy 中的 container1 到 container5 拖入此数组")]
        public Transform[] cardContainers = new Transform[5]; 
        public GameObject cardPrefab;   
        
        // 缓存生成的卡牌 UI (固定5个)
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
        }

        private void OnDisable()
        {
            GameEventBus.OnEnterPreparationPhase -= ShowShop;
            GameEventBus.OnEnterCombatPhase -= HideShop;
            GameEventBus.OnCoinChanged -= UpdateCoinUI;
            GameEventBus.OnShopRefreshed -= UpdateShopUI;
            GameEventBus.OnLevelExpChanged -= UpdateLevelUI;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Preparation)
            {
                if (timerText != null) 
                    timerText.text = Mathf.CeilToInt(GameManager.Instance.PhaseTimer).ToString();
                    
                // 快捷键支持
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
                {
                    levelText.text = $"Lv {level} (MAX)";
                }
                else
                {
                    levelText.text = $"Lv {level}  [{exp}/{nextExp}]";
                }
            }
        }

        // 精准对位 5 个 Container
        private void UpdateShopUI(List<CardDataSO> shopUnits)
        {
            for (int i = 0; i < cardContainers.Length; i++)
            {
                if (i >= shopUnits.Count) break; // 防越界保护

                // 1. 如果这个 Container 里还没生成过 Card 预制体，就生成一个
                if (_spawnedCards[i] == null)
                {
                    GameObject newCardObj = Instantiate(cardPrefab, cardContainers[i]);
                    // 确保生成的卡牌在 Container 中心且不变形
                    newCardObj.transform.localPosition = Vector3.zero; 
                    newCardObj.transform.localScale = Vector3.one; 
                    _spawnedCards[i] = newCardObj.GetComponent<ShopCardUI>();
                }

                // 2. 根据数据刷新显示
                CardDataSO cardData = shopUnits[i];
                if (cardData != null)
                {
                    // 有卡牌数据：激活卡牌节点，并注入数据
                    _spawnedCards[i].gameObject.SetActive(true);
                    _spawnedCards[i].Setup(cardData, i);
                }
                else
                {
                    // 数据为 null（被买走或槽位轮空）：将卡牌节点隐藏
                    _spawnedCards[i].gameObject.SetActive(false); 
                }
            }
        }

        // 绑定给 UI 的按钮事件
        public void OnRerollButtonClicked() => ShopManager.Instance.RequestReroll();
        public void OnBuyExpButtonClicked() => ShopManager.Instance.RequestBuyExp();
        public void ToggleShopPanel() => shopPanel?.SetActive(!shopPanel.activeSelf);
    }
}