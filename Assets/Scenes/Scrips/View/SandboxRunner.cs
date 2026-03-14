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

    private Unit CreateUnitFromCard(string id, CardDataSO card, Team team, Vector3 startPos, int starLevel)
        {
            if (card == null) return null;
            
            // ✅ 核心：星级数值膨胀系数！2星1.8倍，3星3.6倍（你可以按需调整）
            float multi = 1f;
            if (starLevel == 2) multi = 1.8f;
            if (starLevel == 3) multi = 3.6f;

            return new Unit(
                id, team, 
                card.hp * multi, card.atk * multi, card.atkInterval, card.moveSpeed, 
                card.range, startPos, card.radius, card.isranged, 
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

    private void LoadPlayerUnits()
    {
        int idxA = 0;
        var playerUnits = BoardManager.Instance.BoardUnits;
        var playerAnchors = BoardManager.Instance.BoardAnchors;

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
                        var unit = CreateUnitFromCard(id, cu.Data, Team.A, anchor.position, cu.StarLevel);
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