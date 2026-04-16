using UnityEngine;
using AutoChess.Configs;
using AutoChess.Core;

namespace AutoChess.Managers
{
    public class BenchManager : MonoBehaviour
    {
        public static BenchManager Instance { get; private set; }

        [Header("备战区节点配置")]
        [Tooltip("将层级图中的 Bench 父节点拖入这里，会自动读取它下面的 10 个 position")]
        public Transform benchRoot;

        private Transform[] _benchAnchors;
        private ChessUnit[] _benchedUnits;

        // 暴露给外部 DeployManager (布阵管理器) 读取和修改的属性
        public Transform[] BenchAnchors => _benchAnchors;
        public ChessUnit[] BenchedUnits => _benchedUnits;

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

            _benchedUnits = new ChessUnit[_benchAnchors.Length];
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

                // 1. 生成并认 position 为父节点
                GameObject newUnit = PoolManager.Instance.GetUnit(unitData, spawnPoint.position, spawnPoint.rotation, spawnPoint);

                // 保持预制体原本的缩放和旋转
                newUnit.transform.localRotation = unitData.prefab.transform.localRotation;
                newUnit.transform.localScale = unitData.prefab.transform.localScale;

                // ✅ 2. 模型已手动调整中心点，直接完美居中于锚点即可
                Vector3 finalOffset = Vector3.zero;
                newUnit.transform.localPosition = finalOffset;

                // 3. 加上物理碰撞体 (包住模型，方便鼠标点击拖拽)
                if (newUnit.GetComponent<Collider>() == null)
                {
                    var col = newUnit.AddComponent<BoxCollider>();

                    // 读取 SO 配置文件中的真实 radius
                    // 加一层防呆保护：如果策划忘了配或者体积太小，给一个最低兜底半径 0.4f
                    float validRadius = unitData.radius > 0.1f ? unitData.radius : 0.4f;
                    float diameter = validRadius * 2f;

                    // 使用真实直径作为 BoxCollider 的长和宽，高度依然保持 2f 方便点击
                    col.size = new Vector3(diameter, 1.5f, diameter);
                    col.center = new Vector3(0, 1f, 0);
                }

                // 4. 挂载身份证，供 DeployManager 拖拽和记录位置使用
                ChessUnit chessUnit = newUnit.GetComponent<ChessUnit>();
                if (chessUnit == null) chessUnit = newUnit.AddComponent<ChessUnit>();

                chessUnit.Data = unitData;
                chessUnit.BaseOffset = finalOffset; // 记录偏移量为 0，拖拽松手时完美归零
                chessUnit.CurrentBenchSlot = emptyIndex;

                _benchedUnits[emptyIndex] = chessUnit;

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