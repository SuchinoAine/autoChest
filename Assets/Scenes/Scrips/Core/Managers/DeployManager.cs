using UnityEngine;
using UnityEngine.EventSystems;
using AutoChess.Core;

namespace AutoChess.Managers
{
    public class DeployManager : MonoBehaviour
    {
        public static DeployManager Instance { get; private set; }

        private Camera _mainCam;
        private ChessUnit _draggingUnit;
        private Plane _dragPlane;

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ChessUnit unit = hit.collider.GetComponent<ChessUnit>();
                if (unit != null)
                {
                    _draggingUnit = unit;
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
                _draggingUnit.transform.position = Vector3.Lerp(_draggingUnit.transform.position, targetPivotPos, Time.deltaTime * 20f);
            }
        }

        private void Drop()
        {
            if (Input.mousePosition.y < Screen.height * 0.05f)
            {
                int starMultiplier = (int)Mathf.Pow(3, _draggingUnit.StarLevel - 1);
                int sellPrice = _draggingUnit.Data.cost * starMultiplier; 
                
                EconomyManager.Instance.AddGold(sellPrice);
                
                for (int i = 0; i < starMultiplier; i++)
                {
                    ShopManager.Instance.SellCardToPool(_draggingUnit.Data);
                }
                
                Debug.Log($"<color=orange>💰 出售了 [{_draggingUnit.StarLevel}星 {_draggingUnit.Data.unitName}]，获得了 {sellPrice} 金币，回收了 {starMultiplier} 张卡牌。</color>");
                
                // ✅ 补齐对象池闭环：放回对象池
                PoolManager.Instance.ReleaseUnit(_draggingUnit.Data, _draggingUnit.gameObject);
                _draggingUnit = null;

                if (SynergyManager.Instance != null) SynergyManager.Instance.BroadcastSynergiesToUI();
                return; 
            }

            Transform[] benchAnchors = BenchManager.Instance.BenchAnchors;
            ChessUnit[] benchedUnits = BenchManager.Instance.BenchedUnits; // ✅ 使用 ChessUnit[]
            Transform[,] boardAnchors = BoardManager.Instance.BoardAnchors;
            ChessUnit[,] boardUnits = BoardManager.Instance.BoardUnits;    // ✅ 使用 ChessUnit[,]

            float minDistance = float.MaxValue;
            float snapRadius = 2.0f;
            Vector3 visualCenter = _draggingUnit.transform.position - _draggingUnit.BaseOffset;

            bool targetIsBoard = false;
            int bestBenchIdx = -1;
            int bestBoardRow = -1, bestBoardCol = -1;
            Transform targetAnchor = null;

            for (int i = 0; i < benchAnchors.Length; i++)
            {
                float dist = Vector3.Distance(visualCenter, benchAnchors[i].position);
                if (dist < minDistance && dist < snapRadius)
                {
                    minDistance = dist; targetIsBoard = false; bestBenchIdx = i; targetAnchor = benchAnchors[i];
                }
            }

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (boardAnchors[r, c] == null) continue;
                    float dist = Vector3.Distance(visualCenter, boardAnchors[r, c].position);
                    if (dist < minDistance && dist < snapRadius)
                    {
                        minDistance = dist; targetIsBoard = true; bestBoardRow = r; bestBoardCol = c; targetAnchor = boardAnchors[r, c];
                    }
                }
            }

            if (targetAnchor != null)
            {
                // ✅ 现在获取到的是直接的 ChessUnit，不用再 GetComponent
                ChessUnit targetOccupant = targetIsBoard ? boardUnits[bestBoardRow, bestBoardCol] : benchedUnits[bestBenchIdx];

                if (targetIsBoard && !_originalIsOnBoard && targetOccupant == null)
                {
                    int currentPopulation = 0;
                    foreach (var u in boardUnits) if (u != null) currentPopulation++;

                    if (currentPopulation >= ShopManager.Instance.PlayerLevel)
                    {
                        Debug.LogWarning($"<color=red>❌ 人口已满！当前等级 {ShopManager.Instance.PlayerLevel}，最多只能上阵 {ShopManager.Instance.PlayerLevel} 个棋子。</color>");
                        targetAnchor = null; 
                    }
                }

                if (targetAnchor != null)
                {
                    if (targetOccupant != null && targetOccupant.gameObject != _draggingUnit.gameObject)
                    {
                        ChessUnit otherUnit = targetOccupant;

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
                        boardUnits[bestBoardRow, bestBoardCol] = _draggingUnit;
                        _draggingUnit.IsOnBoard = true;
                        _draggingUnit.BoardRow = bestBoardRow;
                        _draggingUnit.BoardCol = bestBoardCol;

                        var runner = FindFirstObjectByType<SandboxRunner>();
                        if (runner != null) runner.AttachHud(_draggingUnit.gameObject, null);
                    }
                    else
                    {
                        benchedUnits[bestBenchIdx] = _draggingUnit;
                        _draggingUnit.IsOnBoard = false;
                        _draggingUnit.CurrentBenchSlot = bestBenchIdx;
                    }
                    _draggingUnit.transform.SetParent(targetAnchor);
                }
            }
            
            if (targetAnchor == null)
            {
                if (_originalIsOnBoard)
                {
                    boardUnits[_originalBoardRow, _originalBoardCol] = _draggingUnit;
                    _draggingUnit.transform.SetParent(boardAnchors[_originalBoardRow, _originalBoardCol]);
                }
                else
                {
                    benchedUnits[_originalBenchSlot] = _draggingUnit;
                    _draggingUnit.transform.SetParent(benchAnchors[_originalBenchSlot]);
                }
            }

            _draggingUnit.transform.localPosition = _draggingUnit.BaseOffset;
            _draggingUnit = null;

            if (SynergyManager.Instance != null) SynergyManager.Instance.BroadcastSynergiesToUI();
        }
    }
}