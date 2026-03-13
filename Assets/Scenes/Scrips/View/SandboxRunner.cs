using System.Collections.Generic;
using System.IO;
using AutoChess.Configs;
using AutoChess.Core;
using AutoChess.View;
using AutoChess.View.Hud;
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
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        InitWorld();
    }

    void Update()
    {
        // ✅ 核心拦截：如果 GameManager 存在，且当前不是战斗阶段，就直接返回，不推进 Tick
        if (AutoChess.Managers.GameManager.Instance != null && 
            AutoChess.Managers.GameManager.Instance.CurrentPhase != AutoChess.Managers.GamePhase.Combat)
        {
            return;
        }
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
        
        // ✅在 Tick 完之后检查战斗是否结束，如果结束则通知 GameManager 结算
        if (_world.IsEnded)
        {
            if (AutoChess.Managers.GameManager.Instance != null)
            {
                AutoChess.Managers.GameManager.Instance.ReportCombatEnd(_world.Winner);
            }
        }
    }

    private Unit CreateUnitFromConfig(string id, UnitConfig cfg, Team team, Vector3 startPos,
                                    SkillDefSO basicAttack,
                                    SkillDefSO deaultskill)
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
            cfg.isranged,
            basicAttack,
            deaultskill
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

        // 空指针保护
        var useSpawns = (scenario != null) ? scenario.spawns : spawns;
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
            Vector3 startPos = s.startPos;

            // 使用棋盘 anchor：按生成顺序填 Row/Col
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
            var unit = CreateUnitFromConfig(id, s.config, s.team, startPos, s.basicAttack, s.defaultSkill);
            _world.Add(unit);
            SpawnView(unit); 
        }
    }

    private void SpawnView(Unit u)
    {
        if (UnitFactory == null)
        {
            Debug.LogError("[SandboxRunner] UnitFactory is null!");
            return;
        }
        var view = UnitFactory.CreateU(u.Id, u.Team, u.Position, u.Radius);
        var hud = view.GetComponentInChildren<UnitHud>(true);
        if (hud != null) hud.Bind(u);

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
                u.Position = new Vector3(p3.x, 0, p3.z);
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

