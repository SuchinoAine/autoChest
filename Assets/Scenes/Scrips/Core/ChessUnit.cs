using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Core
{
    // 挂载在每个实例化的 3D 棋子模型上，作为它的身份标识
    public class ChessUnit : MonoBehaviour
    {
        public CardDataSO Data;          
        public Vector3 BaseOffset;       
        
        // --- 位置状态 ---
        public bool IsOnBoard = false;   // 是否在棋盘上
        public int CurrentBenchSlot = -1;// 备战区槽位
        public int BoardRow = -1;        // 棋盘行数 (0-3)
        public int BoardCol = -1;        // 棋盘列数 (0-6)
    }
}