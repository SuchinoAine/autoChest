using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<CardDataSO, ObjectPool<GameObject>> _unitPools = new();
        private Dictionary<GameObject, ObjectPool<GameObject>> _prefabPools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public GameObject GetUnit(CardDataSO cardData, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!_unitPools.TryGetValue(cardData, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(cardData.prefab),
                    actionOnGet: (obj) => {
                        // ✅ 只负责激活，绝对不能在这里绑定位置（避免闭包捕获旧数据）
                        obj.SetActive(true);
                    },
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: 10,
                    maxSize: 30
                );
                _unitPools[cardData] = pool;
            }
            
            GameObject instance = pool.Get();
            // ✅ 核心修复：拿到实例后，实时赋予最新的位置和父节点
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.SetParent(parent);
            
            return instance;
        }

        public void ReleaseUnit(CardDataSO cardData, GameObject unitObj)
        {
            if (_unitPools.TryGetValue(cardData, out var pool)) pool.Release(unitObj);
            else Destroy(unitObj);
        }

        public GameObject GetPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!_prefabPools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: 5,
                    maxSize: 20
                );
                _prefabPools[prefab] = pool;
            }
            
            GameObject instance = pool.Get();
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.SetParent(parent);
            
            return instance;
        }

        public void ReleasePrefab(GameObject prefab, GameObject instanceObj)
        {
            if (_prefabPools.TryGetValue(prefab, out var pool)) pool.Release(instanceObj);
            else Destroy(instanceObj);
        }
    }
}