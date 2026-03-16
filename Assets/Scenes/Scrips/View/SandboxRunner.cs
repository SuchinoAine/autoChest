using System;
using System.Collections;
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
    public int row;
    public int col;

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

    private bool _isEnding = false;

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

    private IEnumerator HandleCombatEndCo()
    {
        Debug.Log($"[SandboxRunner] 战斗结束！胜利方是: {_world.Winner}");
        yield return new WaitForSeconds(2f);
        _isEnding = false;
        GameManager.Instance.ReportCombatEnd(_world.Winner);
    }

    private void LoadPlayerUnits()
    {
        int idxA = 0;
        var playerUnits = BoardManager.Instance.BoardUnits;
        var playerAnchors = BoardManager.Instance.BoardAnchors;

        Dictionary<string, int> activeSynergies = null;
        if (SynergyManager.Instance != null)
        {
            activeSynergies = SynergyManager.Instance.CalculateActiveSynergies();
        }
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                // 直接获取 ChessUnit
                ChessUnit cu = playerUnits[r, c];

                if (cu != null && cu.Data != null)
                {
                    GameObject go = cu.gameObject; // 通过组件反向获取 GameObject
                    Transform anchor = playerAnchors[r, c];
                    if (anchor == null) continue;

                    string id = $"A{++idxA}";
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



    private Unit CreateUnitFromCard(string id, CardDataSO card, Team team, Vector3 startPos, int starLevel, Dictionary<string, int> synergies)
    {
        if (card == null) return null;

        float multi = 1f;
        if (starLevel == 2) multi = 1.8f;
        if (starLevel == 3) multi = 3.6f;

        float finalHp = card.hp * multi;
        float finalAtk = card.atk * multi;
        float finalAtkInterval = card.atkInterval;
        float finalMoveSpeed = card.moveSpeed;
        float finalRange = card.range;

        if (team == Team.A && synergies != null && card.bonds != null)
        {
            HashSet<string> myBonds = new HashSet<string>();
            foreach (var b in card.bonds) if (b != null) myBonds.Add(b.bondName);

            if (myBonds.Contains("坚体") && synergies.ContainsKey("坚体"))
            {
                int count = synergies["坚体"];
                if (count >= 6) finalHp += 2000f;
                else if (count >= 4) finalHp += 1000f;
                else if (count >= 2) finalHp += 400f;
            }
            if (myBonds.Contains("锐体") && synergies.ContainsKey("锐体"))
            {
                int count = synergies["锐体"];
                if (count >= 6) finalAtk += 150f;
                else if (count >= 4) finalAtk += 80f;
                else if (count >= 2) finalAtk += 30f;
            }
            if (myBonds.Contains("圆体") && synergies.ContainsKey("圆体"))
            {
                int count = synergies["圆体"];
                if (count >= 6) { finalHp += 1200f; finalAtk += 100f; }
                else if (count >= 4) { finalHp += 500f; finalAtk += 50f; }
                else if (count >= 2) { finalHp += 200f; finalAtk += 20f; }
            }
            if (myBonds.Contains("悬体") && synergies.ContainsKey("悬体"))
            {
                int count = synergies["悬体"];
                if (count >= 4) { finalMoveSpeed += 3.0f; finalAtkInterval -= 0.35f; }
                else if (count >= 2) { finalMoveSpeed += 1.5f; finalAtkInterval -= 0.15f; }
            }

            if (myBonds.Contains("突击") && synergies.ContainsKey("突击"))
            {
                int count = synergies["突击"];
                if (count >= 6) finalAtk += 180f;
                else if (count >= 4) finalAtk += 100f;
                else if (count >= 2) finalAtk += 40f;
            }
            if (myBonds.Contains("壁垒") && synergies.ContainsKey("壁垒"))
            {
                int count = synergies["壁垒"];
                if (count >= 6) finalHp += 2500f;
                else if (count >= 4) finalHp += 1200f;
                else if (count >= 2) finalHp += 500f;
            }
            if (myBonds.Contains("射手") && synergies.ContainsKey("射手"))
            {
                int count = synergies["射手"];
                if (count >= 4) { finalRange += 2.0f; finalAtk += 80f; }
                else if (count >= 2) { finalRange += 1.0f; finalAtk += 30f; }
            }
            if (myBonds.Contains("矩阵") && synergies.ContainsKey("矩阵"))
            {
                int count = synergies["矩阵"];
                if (count >= 6) finalAtkInterval -= 0.6f;
                else if (count >= 4) finalAtkInterval -= 0.35f;
                else if (count >= 2) finalAtkInterval -= 0.15f;
            }
        }

        finalAtkInterval = Mathf.Max(0.2f, finalAtkInterval);

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
            if (anchor == null) continue;

            string id = $"B{++idxB}";

            // ✅ 核心修改：通过对象池获取野怪
            GameObject go = PoolManager.Instance.GetPrefab(enemy.prefab, anchor.position, Quaternion.Euler(0, 180, 0), anchor);

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
                    // ✅ 核心修改：查找当初生成的 Prefab Key，放回对象池
                    GameObject prefabKey = enemySetups.Find(e => e.row == r && e.col == c)?.prefab;
                    if (prefabKey != null)
                    {
                        PoolManager.Instance.ReleasePrefab(prefabKey, enemyUnits[r, c]);
                    }
                    else
                    {
                        Destroy(enemyUnits[r, c]);
                    }
                    enemyUnits[r, c] = null;
                }
            }
        }

        var playerUnits = BoardManager.Instance.BoardUnits;
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                // ✅ 修改这里：直接获取 ChessUnit
                ChessUnit cu = playerUnits[r, c];

                if (cu != null)
                {
                    GameObject go = cu.gameObject; // ✅ 反向获取 GameObject

                    go.transform.localPosition = cu.BaseOffset;
                    go.transform.localRotation = cu.Data.prefab.transform.localRotation;

                    if (!go.activeSelf) go.SetActive(true);
                    go.transform.localScale = cu.Data.prefab.transform.localScale;

                    var hud = go.GetComponentInChildren<UnitHud>(true);
                    if (hud != null) hud.gameObject.SetActive(true);
                }
            }
        }
    }
}
