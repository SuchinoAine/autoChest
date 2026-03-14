using UnityEngine;
using System.Collections.Generic;
using AutoChess.Configs;
using AutoChess.View;
using TMPro;

namespace AutoChess.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI 面板")] 
        public GameObject shopPanel;
        public GameObject dButton; // D牌按钮
        public TextMeshProUGUI coinText; // 金币
        public TextMeshProUGUI timerText;
        
        [Header("动态商店卡池 (严格对应5个槽位)")]
        public Transform[] cardContainers = new Transform[5]; 
        public GameObject cardPrefab;   
        
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
                    
                if (Input.GetKeyDown(KeyCode.D)) OnRerollButtonClicked();
            }
        }

        private void ShowShop() => shopPanel.SetActive(true);
        private void HideShop() => shopPanel.SetActive(false);
        private void UpdateCoinUI(int coins)
        {
            if (coinText != null) coinText.text = $"{coins}";
        }

        // ✅ 核心逻辑：精准对位，有数据就显示，没数据就置空隐藏
        private void UpdateShopUI(List<CardDataSO> shopUnits)
        {
            for (int i = 0; i < cardContainers.Length; i++)
            {
                if (i >= shopUnits.Count) break;

                // 1. 如果槽位里还没实例化卡牌，就实例化一个
                if (_spawnedCards[i] == null)
                {
                    GameObject newCardObj = Instantiate(cardPrefab, cardContainers[i]);
                    // 非常重要：重置位置和缩放，防止UI错乱
                    newCardObj.transform.localPosition = Vector3.zero; 
                    newCardObj.transform.localScale = Vector3.one; 
                    
                    _spawnedCards[i] = newCardObj.GetComponent<ShopCardUI>();
                }

                // 2. 根据数据刷新显示
                CardDataSO cardData = shopUnits[i];
                if (cardData != null)
                {
                    _spawnedCards[i].gameObject.SetActive(true);
                    _spawnedCards[i].Setup(cardData, i);
                }
                else
                {
                    // 数据为 null（被买走）：隐藏卡牌
                    _spawnedCards[i].gameObject.SetActive(false); 
                }
            }
        }

        public void OnRerollButtonClicked() => ShopManager.Instance.RequestReroll();
        public void ToggleShopPanel()
        {
            shopPanel?.SetActive(!shopPanel.activeSelf);
            dButton?.SetActive(shopPanel.activeSelf);
            
        }
    }
}