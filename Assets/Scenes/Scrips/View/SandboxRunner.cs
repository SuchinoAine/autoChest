using System.Collections.Generic;
using AutoChess.Core;
using AutoChess.View;
using UnityEngine;

public class SandboxRunner : MonoBehaviour
{
    public GameObject unitPrefab;

    private BattleWorld _world = new();
    private readonly Dictionary<string, UnitView> _views = new();

    void Start()
    {
        InitWorld();
    }

    void Update()
    {
        if (_world.IsEnded) return;

        _world.ResetLogs();
        _world.Tick(Time.deltaTime);

        // apply positions to views
        foreach (var u in _world.Units)
        {
            if (_views.TryGetValue(u.Id, out var v))
                v.SetX(u.X);
        }

        // print logs (MVP: spam is ok; later we can throttle)
        foreach (var log in _world.Logs)
            Debug.Log(log.ToString());
    }

    private void InitWorld()
    {
        _world = new BattleWorld();
        _views.Clear();

        // create two units
        var u1 = new Unit("A1", Team.A, hp: 60, atk: 8, atkInterval: 0.8f, moveSpeed: 2.5f, range: 1.2f, startX: -4f);
        var u2 = new Unit("B1", Team.B, hp: 55, atk: 9, atkInterval: 1.0f, moveSpeed: 2.2f, range: 1.2f, startX:  4f);

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
        view.SetX(u.X);

        // simple color
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = u.Team == Team.A ? Color.cyan : Color.magenta;

        _views[u.Id] = view;
    }
}
