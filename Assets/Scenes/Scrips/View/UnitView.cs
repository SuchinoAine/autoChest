using AutoChess.Core;
using UnityEngine;
using System.Collections;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

        [Header("Model")]
        public Transform modelRoot;  // 模型/特效挂在这里
        public Renderer[] cachedRenderers;

        [Header("Visual")]
        public bool autoColorByTeam = true;
        public Color teamAColor = Color.cyan;
        public Color teamBColor = Color.magenta;

        // 动画效果
        private Coroutine _deathCo;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialScale = transform.localScale;

            if (modelRoot == null)
            {
                // 尝试找子节点 ModelRoot
                var mr = transform.Find("ModelRoot");
                modelRoot = (mr != null) ? mr : transform;
            }

            CacheRenderers();
            if (autoColorByTeam) ApplyTeamColor();
        }

        // ---------------- 原有接口：保持不变 ----------------

        public void SetPos(Vector3 pos)
        {
            var p = transform.position;
            p.x = pos.x;
            p.y = 0f;
            p.z = pos.z;
            transform.position = p;
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
                // ✅ 就算你以后暂停 timeScale 也能播完
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.Lerp(start, end, k);
                yield return null;
            }

            transform.localScale = end;
            gameObject.SetActive(false);
        }

        public void ResetView()
        {
            if (_deathCo != null) StopCoroutine(_deathCo);
            _deathCo = null;
            transform.localScale = _initialScale;
            gameObject.SetActive(true);
        }

        // ---------------- 新增：模型系统 ----------------

        public void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void SetModel(GameObject modelPrefab)
        {
            if (modelRoot == null) modelRoot = transform;

            // 清空旧模型（只删 modelRoot 下的）
            for (int i = modelRoot.childCount - 1; i >= 0; i--)
            {
                var child = modelRoot.GetChild(i);
                Destroy(child.gameObject);
            }

            if (modelPrefab != null)
            {
                var inst = Instantiate(modelPrefab, modelRoot);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                AdjustModelHeight(inst);
            }

            CacheRenderers();
            if (autoColorByTeam) ApplyTeamColor();
        }
        
        private void AdjustModelHeight(GameObject modelInstance)
        {
            // 找所有 Renderer，计算整体 bounds
            var renderers = modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            float halfHeight = b.size.y * 0.5f;

            // 把 ModelRoot 向上抬 halfHeight
            modelRoot.localPosition = new Vector3(0f, halfHeight, 0f);
        }

        public void ApplyTeamColor()
        {
            if (cachedRenderers == null) return;

            var c = (team == Team.A) ? teamAColor : teamBColor;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                var r = cachedRenderers[i];
                if (r == null) continue;
                if (r.material != null && r.material.HasProperty("_Color"))
                    r.material.color = c;
            }
        }
    }
}
