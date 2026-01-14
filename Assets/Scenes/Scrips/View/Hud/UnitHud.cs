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
            EnsureRefs();
            InitRanges();
        }

        private void LateUpdate()
        {
            if (_unit == null) return;

            if (hideWhenDead && _unit.IsDead)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            // 头顶偏移（这个 UnitHud 一般挂在 visualRoot 下）
            var lp = transform.localPosition;
            lp.y = yOffset;
            transform.localPosition = lp;

        if (faceCamera && _cam != null)
        {
            // 取相机的 pitch（绕X）
            float camX = _cam.eulerAngles.x;
            // 只改 X，Y/Z 固定
            transform.rotation = Quaternion.Euler(camX, 0f, 0f);
        }

            Refresh();
        }

        private void EnsureRefs()
        {
            // 允许你不手动拖引用：按名字自动找
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
            // 全部用 [0,1] 表示 fill
            if (hpBar != null) { hpBar.minValue = 0f; hpBar.maxValue = 1f; }
            if (skBar != null) { skBar.minValue = 0f; skBar.maxValue = 1f; }
            if (atkBar != null){ atkBar.minValue = 0f; atkBar.maxValue = 1f; }
        }

        private void Refresh()
        {
            if (_unit == null) return;

            // HP
            if (hpBar != null)
            {
                float max = Mathf.Max(0.0001f, _unit.MaxHp);
                hpBar.value = Mathf.Clamp01(_unit.Hp / max);
            }

            // 普攻 CD：就绪=1，冷却中逐渐回到1
            if (atkBar != null)
            {
                atkBar.value = 1f - Mathf.Clamp01(_unit.AtkCdNorm);
            }

            // 技能 CD：默认显示 Unit.Skills[skillIndex]
            if (skBar != null)
            {
                float v = 1f; // 没技能就显示满（就绪）
                if (_unit.Skills != null && skillIndex >= 0 && skillIndex < _unit.Skills.Count)
                {
                    var sr = _unit.Skills[skillIndex];
                    float cd = (sr != null && sr.Def != null) ? sr.Def.cooldown : 0f;
                    if (cd > 0.0001f)
                        v = 1f - Mathf.Clamp01(sr.CdLeft / cd); // 0->冷却刚开始, 1->好了
                }
                skBar.value = v;
            }
        }
    }
}
