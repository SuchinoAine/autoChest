using AutoChess.Core;
using UnityEngine;
using System.Collections;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

        [Header("Roots")]
        [Tooltip("Only used for visual scaling by Unit.Radius. Root transform should stay at scale=1.")]
        public Transform visualRoot;

        [Tooltip("Models / VFX are parented here. Keep scale=1; localPosition used for grounding offset.")]
        public Transform modelRoot;

        [Header("Model")]
        public Renderer[] cachedRenderers;

        [Header("Visual")]
        public bool autoColorByTeam = true;
        public Color teamAColor = Color.cyan;
        public Color teamBColor = Color.magenta;

        private Coroutine _deathCo;

        private void Awake()
        {
            EnsureRoots();
            CacheRenderers();
            if (autoColorByTeam) ApplyTeamColor();
        }

        // ---------------- Core -> View mapping ----------------

        public void SetPos(Vector3 pos)
        {
            // Root only follows core position (Y locked to 0 for board).
            var p = transform.position;
            p.x = pos.x;
            p.y = 0f;
            p.z = pos.z;
            transform.position = p;
        }

        /// <summary>
        /// Visual diameter is driven by Unit.Radius; never use Transform scale for gameplay.
        /// </summary>
        public void ApplyRadius(float radius)
        {
            EnsureRoots();

            // Enforce scales: root and model are fixed; only visualRoot scales.
            transform.localScale = Vector3.one;
            if (modelRoot != null) modelRoot.localScale = Vector3.one;

            float diameter = (radius > 0f) ? radius * 2f : 1f;
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * diameter;
        }

        public void PlayDeathFade(float duration = 0.25f)
        {
            if (_deathCo != null) StopCoroutine(_deathCo);
            _deathCo = StartCoroutine(DeathFadeCo(duration));
        }

        private IEnumerator DeathFadeCo(float duration)
        {
            float t = 0f;

            // Fade visual only; keep root for positioning / logic anchors.
            Transform fadeRoot = (visualRoot != null) ? visualRoot : transform;

            Vector3 start = fadeRoot.localScale;
            Vector3 end = Vector3.zero;

            while (t < duration)
            {
                // keep playing even if timeScale changes
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                fadeRoot.localScale = Vector3.Lerp(start, end, k);
                yield return null;
            }

            fadeRoot.localScale = end;
            gameObject.SetActive(false);
        }

        // ---------------- Model system ----------------

        public void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void SetModel(GameObject modelPrefab)
        {
            EnsureRoots();

            // Reset grounding offset so it won't accumulate across model swaps.
            if (modelRoot != null) modelRoot.localPosition = Vector3.zero;

            // Clear old model (only delete children under modelRoot)
            if (modelRoot != null)
            {
                for (int i = modelRoot.childCount - 1; i >= 0; i--)
                {
                    var child = modelRoot.GetChild(i);
                    Destroy(child.gameObject);
                }
            }

            if (modelPrefab != null && modelRoot != null)
            {
                var inst = Instantiate(modelPrefab, modelRoot);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;

                GroundModelToBoard(inst);
            }

            CacheRenderers();
            if (autoColorByTeam) ApplyTeamColor();
        }

        /// <summary>
        /// Align model so that its lowest point sits on board (y = root.y).
        /// Uses world-space bounds (already includes scaling), then converts delta into modelRoot local-space offset.
        /// </summary>
        public void RegroundCurrentModel()
        {
            EnsureRoots();
            if (modelRoot == null) return;
            // Find first child as model instance.
            if (modelRoot.childCount == 0) return;
            GroundModelToBoard(modelRoot.GetChild(0).gameObject);
        }

        private void GroundModelToBoard(GameObject modelInstance)
        {
            if (modelInstance == null || modelRoot == null) return;

            var renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            float worldMinY = b.min.y;
            float groundY = transform.position.y; // board plane

            float deltaWorld = groundY - worldMinY;

            // Convert world-space vertical delta into modelRoot local-space y offset.
            float parentScaleY = (modelRoot.parent != null) ? modelRoot.parent.lossyScale.y : 1f;
            if (Mathf.Abs(parentScaleY) < 1e-5f) parentScaleY = 1f;

            float deltaLocalY = deltaWorld / parentScaleY;

            var lp = modelRoot.localPosition;
            lp.y += deltaLocalY;
            modelRoot.localPosition = lp;
        }

        private void EnsureRoots()
        {
            // Root should remain scale 1
            transform.localScale = Vector3.one;

            // Find existing roots anywhere under this view (not only direct children).
            if (visualRoot == null)
                visualRoot = FindChildByName(transform, "VisualRoot");

            if (modelRoot == null)
                modelRoot = FindChildByName(transform, "ModelRoot");

            // Create if missing.
            if (visualRoot == null)
            {
                var go = new GameObject("VisualRoot");
                go.transform.SetParent(transform, false);
                visualRoot = go.transform;
            }

            if (modelRoot == null)
            {
                var go = new GameObject("ModelRoot");
                go.transform.SetParent(visualRoot, false);
                modelRoot = go.transform;
            }

            // Ensure hierarchy: ModelRoot under VisualRoot (visual scaling),
            // but keep ModelRoot local scale = 1.
            if (modelRoot.parent != visualRoot) modelRoot.SetParent(visualRoot, false);

            modelRoot.localScale = Vector3.one;
            // Force centering
            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
            }
            if (modelRoot != null)
            {
                modelRoot.localPosition = Vector3.zero;
                modelRoot.localRotation = Quaternion.identity;
            }
        }

        private Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            // Includes inactive.
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i];
            }
            return null;
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
