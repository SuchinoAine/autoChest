using System.Collections.Generic;
using AutoChess.Configs;
using AutoChess.Core;
using AutoChess.View;
using UnityEngine;


public class SandboxRunner : MonoBehaviour
{
    public GameObject unitPrefab;
    public AIConfig aIConfig;
    private BattleWorld _world = new();
    public BattleScenarioConfig scenario;
    public List<SpawnEntry> spawns = new();  // 生成用的单位列表
    private readonly Dictionary<string, UnitView> _views = new();

    [Header("Simulation")]
    [SerializeField] private float simDt = 0.02f; // 50 tick/s
    [SerializeField] private int maxStepsPerFrame = 8; // 防止卡顿时死循环追帧
    private float _accum;


    // debug mode
    [SerializeField] private bool showDevUI = true;
    private bool devPaused;
    private bool prevDevPaused = false;


    void Start()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
        InitWorld();
    }

    void Update()
    {
        // debug pause handling
        // 从暂停 -> 恢复的瞬间（可选回写）
        if (prevDevPaused && !devPaused)
        {
            SyncCoreFromViews();
            Debug.Log("SyncCoreFromViews called.");
        }
        prevDevPaused = devPaused;
        // 开发者暂停：不 Tick，也不覆盖位置
        if (devPaused) return;
        if (_world.IsEnded) return;

        float dt = simDt;
        // 累积真实帧间隔时间
        _accum += Time.deltaTime;
        // 防止 Debug 暂停/切后台回来 accum 爆炸
        _accum = Mathf.Min(_accum, dt * maxStepsPerFrame);
        int steps = 0;
        while (_accum >= dt && steps < maxStepsPerFrame)
        {
            _world.ResetLogs();
            _world.Tick(dt);
            // apply positions to views
            foreach (var u in _world.Units)
            {
                if (_views.TryGetValue(u.Id, out var v))
                    v.SetPos(u.Position);
            }

            // print logs (MVP: spam is ok; later we can throttle)
            foreach (var log in _world.Logs)
            {
                if (log.Type == AutoChess.Core.LogType.Death)
                {
                    // log.A = 死掉的单位 id
                    if (_views.TryGetValue(log.A, out var view))
                    {
                        // 方案A：直接隐藏（推荐，后面方便做对象池）
                        // view.gameObject.SetActive(false);
                        // 方案B：播放死亡动画
                        view.PlayDeathFade(0.25f); // 你可以调 0.2~0.5
                        // 从字典移除，避免后续还去更新它
                        _views.Remove(log.A);
                    }
                    // 如果还有 _goById 之类的映射，也同步 Remove
                    // _goById.Remove(log.A);

                }
                Debug.Log(log.ToString());
            }
            _accum -= dt;
            steps++;
        }
    }
    private Unit CreateUnitFromConfig(string id, UnitConfig cfg, Team team, Vector3 startPos)
    {
        return new Unit(
            id,
            team,
            cfg.hp,
            cfg.atk,
            cfg.atkInterval,
            cfg.moveSpeed,
            cfg.range,
            startPos,
            cfg.radius,
            cfg.isranged
        );
    }


    // -- Init & Spawn --
    private void InitWorld()
    {
        _world = new BattleWorld();
        _world.AiConfig = aIConfig;

        _views.Clear();
        var useSpawns = (scenario != null) ? scenario.spawns : spawns;

        // 空指针保护
        if (useSpawns == null || useSpawns.Count == 0)
        {
            Debug.LogWarning("No spawns configured in SandboxRunner.");
            return;
        }
        // spawn units according to spawn list
        int idxA = 0, idxB = 0;
        foreach (var s in useSpawns)
        {
            if (s == null || s.config == null) continue;
            // 生成唯一ID
            string id = s.team == Team.A ? $"A{++idxA}" : $"B{++idxB}";

            // 用生成的 id 覆盖 config.id
            // 如果 Unit 里 Id 是只读的（get-only），就直接在 CreateUnitFromConfig 里传入 id
            var unit = CreateUnitFromConfig(id, s.config, s.team, s.startPos);

            _world.Add(unit);
            SpawnView(unit);
        }


    }

    private void SpawnView(Unit u)
    {
        float diameter = u.Radius != 0.0f ? u.Radius * 2f : 0.5f;
        var go = Instantiate(unitPrefab);
        go.name = $"Unit_{u.Id}";
        var view = go.GetComponent<UnitView>();

        if (view == null) view = go.AddComponent<UnitView>();
        view.unitId = u.Id;
        view.team = u.Team;
        view.transform.localScale = Vector3.one * diameter;
        view.SetPos(u.Position);


        // simple color
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = u.Team == Team.A ? Color.cyan : Color.magenta;

        _views[u.Id] = view;
    }


    // debug helper: sync core unit positions from views
    private void SyncCoreFromViews()
    {
        foreach (var u in _world.Units)
        {
            if (_views.TryGetValue(u.Id, out var v))
            {
                var p3 = v.transform.position;
                // 和 UnitView.SetPos
                u.Position = new Vector3(p3.x, p3.y, p3.z);
            }
        }
    }
    private void ToggleDevPause()
    {
        devPaused = !devPaused;
        Time.timeScale = devPaused ? 0f : 1f;
    }

    private void OnGUI()
    {
        if (!showDevUI) return;

        const int w = 140, h = 40, pad = 12;
        var rect = new Rect(Screen.width - w - pad, pad, w, h);

        var label = devPaused ? "Resume (Dev)" : "Pause (Dev)";
        if (GUI.Button(rect, label)) ToggleDevPause();

        if (scenario == null)
        {
            if (GUI.Button(new Rect(Screen.width - w - pad, pad + h + 8, w, h), "Sim x100"))
            {
                var r = BattleSimulator.RunBatch(spawns, aIConfig, 100);
                Debug.Log($"[Sim] A wins={r.aWins}, B wins={r.bWins}, avgTime={r.avgDuration:F2}s");
            }
        }
        else
        {
            if (GUI.Button(new Rect(Screen.width - w - pad, pad + h + 8, w, h), "Sim x100"))
            {
                var r = BattleSimulator.RunBatchByScenario(scenario, 100);
                Debug.Log($"[Sim] A wins={r.aWins}, B wins={r.bWins}, avgTime={r.avgDuration:F2}s");
            }
        }
        if (GUI.Button(new Rect(Screen.width - w - pad, pad + 2*h + 16  , w, h), "Sim x1"))
        {
            var r = BattleSimulator.RunBatch(spawns, aIConfig, 1);
            Debug.Log($"[Sim] A wins={r.aWins}, B wins={r.bWins}, avgTime={r.avgDuration:F2}s");
        }
        
    }
}
