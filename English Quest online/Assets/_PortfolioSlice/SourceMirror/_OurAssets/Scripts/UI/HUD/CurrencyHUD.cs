using System.Collections;
using EnglishKingdom.CurrencySystem;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Displays the player's current coin balance.
    /// Resolves <see cref="ICurrencyService"/> from the scene ServiceLocator and
    /// reacts to <see cref="ICurrencyService.OnBalanceChanged"/> to keep the text up-to-date.
    ///
    /// On balance change:
    ///   - Plays <see cref="_gainFeedback"/> or <see cref="_loseFeedback"/> (MMF_Player) for
    ///     the coin-icon punch scale effect (set up MMF_Scale in the Inspector).
    ///   - Spawns floating coin icons one-by-one via <see cref="_coinFlyPrefab"/> that drift
    ///     upward (gain) or downward (loss) and fade out.
    /// </summary>
    public class CurrencyHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI balanceText;

        [Header("Feedbacks")]
        [Tooltip("MMF_Player with an MMF_Scale (punch) feedback — plays when coins are gained.")]
        [SerializeField] private MMF_Player _gainFeedback;
        [Tooltip("MMF_Player with an MMF_Scale (punch) feedback — plays when coins are lost.")]
        [SerializeField] private MMF_Player _loseFeedback;

        [Header("Sounds")]
        [Tooltip("SFX played once when coins are gained.")]
        [SerializeField] private AudioClip _gainSound;
        [Tooltip("SFX played once when coins are lost.")]
        [SerializeField] private AudioClip _loseSound;

        [Header("Coin Fly Effect")]
        [Tooltip("Prefab with a CoinFlyEffect component and a coin Image.")]
        [SerializeField] private GameObject _coinFlyPrefab;
        [Tooltip("Canvas RectTransform used as the parent for spawned fly coins (e.g. the HUD root).")]
        [SerializeField] private RectTransform _flyContainer;
        [Tooltip("Maximum number of coin icons spawned per single balance change.")]
        [SerializeField] private int _maxFlyCoins = 5;
        [Tooltip("Seconds between each successive coin being spawned.")]
        [SerializeField] private float _coinSpawnInterval = 0.08f;

        private ICurrencyService _currencyService;
        private IAudioService _audioService;
        private int _previousBalance;
        private int _displayedBalance;
        private Coroutine _tickerCoroutine;
        private Coroutine _subscribeCoroutine;
        private bool _subscribed;
        private bool IsInitialized;

        private void OnEnable()
        {
            if (_subscribeCoroutine != null)
                StopCoroutine(_subscribeCoroutine);
            _subscribeCoroutine = StartCoroutine(SubscribeWhenReady());
        }

        private void OnDisable()
        {
            if (_subscribeCoroutine != null)
            {
                StopCoroutine(_subscribeCoroutine);
                _subscribeCoroutine = null;
            }

            Unsubscribe();
        }

        private IEnumerator SubscribeWhenReady()
        {
            const int maxFrames = 300;
            for (int i = 0; i < maxFrames; i++)
            {
                if (TrySubscribe())
                    yield break;

                yield return null;
            }
        }

        private bool TrySubscribe()
        {
            if (_subscribed) return true;
            if (!ServiceLocator.For(this).TryGet(out _currencyService) || !UnityLifetime.IsAlive(_currencyService))
                return false;

            ServiceLocator.For(this).TryGet(out _audioService);
            _previousBalance = _currencyService.Balance;
            _displayedBalance = _currencyService.Balance;
            _currencyService.OnBalanceChanged += UpdateDisplay;
            _subscribed = true;
            return true;
        }

        private void Subscribe()
        {
            TrySubscribe();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (UnityLifetime.IsAlive(_currencyService))
                _currencyService.OnBalanceChanged -= UpdateDisplay;

            _currencyService = null;
            _audioService = null;
            _subscribed = false;
        }

        private void UpdateDisplay(int balance)
        {
            if (!IsInitialized)
            {
                _previousBalance = balance;
                _displayedBalance = balance;
                SetBalanceText(balance);
                IsInitialized = true;
                return;
            }

            int delta = balance - _previousBalance;
            _previousBalance = balance;

            if (delta == 0)
                return;

            if (!gameObject.activeInHierarchy)
            {
                _displayedBalance = balance;
                SetBalanceText(balance);
                return;
            }

            bool isGain = delta > 0;

            if (isGain)
            {
                _gainFeedback?.PlayFeedbacks();
                if (_gainSound != null) _audioService?.PlaySFX(_gainSound);
            }
            else
            {
                _loseFeedback?.PlayFeedbacks();
                if (_loseSound != null) _audioService?.PlaySFX(_loseSound);
            }

            if (_tickerCoroutine != null)
                StopCoroutine(_tickerCoroutine);
            _tickerCoroutine = StartCoroutine(TickBalance(balance, isGain));
        }

        private IEnumerator TickBalance(int target, bool isGain)
        {
            int steps = Mathf.Abs(target - _displayedBalance);
            int flyCoinsLeft = Mathf.Min(steps, _maxFlyCoins);
            int step = isGain ? 1 : -1;
            var wait = new WaitForSeconds(_coinSpawnInterval);

            while (_displayedBalance != target)
            {
                _displayedBalance += step;
                SetBalanceText(_displayedBalance);

                if (_coinFlyPrefab != null && _flyContainer != null && flyCoinsLeft > 0)
                {
                    var go = Instantiate(_coinFlyPrefab, _flyContainer);
                    go.GetComponent<CoinFlyEffect>()?.Play(isGain);
                    flyCoinsLeft--;
                }

                yield return wait;
            }

            _tickerCoroutine = null;
        }

        private void SetBalanceText(int value)
        {
            if (balanceText != null)
                balanceText.text = value.ToString();
        }
    }
}
