using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ 引入 TextMeshPro 命名空间
using AutoChess.Configs;
using AutoChess.Managers;

namespace AutoChess.View
{
    public class ShopCardUI : MonoBehaviour
    {
        private Image _cardImage;
        private Image _border;
        private TextMeshProUGUI _nameText; // ✅ 改为 TMP
        private TextMeshProUGUI _costText; // ✅ 改为 TMP

        // 内部缓存类，用来存找好的羁绊组件
        private class BondUICache
        {
            public GameObject rootObj;
            public Image icon;
            public TextMeshProUGUI bondNameText; // ✅ 改为 TMP

            public void Setup(BondDataSO data) // ✅ 接收最新的 BondDataSO
            {
                rootObj.SetActive(true);
                if (data != null)
                {
                    if (icon != null) { icon.sprite = data.bondIcon; icon.enabled = true; }
                    if (bondNameText != null) bondNameText.text = data.bondName;
                }
                else
                {
                    ClearEmpty();
                }
            }
            
            public void ClearEmpty()
            {
                rootObj.SetActive(true);
                if (icon != null) { icon.sprite = null; icon.enabled = false; }
                if (bondNameText != null) bondNameText.text = "";
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
            
            // === 2. 自动查找名称条 (nameBar) ===
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
                    _bondUIs[i].bondNameText = bondTrans.Find("bondName")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // === 4. 自动绑定按钮点击 ===
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnCardClicked);
            }
        }

        /// <summary>
        /// 接收 UIManager 传来的真实卡牌数据并渲染
        /// </summary>
        public void Setup(CardDataSO cardData, int slotIndex) // ✅ 接收最新的 CardDataSO
        {
            _slotIndex = slotIndex;

            if (cardData == null)
            {
                // 数据为空（被买走或槽位轮空）：隐藏卡牌内容
                gameObject.SetActive(false); 
                return;
            }

            gameObject.SetActive(true);
            
            // 1. 填充名称和费用
            if (_nameText != null) _nameText.text = cardData.unitName;
            if (_costText != null) _costText.text = cardData.cost.ToString();
            
            // 2. 填充卡图和边框
            if (_cardImage != null && cardData.cardImage != null) _cardImage.sprite = cardData.cardImage;
            if (_border != null && cardData.borderImage != null) _border.sprite = cardData.borderImage;

            // 3. 填充羁绊
            for (int i = 0; i < _bondUIs.Length; i++)
            {
                if (_bondUIs[i].rootObj == null) continue;

                if (i < cardData.bonds.Count && cardData.bonds[i] != null)
                {
                    _bondUIs[i].Setup(cardData.bonds[i]);
                }
                else
                {
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
        }
    }
}