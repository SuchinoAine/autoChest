using System;
using System.Collections; // ✅ 新增：用于支持协程 IEnumerator
using System.Collections.Generic;
using AutoChess.Configs;
using AutoChess.Core;
using AutoChess.View;
using AutoChess.View.Hud;
using AutoChess.Managers;
using UnityEngine;

[Serializable]
public class EnemySetup
{
    public int row; // 0-3
    public int col; // 0-6

    [Header("模型与表现")]
    public GameObject prefab;
    public float radius = 0.5f;

    [Header("战斗数值 (纯Unit)")]
    public float hp = 1000f;
    public float atk = 50f;
    public float atkInterval = 1.2f;
    public float moveSpeed = 3f;
    public float range = 1.5f;
    public bool isRanged = false;

    [Header("技能")]
    public SkillDefSO basicAttack;
    public SkillDefSO defaultSkill;
}

public class SandboxRunner : MonoBehaviour
{
    public AIConfig aIConfig;
    private BattleWorld _world = new();
    private readonly Dictionary<string, UnitView> _views = new();

    [Header("Simulation")]
    [SerializeField] private float simDt = 0.02f;
    [SerializeField] private int maxStepsPerFrame = 8;
    private float _accum;

    [Header("敌方野怪配置 (Enemies)")]
    public List<EnemySetup> enemySetups = new();

    [Header("HUD")]
    public GameObject unitHudPrefab;

    private bool _isEnding = false; // ✅ 新增：防止协程重复触发

