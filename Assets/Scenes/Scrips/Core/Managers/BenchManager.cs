using UnityEngine;
using AutoChess.Configs;
using AutoChess.Core; // 引入 ChessUnit 所在的命名空间

namespace AutoChess.Managers
{
    public class BenchManager : MonoBehaviour
    {
        public static BenchManager Instance { get; private set; }

        [Header("备战区节点配置")]
        [Tooltip("将层级图中的 Bench 父节点拖入这里，会自动读取它下面的 10 个 pozition")]
        public Transform benchRoot;
        
        private Transform[] _benchAnchors;
        private GameObject[] _benchedUnits;

        // 暴露给外部 DeployManager (布阵管理器) 读取和修改的属性
        public Transform[] BenchAnchors => _benchAnchors;
        public GameObject[] BenchedUnits => _benchedUnits;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            if (benchRoot != null)
            {
                _benchAnchors = new Transform[benchRoot.childCount];
                for (int i = 0; i < benchRoot.childCount; i++)
                {
                    _benchAnchors[i] = benchRoot.GetChild(i);
                }
            }
            else
            {
                Debug.LogError("[BenchManager] 请在 Inspector 中配置 Bench Root！");
                return;
            }

            _benchedUnits = new GameObject[_benchAnchors.Length];
        }

        private void OnEnable() => GameEventBus.OnUnitPurchased += OnUnitPurchased;
        private void OnDisable() => GameEventBus.OnUnitPurchased -= OnUnitPurchased;

        public bool IsBenchFull()
        {
            if (_benchedUnits == null) return true;
            for (int i = 0; i < _benchedUnits.Length; i++)
            {
                if (_benchedUnits[i] == null) return false; 
            }
            return true;
        }

        private void OnUnitPurchased(CardDataSO unitData)
        {
            if (unitData.prefab == null)
            {
                Debug.LogError($"[BenchManager] 卡牌 {unitData.unitName} 没有在 SO 里配置模型 Prefab！");
                return;
            }

            int emptyIndex = -1;
            for (int i = 0; i < _benchedUnits.Length; i++)
            {
                if (_benchedUnits[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex != -1)
            {
                Transform spawnPoint = _benchAnchors[emptyIndex];
                
                // 1. 生成并认 pozition 为父节点
                GameObject newUnit = Instantiate(unitData.prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                
                // 保持预制体原本的缩放和旋转
                newUnit.transform.localRotation = unitData.prefab.transform.localRotation;
                newUnit.transform.localScale = unitData.prefab.transform.localScale;
                
                // ✅ 2. 核心：X 和 Z 保持 0 (因为你已手动居中)，只计算并调整 Y 轴 (高度贴地)
                float offsetY = 0f;
                MeshFilter mf = newUnit.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // 获取网格最低点并反向抬高
                    offsetY = -mf.sharedMesh.bounds.min.y * newUnit.transform.localScale.y;
                }
                
                Vector3 finalOffset = new Vector3(0, offsetY, 0);
                newUnit.transform.localPosition = finalOffset;
                
                // 3. 加上物理碰撞体 (包住模型，方便鼠标点击拖拽)
                if (newUnit.GetComponent<Collider>() == null)
                {
                    var col = newUnit.AddComponent<BoxCollider>();
                    col.size = new Vector3(1.5f, 2f, 1.5f); 
                    col.center = new Vector3(0, 1f, 0);
                }

                // 4. 挂载身份证，供 DeployManager 拖拽和记录位置使用
                ChessUnit chessUnit = newUnit.GetComponent<ChessUnit>();
                if (chessUnit == null) chessUnit = newUnit.AddComponent<ChessUnit>();
                
                chessUnit.Data = unitData;
                chessUnit.BaseOffset = finalOffset; // 记录偏移量，拖拽松手时完美归位
                chessUnit.CurrentBenchSlot = emptyIndex;

                _benchedUnits[emptyIndex] = newUnit;
                // 升级三星check
                MergeManager.Instance.CheckForMerge(unitData, 1);
                
                Debug.Log($"[BenchManager] 成功在备战区 {emptyIndex} 号位生成了 {unitData.unitName}");
            }
            else
            {
                Debug.LogWarning("[BenchManager] 备战区已满，无法放置！");
            }
        }
    }
}