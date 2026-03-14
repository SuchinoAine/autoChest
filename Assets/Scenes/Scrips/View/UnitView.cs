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

        public void SetPos(Vector3 pos)
        {
            var p = transform.position;
            p.x = pos.x;
            p.y = transform.position.y; 
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