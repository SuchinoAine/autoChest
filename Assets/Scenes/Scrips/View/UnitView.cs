using AutoChess.Core;
using UnityEngine;
using System.Collections;

namespace AutoChess.View
{
    public class UnitView : MonoBehaviour
    {
        public string unitId;
        public Team team;

        // 动画效果
        private Coroutine _deathCo;
        private Vector3 _initialScale;

        public void SetPos(Vector3 pos)
        {
            var p = transform.position;
            p.x = pos.x;
            p.y = 0f;
            p.z = pos.z;
            transform.position = p;
        }

        private void Awake()
        {
            _initialScale = transform.localScale;
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
                t += Time.unscaledDeltaTime; // ✅ 就算你以后暂停 timeScale 也能播完
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.Lerp(start, end, k);
                yield return null;
            }

            transform.localScale = end;
            gameObject.SetActive(false);   // ✅ 暂时直接隐藏，后期你再做对象池/尸体
        }

        // 如果你以后要复用（Reset/复活）：
        public void ResetView()
        {
            if (_deathCo != null) StopCoroutine(_deathCo);
            _deathCo = null;
            transform.localScale = _initialScale;
            gameObject.SetActive(true);
        }
    }
}