    void Start()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;
    }

    private void OnEnable()
    {
        GameEventBus.OnEnterCombatPhase += InitWorld;
        GameEventBus.OnEnterPreparationPhase += ResetBoard;
    }

    private void OnDisable()
    {
        GameEventBus.OnEnterCombatPhase -= InitWorld;
        GameEventBus.OnEnterPreparationPhase -= ResetBoard;
        _world.Shutdown();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Combat) return;

        // ✅ 核心修改 1：拦截战斗结束瞬间，转交给协程处理
        if (_world.IsEnded)
        {
            if (!_isEnding)
            {
                _isEnding = true;
                StartCoroutine(HandleCombatEndCo());
            }
            return;
        }

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

    // ✅ 核心修改 2：新增战斗结束协程
    private IEnumerator HandleCombatEndCo()
    {
        // 1. Debug 打印胜利方
        Debug.Log($"[SandboxRunner] 战斗结束！胜利方是: {_world.Winner}");

        // 2. 暂停/缓冲 2 秒，让玩家看清结果
        yield return new WaitForSeconds(2f);

        // 3. 释放锁，并正式通知 GameManager 结算（这会引发状态切换并调用 ResetBoard）
        _isEnding = false;
        GameManager.Instance.ReportCombatEnd(_world.Winner);
    }


    private void LoadPlayerUnits()
    {
        int idxA = 0;
        var playerUnits = BoardManager.Instance.BoardUnits;
        var playerAnchors = BoardManager.Instance.BoardAnchors;

        // ✅ 1. 开战前，先向管家索要当前的羁绊计算结果 (字典的 Key 是羁绊的中文名 string)
        Dictionary<string, int> activeSynergies = null;
        if (SynergyManager.Instance != null)
        {
            activeSynergies = SynergyManager.Instance.CalculateActiveSynergies();
        }
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                GameObject go = playerUnits[r, c];
                if (go != null)
                {
                    ChessUnit cu = go.GetComponent<ChessUnit>();
                    if (cu != null && cu.Data != null)
                    {
                        Transform anchor = playerAnchors[r, c];
                        if (anchor == null) continue;

                        string id = $"A{++idxA}";
                        // ✅ 2. 把羁绊字典传给构造函数
                        var unit = CreateUnitFromCard(id, cu.Data, Team.A, anchor.position, cu.StarLevel, activeSynergies);
                        if (unit != null)
                        {
                            UnitView view = go.GetComponent<UnitView>();
                            if (view == null) view = go.AddComponent<UnitView>();
                            view.unitId = id;
                            view.team = Team.A;

                            _world.Add(unit);
                            _views[id] = view;
                            AttachHud(go, unit);
                        }
                    }
                }
            }
        }
    }

    // ✅ 接收羁绊字典，并在生成战斗 Unit 时加上额外的数值
    private Unit CreateUnitFromCard(string id, CardDataSO card, Team team, Vector3 startPos, int starLevel, Dictionary<string, int> synergies)
    {
        if (card == null) return null;

        // 1. 基础星级数值膨胀 (1星1倍，2星1.8倍，3星3.6倍)
        float multi = 1f;
        if (starLevel == 2) multi = 1.8f;
        if (starLevel == 3) multi = 3.6f;

        float finalHp = card.hp * multi;
        float finalAtk = card.atk * multi;
        float finalAtkInterval = card.atkInterval;
        float finalMoveSpeed = card.moveSpeed;
        float finalRange = card.range;

        // 2. 核心：羁绊数值增强 (只给己方英雄加成，且需要满足羁绊条件)
        if (team == Team.A && synergies != null && card.bonds != null)
        {
            // 提取该棋子身上的所有羁绊名称，方便快速判断
            HashSet<string> myBonds = new HashSet<string>();
            foreach (var b in card.bonds) if (b != null) myBonds.Add(b.bondName);

            // ================= 【形体羁绊】 =================
            // 🛡️ 坚体 [2/4/6]: 纯粹的生命力强化
            if (myBonds.Contains("坚体") && synergies.ContainsKey("坚体"))
            {
                int count = synergies["坚体"];
                if (count >= 6) finalHp += 2000f;
                else if (count >= 4) finalHp += 1000f;
                else if (count >= 2) finalHp += 400f;
            }

            // ⚔️ 锐体 [2/4/6]: 极致的攻击力提升
            if (myBonds.Contains("锐体") && synergies.ContainsKey("锐体"))
            {
                int count = synergies["锐体"];
                if (count >= 6) finalAtk += 150f;
                else if (count >= 4) finalAtk += 80f;
                else if (count >= 2) finalAtk += 30f;
            }

            // ☯️ 圆体 [2/4/6]: 均衡的生命与攻击加成
            if (myBonds.Contains("圆体") && synergies.ContainsKey("圆体"))
            {
                int count = synergies["圆体"];
                if (count >= 6) { finalHp += 1200f; finalAtk += 100f; }
                else if (count >= 4) { finalHp += 500f; finalAtk += 50f; }
                else if (count >= 2) { finalHp += 200f; finalAtk += 20f; }
            }

            // 💨 悬体 [2/4]: 高机动性，移速与攻速(攻击间隔)提升
            if (myBonds.Contains("悬体") && synergies.ContainsKey("悬体"))
            {
                int count = synergies["悬体"];
                if (count >= 4) { finalMoveSpeed += 3.0f; finalAtkInterval -= 0.35f; }
                else if (count >= 2) { finalMoveSpeed += 1.5f; finalAtkInterval -= 0.15f; }
            }

            // ================= 【定位羁绊】 =================
            // 🔪 突击 [2/4/6]: 恐怖的基础伤害附加
            if (myBonds.Contains("突击") && synergies.ContainsKey("突击"))
            {
                int count = synergies["突击"];
                if (count >= 6) finalAtk += 180f;
                else if (count >= 4) finalAtk += 100f;
                else if (count >= 2) finalAtk += 40f;
            }

            // 🧱 壁垒 [2/4/6]: 坚不可摧的前排血量
            if (myBonds.Contains("壁垒") && synergies.ContainsKey("壁垒"))
            {
                int count = synergies["壁垒"];
                if (count >= 6) finalHp += 2500f;
                else if (count >= 4) finalHp += 1200f;
                else if (count >= 2) finalHp += 500f;
            }

            // 🏹 射手 [2/4]: 增加射程与部分攻击力
            if (myBonds.Contains("射手") && synergies.ContainsKey("射手"))
            {
                int count = synergies["射手"];
                if (count >= 4) { finalRange += 2.0f; finalAtk += 80f; }
                else if (count >= 2) { finalRange += 1.0f; finalAtk += 30f; }
            }

            // 🔮 矩阵 [2/4/6]: 大幅度缩短攻击间隔 (增加攻速)
            if (myBonds.Contains("矩阵") && synergies.ContainsKey("矩阵"))
            {
                int count = synergies["矩阵"];
                if (count >= 6) finalAtkInterval -= 0.6f;
                else if (count >= 4) finalAtkInterval -= 0.35f;
                else if (count >= 2) finalAtkInterval -= 0.15f;
            }
        }

        // 3. 安全校验：保证攻击间隔永远不可能为负数或0（极限攻速限制为一秒五刀）
        finalAtkInterval = Mathf.Max(0.2f, finalAtkInterval);

        // 4. 生成携带全部羁绊与星级Buff的终极单位！
        return new Unit(
            id, team,
            finalHp, finalAtk, finalAtkInterval, finalMoveSpeed,
            finalRange, startPos, card.radius, card.isranged,
            card.basicAttack, card.defaultSkill
        );
    }

    private void InitWorld()
    {
        Debug.Log("[SandboxRunner] 战斗开始！正在初始化战场...");
        _world = new BattleWorld();
        _world.AiConfig = aIConfig;
        _world.BattleController = new BattleController();
        _world.Sinks.Clear();
        _views.Clear();
        _world.AddSink(new BattleViewSink(_views));

        LoadPlayerUnits();
        LoadEnemyUnits();

        if (_world.Units.Count == 0)
        {
            Debug.LogError("【警告】战场上没有任何单位！战斗直接结束。");
        }
    }


    private void LoadEnemyUnits()
    {
        int idxB = 0;
        var enemyAnchors = BoardManager.Instance.BoardAnchorsEne;
        var enemyUnits = BoardManager.Instance.BoardUnitsEne;

        foreach (var enemy in enemySetups)
        {
            if (enemy.prefab == null) continue;
            if (enemy.row < 0 || enemy.row >= 4 || enemy.col < 0 || enemy.col >= 7) continue;

            Transform anchor = enemyAnchors[enemy.row, enemy.col];
            if (anchor == null)
            {
                Debug.LogError($"[SandboxRunner] 无法生成敌人！找不到敌方棋盘 第 {enemy.row} 行，第 {enemy.col} 列的格子。");
                continue;
            }

            string id = $"B{++idxB}";
            GameObject go = Instantiate(enemy.prefab, anchor);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);

            MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                float offsetY = -mf.sharedMesh.bounds.min.y * go.transform.localScale.y;
                go.transform.localPosition = new Vector3(0, offsetY, 0);
            }

            UnitView view = go.GetComponent<UnitView>();
            if (view == null) view = go.AddComponent<UnitView>();
            view.unitId = id;
            view.team = Team.B;

            enemyUnits[enemy.row, enemy.col] = go;

            var unit = new Unit(
                id, Team.B,
                enemy.hp, enemy.atk, enemy.atkInterval, enemy.moveSpeed,
                enemy.range, anchor.position, enemy.radius, enemy.isRanged,
                enemy.basicAttack, enemy.defaultSkill
            );

            if (unit != null)
            {
                _world.Add(unit);
                _views[id] = view;
                AttachHud(go, unit);
            }
        }
    }

    public void AttachHud(GameObject go, Unit unit)
    {
        if (unitHudPrefab == null) return;

        var hud = go.GetComponentInChildren<UnitHud>(true);
        if (hud == null)
        {
            var hudGo = Instantiate(unitHudPrefab, go.transform);
            hudGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            hud = hudGo.GetComponent<UnitHud>();
        }

        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Bind(unit);
        }
    }

    private void ResetBoard()
    {
        if (BoardManager.Instance == null) return;

        // ✅ 核心修改 3：在归位前，把存活/死亡的逻辑 Unit 强行回满血，刷新CD
        if (_world != null && _world.Units != null)
        {
            foreach (var u in _world.Units)
            {
                if (u.Team == Team.A)
                {
                    u.Hp = u.MaxHp;
                    u.AtkCdLeft = 0f;
                    foreach (var skill in u.Skills) skill.CdLeft = 0f;
                }
            }
        }

        var enemyUnits = BoardManager.Instance.BoardUnitsEne;
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                if (enemyUnits[r, c] != null)
                {
                    Destroy(enemyUnits[r, c]);
                    enemyUnits[r, c] = null;
                }
            }
        }

        var playerUnits = BoardManager.Instance.BoardUnits;
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                GameObject go = playerUnits[r, c];
                if (go != null)
                {
                    ChessUnit cu = go.GetComponent<ChessUnit>();
                    if (cu != null)
                    {
                        go.transform.localPosition = cu.BaseOffset;
                        go.transform.localRotation = cu.Data.prefab.transform.localRotation;

                        if (!go.activeSelf) go.SetActive(true);
                        go.transform.localScale = cu.Data.prefab.transform.localScale;

                        // ✅ 强行唤醒可能被隐藏的血条
                        var hud = go.GetComponentInChildren<UnitHud>(true);
                        if (hud != null) hud.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}