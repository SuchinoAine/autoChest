using System;
using AutoChess.Configs;
using System.Collections.Generic;

namespace AutoChess.Managers
{
    public static class GameEventBus
    {
        // === 状态流转事件 ===
        public static Action OnEnterPreparationPhase; // 进入D牌/布阵阶段
        public static Action OnEnterCombatPhase;      // 进入战斗阶段
        public static Action<Core.Team> OnCombatEnd;  // 战斗结束

        // === 商店与经济事件 ===
        public static Action<int> OnCoinChanged;      // 金币数值变化
        
        public static Action<List<CardDataSO>> OnShopRefreshed; 
        public static Action<CardDataSO> OnUnitPurchased; 
    }
}