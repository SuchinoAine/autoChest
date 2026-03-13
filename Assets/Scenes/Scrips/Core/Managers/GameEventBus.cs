using System;
using System.Collections.Generic;
using AutoChess.Configs;

namespace AutoChess.Managers
{
    public static class GameEventBus
    {
        // === 状态流转事件 ===
        public static Action OnEnterPreparationPhase; // 进入D牌/布阵阶段
        public static Action OnEnterCombatPhase;      // 进入战斗阶段
        public static Action<Core.Team> OnCombatEnd;  // 战斗结束 (由 SandboxRunner 触发)

        // === 商店与经济事件 ===
        public static Action<int> OnCoinChanged;      // 金币数值变化
        
        // 传递真实的卡池数据列表
        public static Action<List<CardDataSO>> OnShopRefreshed; 
        
        // 传递购买的真实卡牌数据
        public static Action<CardDataSO> OnUnitPurchased; 
    }
}