using System;
using System.Collections.Generic;
using AutoChess.Core;
using AutoChess.View;
using UnityEngine;



public class SandboxRunner : MonoBehaviour
{
    public UnitConfig unitAConfig;
    public UnitConfig unitBConfig;
    public GameObject unitPrefab;
    public AIConfig aIConfig;

    private BattleWorld _world = new();
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
            // Debug.Log(log.ToString());
        }

    }
    private Unit CreateUnitFromConfig(UnitConfig cfg, Team team, Vector2 startPos)
    {
        return new Unit(
            cfg.id,
            team,
            cfg.hp,
            cfg.atk,
            cfg.atkInterval,
            cfg.moveSpeed,
            cfg.range,
            startPos
        );
    }

    private void InitWorld()
    {
        _world = new BattleWorld();
        _world.AiConfig = aIConfig;
        _views.Clear();

        // create two units
        var u1 = CreateUnitFromConfig(unitAConfig, Team.A, new Vector2(-4f, 0f));
        var u2 = CreateUnitFromConfig(unitBConfig, Team.B, new Vector2(4f, 6f));


        _world.Add(u1);
        _world.Add(u2);

        SpawnView(u1);
        SpawnView(u2);
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
                // 和 UnitView.SetPos 保持一致：Vector2(x, z)
                u.Position = new Vector2(p3.x, p3.z);
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
