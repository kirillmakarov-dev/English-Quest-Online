using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Drives a UI health bar by listening to <see cref="HealthComponent"/> events.
    /// Requires no Fusion knowledge — works for both local and networked entities.
    /// Assign the target <see cref="HealthComponent"/> in the Inspector, or let
    /// <see cref="PlayerHealthHUD"/> bind the local player at runtime.
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        public enum BarFillMode
        {
            Slider,
            ImageWidth
        }

        [SerializeField] private HealthComponent _health;

        [Header("Bar")]
        [SerializeField] private BarFillMode _fillMode = BarFillMode.Slider;

        [Tooltip("Slider whose normalized value (0-1) represents current HP ratio.")]
        [SerializeField] private Slider _slider;

        [Tooltip("Fill rect resized horizontally (SoftKitty PlayerBar style).")]
        [SerializeField] private RectTransform _fillRect;

        [SerializeField] private float _fillMaxWidth = 390f;
        [SerializeField] private float _fillHeight = 40f;

        [Tooltip("Smooth fill speed in units/second. Set to 0 for instant updates.")]
        [SerializeField] [Min(0f)] private float _smoothSpeed = 8f;

        [Header("Label")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Text _legacyHpText;
        [SerializeField] private bool _showNumericText = true;

        private float _targetFill;
        private float _displayFill;
        private bool _subscribed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_health != null)
                Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_smoothSpeed <= 0f) return;

            _displayFill = Mathf.MoveTowards(_displayFill, _targetFill, _smoothSpeed * Time.deltaTime);
            ApplyFillImmediate(_displayFill);
        }

        // ── Binding ───────────────────────────────────────────────────────────

        public void Bind(HealthComponent health)
        {
            Unsubscribe();
            _health = health;
            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();
            _health = null;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void Subscribe()
        {
            if (_health == null || _subscribed) return;

            _health.OnHealthChanged += HandleHealthChanged;
            _subscribed = true;

            _targetFill = _health.NormalizedHP;
            _displayFill = _targetFill;
            ApplyFillImmediate(_displayFill);
            UpdateLabel(_health.CurrentHP, _health.MaxHP);
        }

        private void Unsubscribe()
        {
            if (_health == null || !_subscribed) return;

            _health.OnHealthChanged -= HandleHealthChanged;
            _subscribed = false;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _targetFill = max > 0f ? current / max : 0f;

            if (_smoothSpeed <= 0f)
            {
                _displayFill = _targetFill;
                ApplyFillImmediate(_displayFill);
            }

            UpdateLabel(current, max);
        }

        private void ApplyFillImmediate(float fill)
        {
            switch (_fillMode)
            {
                case BarFillMode.Slider:
                    if (_slider != null)
                        _slider.value = fill;
                    break;

                case BarFillMode.ImageWidth:
                    if (_fillRect != null)
                        _fillRect.sizeDelta = new Vector2(_fillMaxWidth * fill, _fillHeight);
                    break;
            }
        }

        private void UpdateLabel(float current, float max)
        {
            if (!_showNumericText) return;

            string label = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

            if (_hpText != null)
                _hpText.text = label;
            else if (_legacyHpText != null)
                _legacyHpText.text = label;
        }
    }
}
