using System.Collections.Generic;
using UnityEngine;
using AutoChess.Core;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public class SynergyManager : MonoBehaviour
    {
        public static SynergyManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ⚔️ 给战斗系统用的：把 SO 转换成 String 字典返回 (SandboxRunner调用)
        public Dictionary<string, int> CalculateActiveSynergies()
        {
            var stringDict = new Dictionary<string, int>();
            foreach (var kvp in GetActiveBondsSO())
            {
                if (kvp.Key != null && !string.IsNullOrEmpty(kvp.Key.bondName))
                {
                    stringDict[kvp.Key.bondName] = kvp.Value;
                }
            }
            return stringDict;
        }

        // 📺 给 UI 系统用的：计算后直接通过 EventBus 广播出去 (DeployManager调用)
        public void BroadcastSynergiesToUI()
        {
            var dict = GetActiveBondsSO();
            GameEventBus.OnSynergyChanged?.Invoke(dict);
        }

        // 核心计算逻辑：返回包含 SO 和激活数量的字典
        private Dictionary<BondDataSO, int> GetActiveBondsSO()
        {
            var boardUnits = BoardManager.Instance.BoardUnits;
            HashSet<string> uniqueUnits = new HashSet<string>();
            Dictionary<BondDataSO, int> synergyCounts = new Dictionary<BondDataSO, int>();

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (boardUnits[r, c] != null)
                    {
                        var cu = boardUnits[r, c].GetComponent<ChessUnit>();
                        // 保证不重名
                        if (cu != null && cu.Data != null && !uniqueUnits.Contains(cu.Data.unitName))
                        {
                            uniqueUnits.Add(cu.Data.unitName);
                            if (cu.Data.bonds != null)
                            {
                                foreach (BondDataSO bond in cu.Data.bonds)
                                {
                                    if (bond == null) continue;
                                    if (!synergyCounts.ContainsKey(bond)) synergyCounts[bond] = 0;
                                    synergyCounts[bond]++;
                                }
                            }
                        }
                    }
                }
            }
            return synergyCounts;
        }
    }
}