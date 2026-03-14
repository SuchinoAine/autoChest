using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AutoChess.Configs; // 确保引用了 BondDataSO 所在的命名空间

namespace AutoChess.View
{
    public class BondUIItem : MonoBehaviour
    {
        [Header("UI 引用")]
        public Image iconImage;           // 拖入显示图标的 Image
        public TextMeshProUGUI nameText;  // 拖入显示名字的 Text
        public TextMeshProUGUI countText; // 拖入显示数量的 Text

        // ✅ 这个方法由 UIManager 调用，注入数据
        public void Setup(BondDataSO bondData, int count)
        {
            if (bondData == null) return;

            // 从 BondDataSO 读取图片和名字
            if (iconImage != null && bondData.bondIcon != null) 
                iconImage.sprite = bondData.bondIcon; 

            if (nameText != null) 
                nameText.text = bondData.bondName;

            // 设置数量
            if (countText != null) 
                countText.text = count.ToString();

            // 视觉反馈：数量 >= 2 算作激活（金色），否则灰色
            if (count >= 2)
                countText.color = new Color(1f, 0.8f, 0.2f); 
            else
                countText.color = Color.gray;
        }
    }
}