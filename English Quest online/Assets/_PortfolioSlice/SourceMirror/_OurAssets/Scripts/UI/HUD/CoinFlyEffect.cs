using System.Collections;
using UnityEngine;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Attached to a coin UI prefab spawned by <see cref="CurrencyHUD"/>.
    /// Drifts upward on coin gain or downward on coin loss while fading out, then destroys itself.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CoinFlyEffect : MonoBehaviour
    {
        [SerializeField] private float _travelDistance = 80f;
        [SerializeField] private float _duration = 0.55f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Play(bool isGain)
        {
            StartCoroutine(Animate(isGain ? 1f : -1f));
        }

        private IEnumerator Animate(float direction)
        {
            Vector3 startPos = transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * (_travelDistance * direction);
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                float t = elapsed / _duration;
                float ease = 1f - (1f - t) * (1f - t); // ease-out quad
                transform.localPosition = Vector3.Lerp(startPos, endPos, ease);
                _canvasGroup.alpha = 1f - t;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
