using UnityEngine;

namespace AutoChess.Managers
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("己方棋盘配置 (BoardAttach)")]
        public Transform boardRoot;
        
        [Header("敌方棋盘配置 (BoardAttachEne)")]
        public Transform boardRootEne;

        // 4行7列的二维数组
        public Transform[,] BoardAnchors { get; private set; }
        public GameObject[,] BoardUnits { get; private set; }

        public Transform[,] BoardAnchorsEne { get; private set; }
        public GameObject[,] BoardUnitsEne { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BoardAnchors = new Transform[4, 7];
            BoardUnits = new GameObject[4, 7];
            BoardAnchorsEne = new Transform[4, 7];
            BoardUnitsEne = new GameObject[4, 7];

            InitBoard(boardRoot, BoardAnchors);
            InitBoard(boardRootEne, BoardAnchorsEne);
        }

        private void InitBoard(Transform root, Transform[,] anchors)
        {
            if (root != null)
            {
                for (int r = 0; r < 4; r++)
                {
                    if (r < root.childCount)
                    {
                        Transform row = root.GetChild(r);
                        for (int c = 0; c < 7; c++)
                        {
                            if (c < row.childCount)
                            {
                                anchors[r, c] = row.GetChild(c);
                            }
                        }
                    }
                }
            }
        }
    }
}