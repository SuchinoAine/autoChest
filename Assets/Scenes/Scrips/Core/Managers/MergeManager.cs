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

        public void CheckForMerge(CardDataSO cardData, int currentStar)
        {
            if (currentStar >= 3) return;

            List<ChessUnit> matches = new List<ChessUnit>();

            // 1. 扫描备战区 (现在直接拿到的是 ChessUnit)
            var benchUnits = BenchManager.Instance.BenchedUnits;
            for (int i = 0; i < benchUnits.Length; i++)
            {
                var cu = benchUnits[i]; // ✅ 不再需要 GetComponent！
                if (cu != null && cu.Data == cardData && cu.StarLevel == currentStar)
                    matches.Add(cu);
            }

            // 2. 扫描战斗棋盘
            var boardUnits = BoardManager.Instance.BoardUnits;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    var cu = boardUnits[r, c]; // ✅ 不再需要 GetComponent！
                    if (cu != null && cu.Data == cardData && cu.StarLevel == currentStar)
                        matches.Add(cu);
                }
            }

            if (matches.Count >= 3)
            {
                matches.Sort((a, b) => b.IsOnBoard.CompareTo(a.IsOnBoard));

                ChessUnit primary = matches[0]; 
                ChessUnit sac1 = matches[1];    
                ChessUnit sac2 = matches[2];    

                ClearSlot(sac1);
                ClearSlot(sac2);

                // ✅ 补齐对象池闭环：不再直接 Destroy，而是回收到对象池
                PoolManager.Instance.ReleaseUnit(cardData, sac1.gameObject);
                PoolManager.Instance.ReleaseUnit(cardData, sac2.gameObject);

                primary.StarLevel++;
                primary.transform.localScale *= 1.2f; 
                primary.UpdateStarVisuals();

                Debug.Log($"<color=yellow>✨合成！{cardData.unitName} 升到了 {primary.StarLevel} 星！</color>");

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