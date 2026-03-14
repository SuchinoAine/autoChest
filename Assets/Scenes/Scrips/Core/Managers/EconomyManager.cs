using UnityEngine;
using AutoChess.Core;

namespace AutoChess.Managers
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("经济数值配置")]
        public int initialGold = 10;        // 初始金币
        public int baseRoundIncome = 5;     // 每回合基础收入
        public int maxInterest = 5;         // 最高利息上限

        public int CurrentGold { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 初始化金币并广播给 UIManager
            CurrentGold = initialGold;
            GameEventBus.OnCoinChanged?.Invoke(CurrentGold);
        }

        private void OnEnable() => GameEventBus.OnEnterPreparationPhase += GrantRoundIncome;
        private void OnDisable() => GameEventBus.OnEnterPreparationPhase -= GrantRoundIncome;

        // ✅ 加钱 (出售、发工资)
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            GameEventBus.OnCoinChanged?.Invoke(CurrentGold); // 通知 UIManager 刷新文本
        }

        // ✅ 扣钱 (买怪、买经验、D牌)
        public bool SpendGold(int amount)
        {
            if (amount < 0) return false;
            
            if (CurrentGold >= amount)
            {
                CurrentGold -= amount;
                GameEventBus.OnCoinChanged?.Invoke(CurrentGold);
                return true;
            }
            
            Debug.LogWarning("[Economy] 穷鬼，没钱了！");
            return false;
        }

    // 核心：自走棋利息结算逻辑
        private void GrantRoundIncome()
        {
            // ✅ 核心修复：如果是第一回合，直接跳过发工资（因为已经拿了 initialGold 初始资金）
            if (GameManager.Instance != null && GameManager.Instance.CurrentRound == 1)
            {
                Debug.Log("[Economy] 第 1 回合，使用启动资金，不发工资。");
                return;
            }

            // 利息计算：每存 10 块钱给 1 块钱利息，最高不超过 5
            int interest = Mathf.Min(CurrentGold / 10, maxInterest);
            int totalIncome = baseRoundIncome + interest;
            
            AddGold(totalIncome);
            Debug.Log($"<color=green>[Economy] 第 {GameManager.Instance.CurrentRound} 回合结算！基础 {baseRoundIncome} + 利息 {interest} = 获得 {totalIncome} 金币。当前存款：{CurrentGold}</color>");
        }
    }
}