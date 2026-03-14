using System.Collections.Generic;
using UnityEngine;
using AutoChess.Core;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class MergeManager : MonoBehaviour
    {
        public static MergeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // 核心检查与合成逻辑
        public void CheckForMerge(CardDataSO cardData, int currentStar)
        {
            if (currentStar >= 3) return; // 3星已经是最高级了，停止合成

            List<ChessUnit> matches = new List<ChessUnit>();

            // 1. 扫描备战区
            var benchUnits = BenchManager.Instance.BenchedUnits;
            for (int i = 0; i < benchUnits.Length; i++)
            {
                if (benchUnits[i] != null)
                {
                    var cu = benchUnits[i].GetComponent<ChessUnit>();
                    if (cu != null && cu.Data == cardData && cu.StarLevel == currentStar)
                        matches.Add(cu);
                }
            }

            // 2. 扫描战斗棋盘
            var boardUnits = BoardManager.Instance.BoardUnits;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (boardUnits[r, c] != null)
                    {
                        var cu = boardUnits[r, c].GetComponent<ChessUnit>();
                        if (cu != null && cu.Data == cardData && cu.StarLevel == currentStar)
                            matches.Add(cu);
                    }
                }
            }

            // 3. 如果场上同星级的该棋子 >= 3 个，触发合成！
            if (matches.Count >= 3)
            {
                // 排序：优先保留站在战斗棋盘上的棋子作为“大哥”
                matches.Sort((a, b) => b.IsOnBoard.CompareTo(a.IsOnBoard));

                ChessUnit primary = matches[0]; // 升星的大哥
                ChessUnit sac1 = matches[1];    // 祭品1
                ChessUnit sac2 = matches[2];    // 祭品2

                // 从数据网格中清空两个祭品的位置
                ClearSlot(sac1);
                ClearSlot(sac2);

                // 物理销毁两个祭品
                Destroy(sac1.gameObject);
                Destroy(sac2.gameObject);

                // 大哥升星！
                primary.StarLevel++;
                
                // 视觉强化 1：模型体积膨胀 1.2 倍
                primary.transform.localScale *= 1.2f; 
                
                // 视觉强化 2：播放发光描边
                var view = primary.GetComponent<View.UnitView>();
                if (view != null) view.SetStarVisuals(primary.StarLevel);

                Debug.Log($"<color=yellow>✨合成！{cardData.unitName} 升到了 {primary.StarLevel} 星！</color>");

                // 递归检查：万一这次合成正好凑齐了 3 个两星，就会引发“连环爆燃”直接合出三星！
                CheckForMerge(cardData, primary.StarLevel);
            }
        }

        private void ClearSlot(ChessUnit unit)
        {
            if (unit.IsOnBoard) BoardManager.Instance.BoardUnits[unit.BoardRow, unit.BoardCol] = null;
            else BenchManager.Instance.BenchedUnits[unit.CurrentBenchSlot] = null;
        }
    }
}