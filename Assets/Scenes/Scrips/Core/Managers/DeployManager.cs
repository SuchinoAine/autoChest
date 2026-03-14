using UnityEngine;
using UnityEngine.EventSystems; // 引入事件系统以拦截 UI 点击
using AutoChess.Core;

namespace AutoChess.Managers
{
    public class DeployManager : MonoBehaviour
    {
        public static DeployManager Instance { get; private set; }

        private Camera _mainCam;
        private ChessUnit _draggingUnit;
        private Plane _dragPlane;

        // 记录抓起前的原位置，方便退回或换位
        private bool _originalIsOnBoard;
        private int _originalBenchSlot;
        private int _originalBoardRow, _originalBoardCol;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _mainCam = Camera.main;
            _dragPlane = new Plane(Vector3.up, new Vector3(0, 1.0f, 0));
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Preparation) return;

            if (Input.GetMouseButtonDown(0)) TryPickUp();
            else if (Input.GetMouseButton(0) && _draggingUnit != null) Drag();
            else if (Input.GetMouseButtonUp(0) && _draggingUnit != null) Drop();
        }

        private void TryPickUp()
        {
            // 如果鼠标当前悬停在 UI 元素上（比如按钮、卡牌），直接终止 防止点穿
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ChessUnit unit = hit.collider.GetComponent<ChessUnit>();
                if (unit != null)
                {
                    _draggingUnit = unit;

                    // 1. 记录它原本的槽位信息
                    _originalIsOnBoard = unit.IsOnBoard;
                    if (_originalIsOnBoard)
                    {
                        _originalBoardRow = unit.BoardRow;
                        _originalBoardCol = unit.BoardCol;
                        // 2. 抓起时，暂时将它的原槽位设为空
                        BoardManager.Instance.BoardUnits[_originalBoardRow, _originalBoardCol] = null;
                    }
                    else
                    {
                        _originalBenchSlot = unit.CurrentBenchSlot;
                        BenchManager.Instance.BenchedUnits[_originalBenchSlot] = null;
                    }
                }
            }
        }

        private void Drag()
        {
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.GetPoint(enter);
                Vector3 targetPivotPos = targetPos + _draggingUnit.BaseOffset;

                _draggingUnit.transform.position = Vector3.Lerp(
                    _draggingUnit.transform.position, targetPivotPos, Time.deltaTime * 20f);
            }
        }

        private void Drop()
        {
            // 获取两边的数据池
            Transform[] benchAnchors = BenchManager.Instance.BenchAnchors;
            GameObject[] benchedUnits = BenchManager.Instance.BenchedUnits;
            Transform[,] boardAnchors = BoardManager.Instance.BoardAnchors;
            GameObject[,] boardUnits = BoardManager.Instance.BoardUnits;

            float minDistance = float.MaxValue;
            float snapRadius = 2.0f;
            Vector3 visualCenter = _draggingUnit.transform.position - _draggingUnit.BaseOffset;

            // 目标记录
            bool targetIsBoard = false;
            int bestBenchIdx = -1;
            int bestBoardRow = -1, bestBoardCol = -1;
            Transform targetAnchor = null;

            // ✅ 1. 扫描备战区 (Bench) 寻找最近格子
            for (int i = 0; i < benchAnchors.Length; i++)
            {
                float dist = Vector3.Distance(visualCenter, benchAnchors[i].position);
                if (dist < minDistance && dist < snapRadius)
                {
                    minDistance = dist;
                    targetIsBoard = false; bestBenchIdx = i;
                    targetAnchor = benchAnchors[i];
                }
            }

            // ✅ 2. 扫描战斗棋盘 (Board) 寻找最近格子
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (boardAnchors[r, c] == null) continue;
                    float dist = Vector3.Distance(visualCenter, boardAnchors[r, c].position);
                    if (dist < minDistance && dist < snapRadius)
                    {
                        minDistance = dist;
                        targetIsBoard = true; bestBoardRow = r; bestBoardCol = c;
                        targetAnchor = boardAnchors[r, c];
                    }
                }
            }

            // ✅ 3. 处理放置或换位逻辑
            if (targetAnchor != null)
            {
                // 找到目标格子上的占位者
                GameObject targetOccupant = targetIsBoard ? boardUnits[bestBoardRow, bestBoardCol] : benchedUnits[bestBenchIdx];

                // 如果格子里有人，执行【互换】
                if (targetOccupant != null && targetOccupant != _draggingUnit.gameObject)
                {
                    ChessUnit otherUnit = targetOccupant.GetComponent<ChessUnit>();

                    if (_originalIsOnBoard)
                    {
                        boardUnits[_originalBoardRow, _originalBoardCol] = targetOccupant;
                        otherUnit.IsOnBoard = true;
                        otherUnit.BoardRow = _originalBoardRow;
                        otherUnit.BoardCol = _originalBoardCol;
                        targetOccupant.transform.SetParent(boardAnchors[_originalBoardRow, _originalBoardCol]);
                    }
                    else
                    {
                        benchedUnits[_originalBenchSlot] = targetOccupant;
                        otherUnit.IsOnBoard = false;
                        otherUnit.CurrentBenchSlot = _originalBenchSlot;
                        targetOccupant.transform.SetParent(benchAnchors[_originalBenchSlot]);
                    }
                    // 对方棋子回位
                    targetOccupant.transform.localPosition = otherUnit.BaseOffset;
                }

                // 将被拖拽的棋子放入目标格子
                if (targetIsBoard)
                {
                    boardUnits[bestBoardRow, bestBoardCol] = _draggingUnit.gameObject;
                    _draggingUnit.IsOnBoard = true;
                    _draggingUnit.BoardRow = bestBoardRow;
                    _draggingUnit.BoardCol = bestBoardCol;

                    // ✅ 新增：如果是上场，立即挂载或初始化血条
                    var runner = FindFirstObjectByType<SandboxRunner>(); // 或者使用你的单例
                    if (runner != null)
                    {
                        runner.AttachHud(_draggingUnit.gameObject, null);
                    }
                }
                else
                {
                    benchedUnits[bestBenchIdx] = _draggingUnit.gameObject;
                    _draggingUnit.IsOnBoard = false;
                    _draggingUnit.CurrentBenchSlot = bestBenchIdx;
                }
                _draggingUnit.transform.SetParent(targetAnchor);
            }
            else
            {
                // ✅ 4. 如果没有对准任何格子（扔在野外），退回原位
                if (_originalIsOnBoard)
                {
                    boardUnits[_originalBoardRow, _originalBoardCol] = _draggingUnit.gameObject;
                    _draggingUnit.transform.SetParent(boardAnchors[_originalBoardRow, _originalBoardCol]);
                }
                else
                {
                    benchedUnits[_originalBenchSlot] = _draggingUnit.gameObject;
                    _draggingUnit.transform.SetParent(benchAnchors[_originalBenchSlot]);
                }
            }

            // 无论放哪，最后都重置局部偏移贴合地面
            _draggingUnit.transform.localPosition = _draggingUnit.BaseOffset;
            _draggingUnit = null;
        }
    }
}