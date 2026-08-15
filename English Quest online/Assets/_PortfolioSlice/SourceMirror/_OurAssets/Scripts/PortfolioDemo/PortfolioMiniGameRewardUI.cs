using System;
using System.Collections;
using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using TMPro;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Mini Game Reward UI")]
    public sealed class PortfolioMiniGameRewardUI : MonoBehaviour
    {
        [Serializable]
        private struct GameRewardOverride
        {
            [SerializeField] private string gameId;
            [SerializeField] [Min(0)] private int coins;

            public string GameId => gameId;
            public int Coins => coins;
        }

        [Header("Scene References")]
        [SerializeField] private QuestObjectiveEventBus objectiveEventBus;
        [SerializeField] private RectTransform presentationRoot;
        [SerializeField] private CanvasGroup presentationCanvasGroup;
        [SerializeField] private RectTransform animatedCoin;
        [SerializeField] private TextMeshProUGUI rewardAmountText;
        [SerializeField] private RectTransform counterRoot;
        [SerializeField] private RectTransform counterCoinTarget;
        [SerializeField] private TextMeshProUGUI counterText;

        [Header("Reward Values")]
        [SerializeField] [Min(0)] private int startingCoins;
        [SerializeField] [Min(0)] private int defaultMiniGameReward = 1;
        [SerializeField] private List<GameRewardOverride> gameRewardOverrides = new();
        [SerializeField] private string counterFormat = "{0}";
        [SerializeField] private string rewardFormat = "+{0}";

        [Header("Animation")]
        [SerializeField] [Min(0.05f)] private float popDuration = 0.35f;
        [SerializeField] [Min(0f)] private float celebrationHold = 0.45f;
        [SerializeField] [Min(0.05f)] private float flyDuration = 0.75f;
        [SerializeField] [Min(0.05f)] private float counterBumpDuration = 0.2f;
        [SerializeField] private float spinDegrees = 540f;
        [SerializeField] [Min(1f)] private float popScale = 1.15f;
        [SerializeField] [Range(0.1f, 1f)] private float arrivalScale = 0.3f;

        private readonly Queue<int> pendingRewards = new();
        private IQuestObjectiveEventBus subscribedBus;
        private Coroutine rewardRoutine;
        private Vector2 presentationStartPosition;
        private Vector3 counterStartScale;
        private int displayedCoins;

        private void Awake()
        {
            displayedCoins = Mathf.Max(0, startingCoins);
            presentationStartPosition = presentationRoot != null
                ? presentationRoot.anchoredPosition
                : Vector2.zero;
            counterStartScale = counterRoot != null ? counterRoot.localScale : Vector3.one;

            SetPresentationVisible(false);
            RefreshCounter();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Update()
        {
            if (subscribedBus == null)
                TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (rewardRoutine != null)
                StopCoroutine(rewardRoutine);

            rewardRoutine = null;
            pendingRewards.Clear();
            SetPresentationVisible(false);
        }

        [ContextMenu("Preview One Coin Reward")]
        public void PreviewReward()
        {
            if (!Application.isPlaying)
            {
                AppLog.Warning("[PortfolioMiniGameRewardUI] Reward preview is available in Play Mode.", this);
                return;
            }

            EnqueueReward(defaultMiniGameReward);
        }

        private void TrySubscribe()
        {
            IQuestObjectiveEventBus candidate = objectiveEventBus;
            if (candidate == null)
                ServiceLocator.For(this)?.TryGet(out candidate);

            if (candidate == null || ReferenceEquals(candidate, subscribedBus))
                return;

            Unsubscribe();
            subscribedBus = candidate;
            subscribedBus.OnMiniGameCompleted += HandleMiniGameCompleted;
        }

        private void Unsubscribe()
        {
            if (subscribedBus != null)
                subscribedBus.OnMiniGameCompleted -= HandleMiniGameCompleted;

            subscribedBus = null;
        }

        private void HandleMiniGameCompleted(QuestObjectiveEvents.MiniGameCompleted completed)
        {
            EnqueueReward(ResolveReward(completed.GameId));
        }

        private int ResolveReward(string gameId)
        {
            for (int i = 0; i < gameRewardOverrides.Count; i++)
            {
                GameRewardOverride rewardOverride = gameRewardOverrides[i];
                if (string.Equals(rewardOverride.GameId, gameId, StringComparison.Ordinal))
                    return rewardOverride.Coins;
            }

            return defaultMiniGameReward;
        }

        private void EnqueueReward(int amount)
        {
            if (amount <= 0)
                return;

            pendingRewards.Enqueue(amount);
            if (rewardRoutine == null && isActiveAndEnabled)
                rewardRoutine = StartCoroutine(PlayPendingRewards());
        }

        private IEnumerator PlayPendingRewards()
        {
            while (pendingRewards.Count > 0)
                yield return PlayReward(pendingRewards.Dequeue());

            rewardRoutine = null;
        }

        private IEnumerator PlayReward(int amount)
        {
            if (presentationRoot == null
                || presentationCanvasGroup == null
                || animatedCoin == null
                || counterCoinTarget == null)
            {
                displayedCoins += amount;
                RefreshCounter();
                yield break;
            }

            presentationRoot.anchoredPosition = presentationStartPosition;
            presentationRoot.localScale = Vector3.zero;
            presentationRoot.localRotation = Quaternion.identity;
            animatedCoin.localRotation = Quaternion.identity;
            if (rewardAmountText != null)
                rewardAmountText.text = FormatValue(rewardFormat, amount);

            SetPresentationVisible(true);
            yield return AnimatePop();

            if (celebrationHold > 0f)
                yield return new WaitForSecondsRealtime(celebrationHold);

            yield return AnimateFlightToCounter();

            displayedCoins += amount;
            RefreshCounter();
            SetPresentationVisible(false);
            yield return AnimateCounterBump();
        }

        private IEnumerator AnimatePop()
        {
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float eased = EaseOutBack(t);
                presentationCanvasGroup.alpha = t;
                presentationRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0f, popScale, eased);
                animatedCoin.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 45f, t));
                yield return null;
            }

            presentationRoot.localScale = Vector3.one;
        }

        private IEnumerator AnimateFlightToCounter()
        {
            Vector2 start = presentationRoot.anchoredPosition;
            Vector2 target = GetTargetPositionInPresentationParent();
            float elapsed = 0f;

            while (elapsed < flyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float eased = t * t * (3f - 2f * t);
                float arc = Mathf.Sin(t * Mathf.PI) * 90f;
                Vector2 position = Vector2.LerpUnclamped(start, target, eased);
                position.y += arc;

                presentationRoot.anchoredPosition = position;
                presentationRoot.localScale = Vector3.one * Mathf.Lerp(1f, arrivalScale, eased);
                presentationRoot.localRotation = Quaternion.Euler(0f, 0f, spinDegrees * eased);
                presentationCanvasGroup.alpha = 1f - Mathf.Clamp01((t - 0.82f) / 0.18f);
                yield return null;
            }
        }

        private IEnumerator AnimateCounterBump()
        {
            if (counterRoot == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < counterBumpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / counterBumpDuration);
                float pulse = Mathf.Sin(t * Mathf.PI) * 0.18f;
                counterRoot.localScale = counterStartScale * (1f + pulse);
                yield return null;
            }

            counterRoot.localScale = counterStartScale;
        }

        private Vector2 GetTargetPositionInPresentationParent()
        {
            if (presentationRoot.parent is not RectTransform parent)
                return presentationStartPosition;

            return parent.InverseTransformPoint(counterCoinTarget.position);
        }

        private void SetPresentationVisible(bool visible)
        {
            if (presentationCanvasGroup == null)
                return;

            presentationCanvasGroup.alpha = visible ? 1f : 0f;
            presentationCanvasGroup.interactable = false;
            presentationCanvasGroup.blocksRaycasts = false;
        }

        private void RefreshCounter()
        {
            if (counterText != null)
                counterText.text = FormatValue(counterFormat, displayedCoins);
        }

        private static string FormatValue(string format, int value)
        {
            if (string.IsNullOrEmpty(format))
                return value.ToString();

            try
            {
                return string.Format(format, value);
            }
            catch (FormatException)
            {
                return value.ToString();
            }
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
        }
    }
}
