using UnityEngine;

namespace AutoChess.Managers
{
    public enum GamePhase { None, Preparation, Combat, Resolution }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GamePhase CurrentPhase { get; private set; }
        public float PhaseTimer { get; private set; }
        public int CurrentRound { get; private set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 游戏启动后，进入第一回合准备阶段 (布阵)
            ChangePhase(GamePhase.Preparation);
        }

        private void Update()
        {
            // 【已移除自动倒计时开战逻辑，完全交由玩家点击按钮控制】
            // 如果你以后还需要加回倒计时，可以在这里加个开关
        }

        // ✅ 新增：暴露给 UI 按钮的点击事件
        public void OnClickStartCombat()
        {
            if (CurrentPhase == GamePhase.Preparation)
            {
                Debug.Log("[GameManager] 玩家点击了开始战斗！");
                ChangePhase(GamePhase.Combat);
            }
        }

        public void ChangePhase(GamePhase newPhase)
        {
            CurrentPhase = newPhase;
            switch (newPhase)
            {
                case GamePhase.Preparation:
                    // 准备阶段，可以在这里触发 UI 显示等
                    GameEventBus.OnEnterPreparationPhase?.Invoke();
                    break;
                case GamePhase.Combat:
                    // 战斗阶段，触发开战事件
                    GameEventBus.OnEnterCombatPhase?.Invoke();
                    break;
                case GamePhase.Resolution:
                    CurrentRound++;
                    // 结算后重置回准备阶段
                    ChangePhase(GamePhase.Preparation); 
                    break;
            }
        }

        public void ReportCombatEnd(Core.Team winner)
        {
            if (CurrentPhase != GamePhase.Combat) return;
            Debug.Log($"[GameManager] 战斗结束，胜者: {winner}");
            ChangePhase(GamePhase.Resolution);
        }
    }
}