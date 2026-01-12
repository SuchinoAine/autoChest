using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace AutoChess.Core
{
    /// <summary>
    /// 把战斗事件流写成 JSONL（每行一个事件），用于回放/对比/离线分析。
    /// </summary>
    public sealed class JsonlLogSink : IBattleEventSink, IDisposable
    {
        private readonly StreamWriter _w;
        private readonly int _seed;
        private readonly float _dt;

        // 可选：过滤 Move（Move 行数最大）
        private readonly bool _logMove;

        public JsonlLogSink(string path, int seed, float dt, bool logMove = true)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            _w = new StreamWriter(path, false, Encoding.UTF8);
            _seed = seed;
            _dt = dt;
            _logMove = logMove;

            WriteMeta();
        }

        private void WriteMeta()
        {
            _w.WriteLine(
                $"{{\"kind\":\"meta\",\"seed\":{_seed},\"dt\":{F(_dt)},\"time\":\"{DateTime.Now:O}\"}}"
            );
            _w.Flush();
        }

        public void OnMove(BattleWorld w, Unit u, Vector3 from, Vector3 to)
        {
            if (!_logMove) return;
            _w.WriteLine(
                $"{{\"kind\":\"event\",\"tick\":{w.TickIndex},\"time\":{F(w.Time)},\"type\":\"Move\",\"a\":\"{E(u.Id)}\",\"b\":\"\",\"value\":0," +
                $"\"pos\":{{\"x\":{F(to.x)},\"y\":{F(to.y)},\"z\":{F(to.z)}}}}}"
            );
        }

        public void OnAttack(BattleWorld w, Unit attacker, Unit target, float damage)
        {
            var p = attacker.Position;
            _w.WriteLine(
                $"{{\"kind\":\"event\",\"tick\":{w.TickIndex},\"time\":{F(w.Time)},\"type\":\"Attack\",\"a\":\"{E(attacker.Id)}\",\"b\":\"{E(target.Id)}\",\"value\":{F(damage)}," +
                $"\"pos\":{{\"x\":{F(p.x)},\"y\":{F(p.y)},\"z\":{F(p.z)}}}}}"
            );
        }

        public void OnDeath(BattleWorld w, Unit dead, Unit killer)
        {
            var p = dead.Position;
            _w.WriteLine(
                $"{{\"kind\":\"event\",\"tick\":{w.TickIndex},\"time\":{F(w.Time)},\"type\":\"Death\",\"a\":\"{E(dead.Id)}\",\"b\":\"{E(killer != null ? killer.Id : "")}\",\"value\":0," +
                $"\"pos\":{{\"x\":{F(p.x)},\"y\":{F(p.y)},\"z\":{F(p.z)}}}}}"
            );
        }

        public void OnEnd(BattleWorld w, Team winner)
        {
            string win = winner == Team.A ? "TeamA" : "TeamB";
            _w.WriteLine(
                $"{{\"kind\":\"event\",\"tick\":{w.TickIndex},\"time\":{F(w.Time)},\"type\":\"End\",\"a\":\"{win}\",\"b\":\"\",\"value\":0," +
                $"\"pos\":{{\"x\":0,\"y\":0,\"z\":0}}}}"
            );
            _w.Flush(); // 结束时强制刷盘
        }

        public void Dispose()
        {
            try { _w.Flush(); } catch { }
            try { _w.Dispose(); } catch { }
        }

        private static string F(float v) => v.ToString("0.########", CultureInfo.InvariantCulture);
        private static string E(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
