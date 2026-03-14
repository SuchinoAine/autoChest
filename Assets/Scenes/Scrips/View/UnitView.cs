using AutoChess.Core;
using UnityEngine;
using System.Collections;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

        private Coroutine _deathCo;
        
        // ✅ 新增：引用描边组件
        private Outline _outline;

        private void Awake()
        {
            // 尝试获取或添加 Outline 组件 (稍后我们会把这个脚本导入项目)
            _outline = GetComponent<Outline>();
            if (_outline == null) _outline = gameObject.AddComponent<Outline>();
            
            // 初始状态关闭描边
            _outline.enabled = false;
        }

        public void SetPos(Vector3 pos)
        {
            var p = transform.position;
            p.x = pos.x;
            p.y = transform.position.y; 
            p.z = pos.z;
            transform.position = p;
        }

        // ✅ 核心重构：星级改变时，只在边缘“描边”发光
        public void SetStarVisuals(int starLevel)
        {
            if (_outline == null) return;

            if (starLevel <= 1)
            {
                _outline.enabled = false;
                return;
            }

            _outline.enabled = true;
            
            // 设置描边模式：只显示外发光描边
            _outline.OutlineMode = Outline.Mode.OutlineAll;
            // 设置描边宽度
            _outline.OutlineWidth = 5f; 

            // 2星蓝边，3星金边
            if (starLevel == 2)
            {
                _outline.OutlineColor = new Color(0.2f, 0.6f, 1f); 
            }
            else if (starLevel >= 3)
            {
                _outline.OutlineColor = new Color(1f, 0.8f, 0.1f);
            }
        }

        public void PlayDeathFade(float duration = 0.25f)
        {
            if (_deathCo != null) StopCoroutine(_deathCo);
            _deathCo = StartCoroutine(DeathFadeCo(duration));
        }

        private IEnumerator DeathFadeCo(float duration)
        {
            float t = 0f;
            Vector3 start = transform.localScale;
            Vector3 end = Vector3.zero;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.Lerp(start, end, k);
                yield return null;
            }

            transform.localScale = end;
            gameObject.SetActive(false);
        }
    }
}