using System.Collections;
using EnglishQuest.QuestSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Next Level Sequence")]
    public sealed class PortfolioNextLevelSequence : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform barriersRoot;
        [SerializeField] private CinemachineCamera revealCamera;
        [SerializeField] private PortfolioNextLevelTrigger exitTrigger;
        [SerializeField] private PortfolioDemoHud hud;

        [Header("Reveal Timing")]
        [SerializeField] private int revealCameraPriority = 50;
        [SerializeField] [Min(0f)] private float cameraLeadIn = 0.8f;
        [SerializeField] [Min(0.05f)] private float barrierDisappearDuration = 0.35f;
        [SerializeField] [Min(0f)] private float delayBetweenBarriers = 0.08f;
        [SerializeField] [Min(0f)] private float cameraHoldAfterReveal = 1.25f;
        [SerializeField] [Min(0f)] private float sinkDistance = 0.75f;

        private IQuestService questService;
        private IPlayerLockSystem playerLocks;
        private Coroutine revealRoutine;
        private bool subscribed;

        public bool IsRoadOpen { get; private set; }

        private void Awake()
        {
            barriersRoot ??= transform;
            hud ??= FindFirstObjectByType<PortfolioDemoHud>(FindObjectsInactive.Include);
            hud?.UseWorldExitCompletionFlow();
            exitTrigger?.SetRoadOpen(false);

            DeactivateRevealCamera();
        }

        private void OnEnable()
        {
            TryResolveServices();
        }

        private void Update()
        {
            if (!subscribed)
                TryResolveServices();
        }

        private void OnDisable()
        {
            if (subscribed && questService != null)
                questService.OnLevelCompleted -= BeginReveal;

            subscribed = false;
            DeactivateRevealCamera();
            ReleasePlayerLocks();
        }

        private void TryResolveServices()
        {
            if (questService == null)
            {
                ServiceLocator.For(this)?.TryGet(out questService);
                if (questService == null && QuestManager.HasInstance)
                    questService = QuestManager.Instance;
            }

            if (playerLocks == null)
                ServiceLocator.For(this)?.TryGet(out playerLocks);

            if (questService == null || subscribed)
                return;

            questService.OnLevelCompleted += BeginReveal;
            subscribed = true;

            if (questService.IsLevelCompleted)
                BeginReveal();
        }

        private void BeginReveal()
        {
            if (revealRoutine != null || IsRoadOpen)
                return;

            revealRoutine = StartCoroutine(RevealRoadRoutine());
        }

        private IEnumerator RevealRoadRoutine()
        {
            AcquirePlayerLocks();
            hud?.UseWorldExitCompletionFlow();

            if (revealCamera != null)
            {
                revealCamera.enabled = true;
                revealCamera.Priority = revealCameraPriority;
            }

            yield return new WaitForSecondsRealtime(cameraLeadIn);

            Transform[] barriers = GetBarrierChildren();
            for (int i = 0; i < barriers.Length; i++)
            {
                yield return AnimateBarrierDisappear(barriers[i]);
                if (delayBetweenBarriers > 0f)
                    yield return new WaitForSecondsRealtime(delayBetweenBarriers);
            }

            IsRoadOpen = true;
            exitTrigger?.SetRoadOpen(true);
            yield return new WaitForSecondsRealtime(cameraHoldAfterReveal);

            DeactivateRevealCamera();

            ReleasePlayerLocks();
            revealRoutine = null;
        }

        private void DeactivateRevealCamera()
        {
            if (revealCamera == null)
                return;

            revealCamera.Priority = PlayerSceneCamera.InactivePriority;
            revealCamera.enabled = false;
        }

        private Transform[] GetBarrierChildren()
        {
            Transform root = barriersRoot != null ? barriersRoot : transform;
            Transform[] children = new Transform[root.childCount];
            for (int i = 0; i < root.childCount; i++)
                children[i] = root.GetChild(i);

            return children;
        }

        private IEnumerator AnimateBarrierDisappear(Transform barrier)
        {
            if (barrier == null || !barrier.gameObject.activeSelf)
                yield break;

            Vector3 startScale = barrier.localScale;
            Vector3 startPosition = barrier.localPosition;
            Vector3 endPosition = startPosition + Vector3.down * sinkDistance;
            float elapsed = 0f;

            while (elapsed < barrierDisappearDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / barrierDisappearDuration);
                float eased = t * t * (3f - 2f * t);
                barrier.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, eased);
                barrier.localPosition = Vector3.LerpUnclamped(startPosition, endPosition, eased);
                yield return null;
            }

            barrier.gameObject.SetActive(false);
        }

        private void AcquirePlayerLocks()
        {
            if (playerLocks == null)
                ServiceLocator.For(this)?.TryGet(out playerLocks);

            playerLocks?.Lock(
                this,
                PlayerLockSystem.LockType.Movement,
                PlayerLockSystem.LockType.Camera,
                PlayerLockSystem.LockType.Interaction,
                PlayerLockSystem.LockType.GameplayInput);
        }

        private void ReleasePlayerLocks()
        {
            playerLocks?.Unlock(
                this,
                PlayerLockSystem.LockType.Movement,
                PlayerLockSystem.LockType.Camera,
                PlayerLockSystem.LockType.Interaction,
                PlayerLockSystem.LockType.GameplayInput);
        }
    }
}
