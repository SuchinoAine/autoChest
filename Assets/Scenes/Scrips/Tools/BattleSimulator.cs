using System.Collections.Generic;
using UnityEngine;
using AutoChess.Core;
using AutoChess.Configs;
using TMPro;

public static class BattleSimulator
{
    public struct SimResult
    {
        public int aWins;
        public int bWins;
        public float avgDuration;
    }


    public static SimResult RunBatchByScenario(BattleScenarioConfig scenario, int runs)
    {
        return RunBatch(scenario.spawns, scenario.aiConfig, runs, scenario.fixedDt, scenario.maxTime);
    }

    public static SimResult RunBatch(List<SpawnEntry> spawns, AIConfig aiConfig, int runs, float dt = 0.02f, float maxTime = 60f)
    {
        int aWins = 0, bWins = 0;
        float sumTime = 0f;
        for (int i = 0; i < runs; i++)
        {
            var ai = Object.Instantiate(aiConfig);
            ai.battleSeed = aiConfig.battleSeed + i; // 每局不同，但可复现
            var world = BuildWorld(spawns, ai);
            Debug.Log($"[BattleSimulator] Run {i + 1}/{runs} with seed {world.AiConfig.battleSeed}");

            float t = 0f;
            while (!world.IsEnded && t < maxTime)
            {
                world.Tick(dt);
                t += dt;
            }
            sumTime += t;
            // 简单胜负判定：看存活阵营
            bool aAlive = false, bAlive = false;
            foreach (var u in world.Units)
            {
                if (u.IsDead) continue;
                if (u.Team == Team.A) aAlive = true;  
                if (u.Team == Team.B) bAlive = true;
            }

            if (aAlive && !bAlive) aWins++;
            else if (bAlive && !aAlive) bWins++;
        }
        return new SimResult
        {
            aWins = aWins,
            bWins = bWins,
            avgDuration = sumTime / runs
        };
    }

    private static BattleWorld BuildWorld(List<SpawnEntry> spawns, AIConfig aiConfig)
    {
        var world = new BattleWorld();
        world.AiConfig = aiConfig;

        int idxA = 0, idxB = 0;

        foreach (var s in spawns)
        {
            if (s == null || s.config == null) continue;
            string id = s.team == Team.A ? $"A{++idxA}" : $"B{++idxB}";
            var cfg = s.config;
            var unit = new Unit(
                id,
                s.team,
                cfg.hp,
                cfg.atk,
                cfg.atkInterval,
                cfg.moveSpeed,
                cfg.range,
                new Vector3(s.startPos.x, 0f, s.startPos.z),
                cfg.radius,
                cfg.isranged
                
            );
            world.Add(unit);
        }

        return world;
    }
}

