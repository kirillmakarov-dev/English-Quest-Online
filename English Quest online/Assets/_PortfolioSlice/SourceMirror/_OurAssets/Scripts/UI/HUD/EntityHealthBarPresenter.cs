using UnityEngine;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Shows a world-space <see cref="HealthBarView"/> when the entity takes damage.
    /// Hides the bar on death. Works for local and networked entities via
    /// <see cref="HealthComponent"/> events.
    /// </summary>
    public class EntityHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private HealthBarView _healthBarView;
        [SerializeField] private CanvasGroup _canvasGroup;

        private bool _subscribed;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponentInParent<HealthComponent>();

            if (_healthBarView == null)
                _healthBarView = GetComponent<HealthBarView>();

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _healthBarView?.Bind(_health);
            SetBarVisible(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_health == null || _subscribed) return;

            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDied += HandleDied;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_health == null || !_subscribed) return;

            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDied -= HandleDied;
            _subscribed = false;
        }

        private void HandleDamageTaken(float amount, DamageInfo info)
        {
            SetBarVisible(true);
        }

        private void HandleDied(DeathContext _)
        {
            SetBarVisible(false);
        }

        private void SetBarVisible(bool visible)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
