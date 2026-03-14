using UnityEngine;
using UnityEngine.UI;
using AutoChess.Core;

namespace AutoChess.View.Hud
{
    public class UnitHud : MonoBehaviour
    {
        [Header("Auto-find Sliders (optional)")]
        public Slider hpBar;   // HP_BAR
        public Slider skBar;   // SK_BAR (默认取第0个技能)
        public Slider atkBar;  // AT_BAR (普攻CD)

        [Header("Follow")]
        public float yOffset = 1.2f;
        public bool faceCamera = true;
        public bool hideWhenDead = true;

        [Header("Skill Bar Source")]
        [Tooltip("Which skill index to display from Unit.Skills. 0 = first skill.")]
        public int skillIndex = 0;

        private Unit _unit;
        private Transform _cam;
        
        // ✅ 新增：用来控制显示的组件和身份证
        private Canvas _canvas;
        private ChessUnit _chessUnit;

        public void Bind(Unit unit)
        {
            _unit = unit;
            EnsureRefs();
            InitRanges();
            Refresh();
        }

        private void Awake()
        {
            _cam = Camera.main != null ? Camera.main.transform : null;
            
            // ✅ 获取自身的 Canvas 组件 (只关渲染，不关脚本，性能最佳)
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = GetComponentInChildren<Canvas>();

            EnsureRefs();
            InitRanges();
        }

        private void LateUpdate()
        {
            // 1. 动态获取身份证 (解决 BenchManager 在 Instantiate 后才 AddComponent 的时序问题)
            if (_chessUnit == null)
            {
                _chessUnit = GetComponentInParent<ChessUnit>();
            }

            // 2. 核心逻辑：判断当前是否应该显示血条
            bool shouldShow = true;

            // 规则A：如果有身份证，且在备战区 (!IsOnBoard)，则隐藏
            if (_chessUnit != null && !_chessUnit.IsOnBoard)
            {
                shouldShow = false;
            }
            // 规则B：如果绑定了战斗核心且已死亡，则隐藏
            else if (_unit != null && hideWhenDead && _unit.IsDead)
            {
                shouldShow = false;
            }

            // 3. 高效执行显示/隐藏切换
            if (_canvas != null && _canvas.enabled != shouldShow)
            {
                _canvas.enabled = shouldShow;
            }

            // ✅ 如果不需要显示，直接 return 结束，节省后续追踪和计算性能
            if (!shouldShow) return;

            // --- 以下是保持显示时的逻辑 ---

            // 头顶偏移
            var lp = transform.localPosition;
            lp.y = yOffset;
            transform.localPosition = lp;

            // 朝向摄像机
            if (faceCamera && _cam != null)
            {
                float camX = _cam.eulerAngles.x;
                transform.rotation = Quaternion.Euler(camX, 0f, 0f);
            }

            // 刷新血量等数据（准备阶段 _unit 为 null 时跳过，保持满血外观）
            if (_unit != null)
            {
                Refresh();
            }
        }

        private void EnsureRefs()
        {
            if (hpBar == null) hpBar = FindSlider("HP_BAR");
            if (skBar == null) skBar = FindSlider("SK_BAR");
            if (atkBar == null) atkBar = FindSlider("AT_BAR");
        }

        private Slider FindSlider(string name)
        {
            var t = FindDeepChild(transform, name);
            return t != null ? t.GetComponent<Slider>() : null;
        }

        private Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == name) return all[i];
            return null;
        }

        private void InitRanges()
        {
            if (hpBar != null) { hpBar.minValue = 0f; hpBar.maxValue = 1f; }
            if (skBar != null) { skBar.minValue = 0f; skBar.maxValue = 1f; }
            if (atkBar != null){ atkBar.minValue = 0f; atkBar.maxValue = 1f; }
        }

        private void Refresh()
        {
            if (_unit == null) return;

            if (hpBar != null)
            {
                float max = Mathf.Max(0.0001f, _unit.MaxHp);
                hpBar.value = Mathf.Clamp01(_unit.Hp / max);
            }

            if (atkBar != null)
            {
                atkBar.value = 1f - Mathf.Clamp01(_unit.AtkCdNorm);
            }

            if (skBar != null)
            {
                float v = 1f; 
                if (_unit.Skills != null && skillIndex >= 0 && skillIndex < _unit.Skills.Count)
                {
                    var sr = _unit.Skills[skillIndex];
                    float cd = (sr != null && sr.Def != null) ? sr.Def.cooldown : 0f;
                    if (cd > 0.0001f)
                        v = 1f - Mathf.Clamp01(sr.CdLeft / cd); 
                }
                skBar.value = v;
            }
        }
    }
}