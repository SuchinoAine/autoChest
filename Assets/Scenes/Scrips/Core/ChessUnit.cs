using UnityEngine;
using AutoChess.Configs;
using System.Collections.Generic;

namespace AutoChess.Core
{
    // 挂载在每个实例化的 3D 棋子模型上，作为它的身份标识
    public class ChessUnit : MonoBehaviour
    {
        public CardDataSO Data;
        public Vector3 BaseOffset;

        // --- 位置状态 ---
        public bool IsOnBoard = false;
        public int CurrentBenchSlot = -1;
        public int BoardRow = -1;
        public int BoardCol = -1;

        public int StarLevel = 1;

        // ✅ 核心魔法：向 Unity 面板暴露 HDR 颜色拾取器！
        [Header("星级发光颜色 (HDR)")]
        [ColorUsage(showAlpha: true, hdr: false)]
        public Color color2Star = new Color(0f, 0.4f, 1f, 1f) * 1f; // 默认深蓝色

        [ColorUsage(showAlpha: true, hdr: false)]
        public Color color3Star = new Color(1f, 0.5f, 0f, 1f) * 1f; // 默认暗金色

        private List<Outline> _outlines = new List<Outline>();

        public void UpdateStarVisuals()
        {
            // 1. 先把已有的描边全部关闭
            foreach (var o in _outlines) if (o != null) o.enabled = false;

            if (StarLevel <= 1) return;

            var renderers = GetComponentsInChildren<Renderer>(true);

            // 🔥 终极稳妥方案：绝对不要乘以任何 intensity！给出最纯正的 1.0 满值颜色！
            // 高亮发光青色
            Color brightCyan = new Color(0.0f, 1.0f, 1.0f) * 3.0f;
            // 高亮发光金色
            Color brightGold = new Color(1.0f, 0.87f, 0.2f) * 3.0f;

            Color targetColor = StarLevel == 2 ? color2Star : color3Star;

            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;

                Outline ol = r.GetComponent<Outline>();
                if (ol == null) ol = r.gameObject.AddComponent<Outline>();

                ol.OutlineMode = Outline.Mode.OutlineVisible;
                ol.OutlineColor = targetColor;
                ol.OutlineWidth = 4.0f;
                ol.enabled = true;

                if (!_outlines.Contains(ol)) _outlines.Add(ol);
            }
        }
    }
}