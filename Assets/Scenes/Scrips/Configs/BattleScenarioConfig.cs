using System.Collections.Generic;
using UnityEngine;
using AutoChess.Configs;


[CreateAssetMenu(menuName = "AutoChess/Battle Scenario", fileName = "BattleScenario")]
public class BattleScenarioConfig : ScriptableObject
{
    public AIConfig aiConfig;
    public List<SpawnEntry> spawns = new();

    [Header("Simulation")]
    public float fixedDt = 0.02f;
    public float maxTime = 60f;
}
