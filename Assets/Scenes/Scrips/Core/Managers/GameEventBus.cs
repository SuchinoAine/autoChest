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

        // === 等级与经验事件 ===
        // 参数: 当前等级, 当前经验, 升下一级所需经验
        public static Action<int, int, int> OnLevelExpChanged; 

        // ✅ 新增：羁绊UI刷新事件 (传递羁绊数据SO字典，供左侧UI渲染)
        public static Action<Dictionary<BondDataSO, int>> OnSynergyChanged; 
    }
}