// using System;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using AutoChess.Configs;
// using AutoChess.Core;
// using AutoChess.View;

// public class BattleReplayRunner : MonoBehaviour
// {
//     [Header("Replay Input")]
//     public string jsonlFilePath;                 // 支持绝对路径 or 相对路径
//     public BattleScenarioConfig scenario;        // 可选：优先使用 scenario
//     public List<SpawnEntry> spawns = new();      // 或直接用 spawns
//     public UnitFactory UnitFactory;

//     [Header("Playback")]
//     public bool playOnStart = true;
//     public float speed = 1f;
//     public bool paused = false;

//     [Header("UI")]
//     public bool showDevUI = true;

//     private readonly Dictionary<string, UnitView> _views = new();
//     private readonly List<ReplayEvent> _events = new();
//     private int _eventIndex = 0;

//     private float _playheadTime = 0f;            // 当前播放到的战斗时间（秒）
//     private float _endTime = 0f;
//     private string _endWinner = "";

//     void Start()
//     {
//         Application.targetFrameRate = 100;
//         QualitySettings.vSyncCount = 0;
//         InitViewsFromSpawns();
//         LoadJsonl(jsonlFilePath);

//         if (playOnStart) paused = false;
//     }

//     void Update()
//     {
//         if (paused) return;
//         if (_eventIndex >= _events.Count) return;

//         _playheadTime += Time.deltaTime * Mathf.Max(0f, speed);

//         // 处理所有 time <= playhead 的事件（顺序执行）
//         while (_eventIndex < _events.Count && _events[_eventIndex].time <= _playheadTime + 1e-6f)
//         {
//             Apply(_events[_eventIndex]);
//             _eventIndex++;
//         }
//     }

//     private void InitViewsFromSpawns()
//     {
//         _views.Clear();

//         var list = (scenario != null) ? scenario.spawns : spawns;
//         if (list == null || list.Count == 0)
//         {
//             Debug.LogWarning("[Replay] No spawns/scenario configured. Views will be spawned lazily on first Move/Attack.");
//             return;
//         }

//         int idxA = 0, idxB = 0;
//         foreach (var s in list)
//         {
//             if (s == null || s.config == null) continue;

//             string id = (s.team == Team.A) ? $"A{++idxA}" : $"B{++idxB}";

//             SpawnView(id, s.team, s.startPos, s.config.radius);
//         }
//     }

//         private void SpawnView(string id, Team team, Vector3 pos, float radius)
//         {
//             if (_views.ContainsKey(id)) return;

//             if (UnitFactory == null)
//             {
//                 Debug.LogError("[BattleReplayRunner] UnitFactory is null!");
//                 return;
//             }

//             float diameter = radius > 0f ? radius * 2f : 0.5f;

//             var view = UnitFactory.CreateU(id, team, pos, radius);
//             view.transform.localScale = Vector3.one * diameter;

//             _views[id] = view;
//         }

//     private void LoadJsonl(string path)
//     {
//         _events.Clear();
//         _eventIndex = 0;
//         _playheadTime = 0f;
//         _endWinner = "";
//         _endTime = 0f;

//         if (string.IsNullOrWhiteSpace(path))
//         {
//             Debug.LogError("[Replay] jsonlFilePath is empty.");
//             return;
//         }

//         // 支持相对路径：以 persistentDataPath 为根（你日志通常也在那）
//         if (!Path.IsPathRooted(path))
//             path = Path.Combine(Application.dataPath, path);

//         if (!File.Exists(path))
//         {
//             Debug.LogError($"[Replay] File not found: {path}");
//             return;
//         }

//         foreach (var line in File.ReadLines(path))
//         {
//             if (string.IsNullOrWhiteSpace(line)) continue;

//             // 解析 meta/event 行
//             ReplayLine rl;
//             try
//             {
//                 rl = JsonUtility.FromJson<ReplayLine>(line);
//             }
//             catch
//             {
//                 // 如果某行坏了，跳过（避免回放崩）
//                 continue;
//             }

//             if (rl == null || rl.kind != "event") continue;

