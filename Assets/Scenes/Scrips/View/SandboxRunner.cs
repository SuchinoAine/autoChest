using System.Collections.Generic;
using System.IO;
using AutoChess.Configs;
using AutoChess.Core;
using AutoChess.View;
using UnityEngine;


public class SandboxRunner : MonoBehaviour
{
    // public GameObject unitPrefab;
    public UnitFactory UnitFactory;   // 在 Inspector 里拖引用
    public AIConfig aIConfig;
    private BattleWorld _world = new();
    public BattleScenarioConfig scenario;
    public List<SpawnEntry> spawns = new();  // 生成用的单位列表
    private readonly Dictionary<string, UnitView> _views = new();

    [Header("Simulation")]
    [SerializeField] private float simDt = 0.02f;       // 50 tick/s
    [SerializeField] private int maxStepsPerFrame = 8;  // 防止卡顿时死循环追帧
    private float _accum;

    // debug mode
    [SerializeField] private bool showDevUI = true;
    private bool devPaused;
    private bool prevDevPaused = false;

    [Header("Logging")]
    public bool enableJsonLog = true;
    public string logDirectory = "./BattleLogs";  // 相对路径 or 绝对路径
    public bool logMove = false;

    [Header("Board Anchors (Deploy)")]
    public BoardAnchorGrid anchorGrid;   // 拖 BoardRing 上挂的 BoardAnchorGrid
    public bool useAnchorGridForSpawn = true;

    void Start()
    {
        Application.targetFrameRate = 100;
        QualitySettings.vSyncCount = 0;
        InitWorld();
    }

    void Update()
    {
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

        // 累积真实帧间隔时间, 防止 Debug 暂停/切后台回来 accum 爆炸
        float dt = simDt;
        _accum += Time.deltaTime;
        _accum = Mathf.Min(_accum, dt * maxStepsPerFrame);
        int steps = 0;
        while (_accum >= dt && steps < maxStepsPerFrame)
        {
            _world.Tick(dt);
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

    private void InitWorld()
    {
        // -- Init & Spawn --
        _world = new BattleWorld();
        _world.AiConfig = aIConfig;
        _world.BattleController = new BattleController();
        _world.Sinks.Clear();
        _views.Clear();
        _world.AddSink(new BattleViewSink(_views));

        if (enableJsonLog)
        {
            int seed = aIConfig != null ? aIConfig.battleSeed : 0;
            string dir = logDirectory;
            if (!Path.IsPathRooted(dir)) dir = Path.Combine(Application.persistentDataPath, dir);

            string path = Path.Combine(dir, $"battle_unity_seed{seed}.jsonl");
            Debug.Log($"[BattleLog] Writing to: {path}");
            _world.AddSink(new JsonlLogSink(path, seed, simDt, logMove));
        }

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
            string id = s.team == Team.A ? $"A{++idxA}" : $"B{++idxB}";  // 生成唯一ID
            Vector3 startPos = s.startPos;

            // ✅ 使用棋盘 anchor：按生成顺序填 Row/Col
            if (useAnchorGridForSpawn && anchorGrid != null)
            {
                if (anchorGrid.Try2Build())
                {
                    int indexInTeam = (s.team == Team.A) ? (idxA - 1) : (idxB - 1);
                    int row = indexInTeam / anchorGrid.cols;
                    int col = indexInTeam % anchorGrid.cols;

                    startPos = anchorGrid.GetDeployWorldPos(s.team, row, col);
                }
                else
                {
                    Debug.LogWarning("[SandboxRunner] AnchorGrid not ready, fallback to SpawnEntry.startPos");
                }
            }
            var unit = CreateUnitFromConfig(id, s.config, s.team, startPos);
            _world.Add(unit);
            SpawnView(unit);
        }
    }

    // private void SpawnView(Unit u)
    // {
    //     float diameter = u.Radius != 0.0f ? u.Radius * 2f : 0.5f;
    //     var go = Instantiate(unitPrefab);
    //     go.name = $"Unit_{u.Id}";
    //     var view = go.GetComponent<UnitView>();

    //     if (view == null) view = go.AddComponent<UnitView>();
    //     view.unitId = u.Id;
    //     view.team = u.Team;
    //     view.transform.localScale = Vector3.one * diameter;
    //     view.SetPos(u.Position);


    //     // simple color
    //     var renderer = go.GetComponent<Renderer>();
    //     if (renderer != null)
    //         renderer.material.color = u.Team == Team.A ? Color.cyan : Color.magenta;

    //     _views[u.Id] = view;
    // }

    private void SpawnView(Unit u)
    {
        if (UnitFactory == null)
        {
            Debug.LogError("[SandboxRunner] UnitFactory is null!");
            return;
        }
        var view = UnitFactory.CreateU(u.Id, u.Team, u.Position);
        // view.modelRoot.localScale = new Vector3(u.Radius,u.Radius,u.Radius);

        _views[u.Id] = view;
    }


    private void SyncCoreFromViews()
    {
        // debug helper: sync core unit positions from views
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
        if (GUI.Button(new Rect(Screen.width - w - pad, pad + 2 * h + 16, w, h), "Sim x1"))
        {
            var r = BattleSimulator.RunBatch(spawns, aIConfig, 1);
            Debug.Log($"[Sim] A wins={r.aWins}, B wins={r.bWins}, avgTime={r.avgDuration:F2}s");
        }

    }

    private void OnDisable()
    {
        _world.Shutdown();
    }

    private void OnDestroy()
    {
        _world.Shutdown();
    }

    private void OnApplicationQuit()
    {
        _world.Shutdown();
    }

}

