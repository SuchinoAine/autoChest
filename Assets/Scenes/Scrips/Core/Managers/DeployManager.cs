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
            // 如果鼠标当前悬停在 UI 元素上，直接终止 防止点穿
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
            // 出售检测逻辑 (判断鼠标松开时，是否在屏幕底部的 5% 区域)
            if (Input.mousePosition.y < Screen.height * 0.05f)
            {
                int sellPrice = _draggingUnit.Data.cost * (int)Mathf.Pow(3, _draggingUnit.StarLevel - 1); 
                EconomyManager.Instance.AddGold(sellPrice);
                ShopManager.Instance.SellCardToPool(_draggingUnit.Data);

                Debug.Log($"<color=orange>💰 叮！出售了 [{_draggingUnit.StarLevel}星 {_draggingUnit.Data.unitName}]，获得了 {sellPrice} 金币！</color>");

                Destroy(_draggingUnit.gameObject);
                _draggingUnit = null;
                return; 
            }

            Transform[] benchAnchors = BenchManager.Instance.BenchAnchors;
            GameObject[] benchedUnits = BenchManager.Instance.BenchedUnits;
            Transform[,] boardAnchors = BoardManager.Instance.BoardAnchors;
            GameObject[,] boardUnits = BoardManager.Instance.BoardUnits;

            float minDistance = float.MaxValue;
            float snapRadius = 2.0f;
            Vector3 visualCenter = _draggingUnit.transform.position - _draggingUnit.BaseOffset;

            bool targetIsBoard = false;
            int bestBenchIdx = -1;
            int bestBoardRow = -1, bestBoardCol = -1;
            Transform targetAnchor = null;

            // 1. 扫描备战区
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

            // 2. 扫描战斗棋盘
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
                GameObject targetOccupant = targetIsBoard ? boardUnits[bestBoardRow, bestBoardCol] : benchedUnits[bestBenchIdx];

                // 🔥 新增：人口上限拦截！
                // 条件：将【备战区】的棋子拖到【战斗棋盘】的【空位】上
                if (targetIsBoard && !_originalIsOnBoard && targetOccupant == null)
                {
                    // 统计当前棋盘上的己方总人数
                    int currentPopulation = 0;
                    foreach (var u in boardUnits) if (u != null) currentPopulation++;

                    // 判定是否超标
                    if (currentPopulation >= ShopManager.Instance.PlayerLevel)
                    {
                        Debug.LogWarning($"<color=red>❌ 人口已满！当前等级 {ShopManager.Instance.PlayerLevel}，最多只能上阵 {ShopManager.Instance.PlayerLevel} 个棋子。</color>");
                        // 巧妙之处：强行把目标格子设为 null，让它走到下一步的“退回原位”逻辑里！
                        targetAnchor = null; 
                    }
                }

                // 如果人口没满，或者是在进行互换，正常执行放入逻辑
                if (targetAnchor != null)
                {
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
                        targetOccupant.transform.localPosition = otherUnit.BaseOffset;
                    }

                    if (targetIsBoard)
                    {
                        boardUnits[bestBoardRow, bestBoardCol] = _draggingUnit.gameObject;
                        _draggingUnit.IsOnBoard = true;
                        _draggingUnit.BoardRow = bestBoardRow;
                        _draggingUnit.BoardCol = bestBoardCol;

                        var runner = FindFirstObjectByType<SandboxRunner>();
                        if (runner != null) runner.AttachHud(_draggingUnit.gameObject, null);
                    }
                    else
                    {
                        benchedUnits[bestBenchIdx] = _draggingUnit.gameObject;
                        _draggingUnit.IsOnBoard = false;
                        _draggingUnit.CurrentBenchSlot = bestBenchIdx;
                    }
                    _draggingUnit.transform.SetParent(targetAnchor);
                }
            }
            
            // 4. 野外退回 / 被人口限制弹回的逻辑统一处理
            if (targetAnchor == null)
            {
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