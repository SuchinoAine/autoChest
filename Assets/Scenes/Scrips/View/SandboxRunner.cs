using System;
using System.Collections.Generic;
using AutoChess.Core;
using AutoChess.View;
using UnityEngine;

[Serializable]
public class SpawnEntry
    {
        public UnitConfig config;
        public Team team;
        public Vector3 startPos;
    }

public class SandboxRunner : MonoBehaviour
{
    public GameObject unitPrefab;
    public AIConfig aIConfig;
    private BattleWorld _world = new();
    public List<SpawnEntry> spawns = new();  // 生成用的单位列表
    private readonly Dictionary<string, UnitView> _views = new();

    // debug mode
    [SerializeField] private bool showDevUI = true;
    private bool devPaused;
    private bool prevDevPaused = false;

    void Start()
    {
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
        _world.ResetLogs();
        _world.Tick(Time.deltaTime);

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

                // 如果你还有 _goById 之类的映射，也同步 Remove
                // _goById.Remove(log.A);
            }

            Debug.Log(log.ToString());
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
            cfg.redius
        );
    }


    // -- Init & Spawn --
    private void InitWorld()
    {
        _world = new BattleWorld();
        _world.AiConfig = aIConfig;
        _views.Clear();

        // 空指针保护
        if (spawns == null || spawns.Count == 0)
        {
            Debug.LogWarning("No spawns configured in SandboxRunner.");
            return;
        }

        // spawn units according to spawn list

        // 死生成 id 单位
        int idxA = 0, idxB = 0;
        foreach (var s in spawns)
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
        var go = Instantiate(unitPrefab);
        go.name = $"Unit_{u.Id}";
        var view = go.GetComponent<UnitView>();
        if (view == null) view = go.AddComponent<UnitView>();

        view.unitId = u.Id;
        view.team = u.Team;
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
                u.Position = new Vector3(p3.x, p3.y ,p3.z);
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
        if (GUI.Button(rect, label))
            ToggleDevPause();
    }
}
