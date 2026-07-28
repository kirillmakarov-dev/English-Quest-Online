using System.Text;
using EnglishQuest.QuestSystem;
using Fusion;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EnglishQuest.PortfolioDemo
{
    public sealed class PortfolioDemoDebugOverlay : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private PortfolioGameFlowCoordinator flowCoordinator;

        private const float RefreshInterval = 0.2f;

        private RectTransform overlayRoot;
        private TextMeshProUGUI bodyText;
        private float nextRefreshTime;
        private bool isVisible;

        private void Awake()
        {
            if (flowCoordinator == null)
                flowCoordinator = GetComponent<PortfolioGameFlowCoordinator>();

            EnsureOverlay();
            SetVisible(false);
        }

        private void Update()
        {
            if (WasTogglePressed())
                SetVisible(!isVisible);

            if (!isVisible || Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshText();
        }

        private void EnsureOverlay()
        {
            if (overlayRoot != null)
                return;

            Transform host = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                host = canvas.transform;

            GameObject panel = new("Debug Overlay", typeof(RectTransform));
            panel.transform.SetParent(host, false);

            overlayRoot = panel.GetComponent<RectTransform>();
            overlayRoot.anchorMin = new Vector2(0f, 1f);
            overlayRoot.anchorMax = new Vector2(0f, 1f);
            overlayRoot.pivot = new Vector2(0f, 1f);
            overlayRoot.anchoredPosition = new Vector2(24f, -270f);
            overlayRoot.sizeDelta = new Vector2(560f, 350f);

            UnityEngine.UI.Image background = panel.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.03f, 0.04f, 0.06f, 0.9f);
            background.raycastTarget = false;

            bodyText = CreateText(panel.transform, "Body");
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (overlayRoot != null)
                overlayRoot.gameObject.SetActive(visible);

            if (visible)
            {
                nextRefreshTime = 0f;
                RefreshText();
            }
        }

        private void RefreshText()
        {
            if (bodyText == null)
                return;

            var builder = new StringBuilder(1024);
            builder.AppendLine("DEBUG OVERLAY");
            builder.AppendLine($"Flow: {flowCoordinator?.CurrentState.ToString() ?? "Unknown"}");
            builder.AppendLine($"Network: {flowCoordinator?.CurrentNetworkState.ToString() ?? "Unknown"}");

            NetworkRunner runner = ResolveRunner();
            if (runner != null)
            {
                builder.AppendLine($"Room: {runner.SessionInfo.Name}");
                int playerCount = 0;
                foreach (PlayerRef _ in runner.ActivePlayers)
                    playerCount++;

                builder.AppendLine($"Players: {playerCount}");
                builder.AppendLine($"Local Player: {runner.LocalPlayer.PlayerId}");
                builder.AppendLine($"Shared Master: {runner.IsSharedModeMasterClient}");
            }
            else
            {
                builder.AppendLine("Room: Not connected");
            }

            if (QuestManager.HasInstance)
            {
                builder.AppendLine($"Level Completed: {QuestManager.Instance.IsLevelCompleted}");
                builder.AppendLine("Quests:");
                foreach (QuestInfo quest in QuestManager.Instance.AllQuests)
                {
                    if (quest == null)
                        continue;

                    builder.AppendLine($"- {QuestDebugSnapshotBuilder.FormatLine(quest, QuestManager.Instance)}");
                }
            }
            else
            {
                builder.AppendLine("QuestManager: Missing");
            }

            builder.AppendLine();
            builder.AppendLine("F3 - Toggle overlay");
            bodyText.text = builder.ToString();
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                System.Enum.TryParse(toggleKey.ToString(), out Key inputSystemKey))
            {
                return Keyboard.current[inputSystemKey].wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(toggleKey);
#else
            return false;
#endif
        }

        private static NetworkRunner ResolveRunner()
        {
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning && runner.ProvideInput)
                    return runner;
            }

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return runner;
            }

            return null;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 16f);
            rect.offsetMax = new Vector2(-16f, -16f);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = new Color(0.88f, 0.95f, 1f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }
    }
}
