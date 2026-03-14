using UnityEngine;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class BenchManager : MonoBehaviour
    {
        public static BenchManager Instance { get; private set; }

        [Header("备战区槽位配置")]
        [Tooltip("按顺序拖入备战区格子的 Transform (通常是8或9个)")]
        public Transform[] benchAnchors;

        // 内部数组，记录每个槽位当前放着哪个棋子的 GameObject
        private GameObject[] _benchedUnits;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            
            // 初始化数组长度与槽位一致
            _benchedUnits = new GameObject[benchAnchors.Length];
        }

        private void OnEnable()
        {
            // 监听购买成功事件
            GameEventBus.OnUnitPurchased += OnUnitPurchased;
        }

        private void OnDisable()
        {
            GameEventBus.OnUnitPurchased -= OnUnitPurchased;
        }

        /// <summary>
        /// 检查备战区是否已满
        /// </summary>
        public bool IsBenchFull()
        {
            for (int i = 0; i < _benchedUnits.Length; i++)
            {
                if (_benchedUnits[i] == null) return false; // 只要有一个空位就不算满
            }
            return true;
        }

        /// <summary>
        /// 当玩家成功购买棋子时调用
        /// </summary>
        private void OnUnitPurchased(CardDataSO unitData)
        {
            if (unitData.prefab == null)
            {
                Debug.LogError($"[BenchManager] 卡牌 {unitData.unitName} 没有配置模型 Prefab！");
                return;
            }

            // 找一个空位
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
                // 在对应的空位生成 3D 模型
                Transform spawnPoint = benchAnchors[emptyIndex];
                GameObject newUnit = Instantiate(unitData.prefab, spawnPoint.position, spawnPoint.rotation);
                
                // 存入数组
                _benchedUnits[emptyIndex] = newUnit;
                
                Debug.Log($"[BenchManager] 在备战区 {emptyIndex} 号位生成了 {unitData.unitName}");
            }
            else
            {
                // 理论上走到这里说明出Bug了，因为商店买之前应该检查过
                Debug.LogWarning("[BenchManager] 备战区已满，无法放置！");
            }
        }
    }
}