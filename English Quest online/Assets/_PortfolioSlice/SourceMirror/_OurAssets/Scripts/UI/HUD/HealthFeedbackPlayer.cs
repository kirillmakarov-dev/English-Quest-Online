using MoreMountains.Feedbacks;
using UnityEngine;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Plays MMF_Player feedbacks in response to <see cref="HealthComponent"/> events.
    /// Wire up hit-flash, screen-shake, controller rumble, or death effects entirely in the Inspector.
    /// Mirrors the <c>MMF_Player _jumpFeel</c> pattern used in <see cref="PlayerMovement"/>.
    /// </summary>
    public class HealthFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;

        [Header("Feedbacks")]
        [Tooltip("Plays when any hit lands — e.g. screen-shake, hit-flash, controller rumble.")]
        [SerializeField] private MMF_Player _hitFeedback;

        [Tooltip("Plays when HP reaches zero — e.g. death sound, black-out, slow-motion.")]
        [SerializeField] private MMF_Player _deathFeedback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_health == null) return;

            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDied        += HandleDied;
        }

        private void OnDisable()
        {
            if (_health == null) return;

            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDied        -= HandleDied;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleDamageTaken(float amount, DamageInfo info)
            => _hitFeedback?.PlayFeedbacks();

        private void HandleDied(DeathContext _)
            => _deathFeedback?.PlayFeedbacks();
    }
}
