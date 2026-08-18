using Fusion;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("English Quest/Portfolio Demo/Next Level Trigger")]
    public sealed class PortfolioNextLevelTrigger : MonoBehaviour
    {
        [SerializeField] private PortfolioNextLevelSequence sequence;
        [SerializeField] private PortfolioNextLevelCompletionView completionView;
        [SerializeField] private string eyebrow = "DEMO LEVEL COMPLETE";
        [SerializeField] private string finalMessage = "Well done, you complete the demo level";
        [SerializeField] [TextArea(2, 4)] private string supportMessage = "Thank you for playing English Quest Online.";
        [SerializeField] [Min(0.05f)] private float fadeDuration = 0.8f;

        private Collider triggerCollider;
        private bool roadOpen;
        private bool completed;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            SetRoadOpen(false);
        }

        public void SetRoadOpen(bool value)
        {
            roadOpen = value;
            if (triggerCollider != null)
                triggerCollider.enabled = value;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!roadOpen || completed || !TryGetLocalPlayer(other, out IPlayerLockSystem playerLocks))
                return;

            completed = true;
            playerLocks.Lock(
                this,
                PlayerLockSystem.LockType.Movement,
                PlayerLockSystem.LockType.Camera,
                PlayerLockSystem.LockType.Interaction,
                PlayerLockSystem.LockType.GameplayInput);

            ShowCompletionView();
        }

        private void ShowCompletionView()
        {
            if (completionView == null)
            {
                Debug.LogError(
                    $"[{nameof(PortfolioNextLevelTrigger)}] Scene-authored completion view is not assigned on '{name}'.",
                    this);
                return;
            }

            completionView.Show(eyebrow, finalMessage, supportMessage, fadeDuration);
        }

        private static bool TryGetLocalPlayer(Collider other, out IPlayerLockSystem playerLocks)
        {
            playerLocks = null;
            if (other == null)
                return false;

            NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !NetworkPlayerOwnership.IsLocal(networkObject))
                return false;

            PlayerLockSystem networkLocks = other.GetComponentInParent<PlayerLockSystem>();
            if (networkLocks != null)
            {
                playerLocks = networkLocks;
                return true;
            }

            PortfolioPlayerLockService portfolioLocks = other.GetComponentInParent<PortfolioPlayerLockService>();
            if (portfolioLocks != null)
            {
                playerLocks = portfolioLocks;
                return true;
            }

            return false;
        }
    }
}