//             var ev = new ReplayEvent
//             {
//                 tick = rl.tick,
//                 time = rl.time,
//                 type = rl.type,
//                 a = rl.a ?? "",
//                 b = rl.b ?? "",
//                 value = rl.value,
//                 pos = (rl.pos != null) ? new Vector3(rl.pos.x, rl.pos.y, rl.pos.z) : Vector3.zero
//             };

//             _events.Add(ev);

//             if (ev.type == "End")
//             {
//                 _endTime = ev.time;
//                 _endWinner = ev.a;
//             }
//         }

//         // 排序：先 time 再 tick（避免同一时刻乱序）
//         _events.Sort((x, y) =>
//         {
//             int c = x.time.CompareTo(y.time);
//             if (c != 0) return c;
//             return x.tick.CompareTo(y.tick);
//         });

//         if (_events.Count > 0)
//             Debug.Log($"[Replay] Loaded {_events.Count} events from {path}");
//         else
//             Debug.LogWarning($"[Replay] No events loaded from {path}");
//     }

//     private void Apply(ReplayEvent ev)
//     {
//         switch (ev.type)
//         {
//             case "Move":
//             {
//                 EnsureViewExists(ev.a, ev.pos);
//                 if (_views.TryGetValue(ev.a, out var v))
//                     v.SetPos(ev.pos);
//                 break;
//             }
//             case "Attack":
//             {
//                 // 可选：受击/闪烁/飘字（先不做也行）
//                 EnsureViewExists(ev.a, ev.pos);
//                 break;
//             }
//             case "Death":
//             {
//                 if (_views.TryGetValue(ev.a, out var v))
//                 {
//                     v.PlayDeathFade(0.25f);
//                     _views.Remove(ev.a);
//                 }
//                 break;
//             }
//             case "End":
//             {
//                 paused = true;
//                 Debug.Log($"[Replay] End. Winner: {ev.a}  at t={ev.time:F2}s");
//                 break;
//             }
//         }
//     }

//     // 如果你没配 spawns/scenario，则按事件懒生成 view（默认 team 用 Id 前缀）
//     private void EnsureViewExists(string id, Vector3 pos)
//     {
//         if (string.IsNullOrEmpty(id)) return;
//         if (_views.ContainsKey(id)) return;

//         Team team = (id.Length > 0 && id[0] == 'A') ? Team.A : Team.B;
//         SpawnView(id, team, pos, radius: 0.25f);
//     }

//     private void OnGUI()
//     {
//         if (!showDevUI) return;

//         const int w = 160, h = 36, pad = 12;
//         float x = pad, y = pad;

//         if (GUI.Button(new Rect(x, y, w, h), paused ? "Play" : "Pause"))
//             paused = !paused;
//         y += h + 6;

//         if (GUI.Button(new Rect(x, y, w, h), "Restart"))
//         {
//             // 重新生成 view（如果你想严格复位，可以 Destroy 旧对象再生成）
//             foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value.gameObject);
//             _views.Clear();

//             InitViewsFromSpawns();
//             _eventIndex = 0;
//             _playheadTime = 0f;
//             paused = false;
//         }
//         y += h + 6;

//         GUI.Label(new Rect(x, y, w * 2, h), $"t={_playheadTime:F2}s  idx={_eventIndex}/{_events.Count}");
//         y += h + 6;

//         GUI.Label(new Rect(x, y, w * 2, h), $"speed={speed:F2}  end={_endTime:F2}s winner={_endWinner}");
//     }

//     // --- JSONL line schema ---
//     [Serializable]
//     private class ReplayLine
//     {
//         public string kind;
//         public int tick;
//         public float time;
//         public string type;
//         public string a;
//         public string b;
//         public float value;
//         public ReplayPos pos;
//     }

//     [Serializable]
//     private class ReplayPos
//     {
//         public float x, y, z;
//     }

//     private struct ReplayEvent
//     {
//         public int tick;
//         public float time;
//         public string type;
//         public string a, b;
//         public float value;
//         public Vector3 pos;
//     }
// }
