using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ 引入 TextMeshPro 命名空间
using AutoChess.Configs;
using AutoChess.Managers;

namespace AutoChess.View
{
    public class ShopCardUI : MonoBehaviour
    {
        [Header("测试专用 (仅在独立测试时勾选)")]
        public bool testOnStart = false;
        public CardDataSO testUnitData;

        private Image _cardImage;
        private Image _border;
        // ✅ 修改为 TextMeshProUGUI
        private TextMeshProUGUI _nameText; 
        private TextMeshProUGUI _costText; 

        // 内部缓存类，用来存找好的羁绊组件
        private class BondUICache
        {
            public GameObject rootObj;
            public Image icon;
            public TextMeshProUGUI bondNameText; 

            public void Setup(BondData data)
            {
                rootObj.SetActive(true); // 确保节点开启
                
                // 填充图片并开启显示
                if (icon != null) 
                {
                    icon.sprite = data.bondIcon;
                    icon.enabled = true; 
                }
                
                // 填充文字
                if (bondNameText != null) 
                {
                    bondNameText.text = data.bondName;
                }
            }
            
            // 把多余的槽位置空，保留占位
            public void ClearEmpty()
            {
                rootObj.SetActive(true); 
                
                // 清空图片（置空 sprite 并关掉 Image 组件防止白块）
                if (icon != null) 
                {
                    icon.sprite = null;
                    icon.enabled = false; 
                }
                
                // 清空文字
                if (bondNameText != null) 
                {
                    bondNameText.text = "";
                }
            }
        }

        private BondUICache[] _bondUIs = new BondUICache[3];
        private Button _button;
        private int _slotIndex;

        private void Awake()
        {
            // === 1. 自动查找基础图层 ===
            _cardImage = transform.Find("image")?.GetComponent<Image>();
            _border = transform.Find("border")?.GetComponent<Image>();
            
            // === 2. 自动查找名称条 (nameBar) - 注意这里改用 GetComponent<TextMeshProUGUI>() ===
            _nameText = transform.Find("nameBar/name")?.GetComponent<TextMeshProUGUI>();
            _costText = transform.Find("nameBar/cost")?.GetComponent<TextMeshProUGUI>();

            // === 3. 自动查找羁绊 (bonds) ===
            for (int i = 0; i < 3; i++)
            {
                int bondIndex = i + 1; 
                Transform bondTrans = transform.Find($"bonds/bond{bondIndex}");
                
                _bondUIs[i] = new BondUICache();
                if (bondTrans != null)
                {
                    _bondUIs[i].rootObj = bondTrans.gameObject;
                    _bondUIs[i].icon = bondTrans.Find("icon")?.GetComponent<Image>();
                    // ✅ 注意这里改用 GetComponent<TextMeshProUGUI>()
                    _bondUIs[i].bondNameText = bondTrans.Find("bondName")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    Debug.LogWarning($"[ShopCardUI] 找不到羁绊节点: bonds/bond{bondIndex}，请检查名字拼写！");
                }
            }

            // === 4. 自动绑定按钮点击 ===
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnCardClicked);
            }
            else
            {
                Debug.LogWarning("[ShopCardUI] 找不到 Button 组件！请确保 Card 根节点挂了 Button！");
            }
        }

        private void Start()
        {
            // === 5. 临时测试入口 ===
            if (testOnStart && testUnitData != null)
            {
                Setup(testUnitData, 0); 
            }
        }

        /// <summary>
        /// 接收数据并渲染卡牌
        /// </summary>
        public void Setup(CardDataSO unitData, int slotIndex)
        {
            _slotIndex = slotIndex;

            if (unitData == null)
            {
                gameObject.SetActive(false); 
                return;
            }

            gameObject.SetActive(true);
            
            // 1. 填充名称和费用
            if (_nameText != null) _nameText.text = unitData.unitName;
            if (_costText != null) _costText.text = unitData.cost.ToString();
            
            // 2. 填充卡图和边框
            if (_cardImage != null && unitData.cardImage != null) _cardImage.sprite = unitData.cardImage;
            if (_border != null && unitData.borderImage != null) _border.sprite = unitData.borderImage;

            // 3. 填充羁绊
            for (int i = 0; i < _bondUIs.Length; i++)
            {
                // 容错：如果 Awake 里没找到这个节点，直接跳过
                if (_bondUIs[i].rootObj == null) continue;

                if (i < unitData.bonds.Count)
                {
                    // 有数据，正常填充
                    _bondUIs[i].Setup(unitData.bonds[i]);
                }
                else
                {
                    // 没数据了，对应的 bond 节点保留占位，但内容置空
                    _bondUIs[i].ClearEmpty();
                }
            }
        }

        private void OnCardClicked()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.RequestPurchase(_slotIndex);
            }
            else
            {
                Debug.Log($"[ShopCardUI 测试] 假装购买了槽位 {_slotIndex} 的卡牌！");
            }
        }
    }
}