using EnglishQuest.QuestSystem;
using Fusion;
using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    public sealed class PortfolioDemoHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI interactionPrompt;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private QuestObjectiveEventBus objectiveEventBus;
        [SerializeField] private DialogueManager dialogueManager;

        private const float PlayerPanelRefreshInterval = 0.5f;
        private readonly List<GameObject> openWorldHudRoots = new();
        private readonly List<TextMeshProUGUI> playerRows = new();

        private RectTransform demoBriefingRoot;
        private RectTransform playerPanelRoot;
        private RectTransform playerListRoot;
        private RectTransform completionPanelRoot;
        private float nextPlayerPanelRefreshTime;
        private string lastPlayerPanelHash = "";
        private bool isOpenWorldHudVisible = true;
        private PortfolioGameFlowCoordinator flowCoordinator;
        private PortfolioDemoDebugOverlay debugOverlay;
        private IQuestService questService;
        private Button replayButton;
        private Button closeCompletionButton;
        private TextMeshProUGUI completionBodyText;
        private bool completionDismissed;
        private CanvasGroup completionCanvasGroup;
        private IPlayerLockSystem completionLockSystem;
        private bool completionInteractionOwned;

        private void Awake()
        {
            EnsureFlowCoordinator();
            EnsureDemoBriefingPanel();
            EnsurePlayerPanel();
            EnsureCompletionPanel();
            EnsureDebugOverlay();
            CacheOpenWorldHudRoots();
            ApplyState(CurrentStateOrFallback());
        }

        private void Update()
        {
            ApplyState(CurrentStateOrFallback());

            if (!isOpenWorldHudVisible)
                return;

            if (Time.unscaledTime < nextPlayerPanelRefreshTime)
                return;

            nextPlayerPanelRefreshTime = Time.unscaledTime + PlayerPanelRefreshInterval;
            RefreshPlayerPanel();
        }

        private void OnEnable()
        {
            if (objectiveEventBus != null)
                objectiveEventBus.OnMiniGameCompleted += HandleMiniGameCompleted;

            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStart += HandleDialogueStarted;
                dialogueManager.OnDialogueEnd += HandleDialogueEnded;
            }

            ResolveQuestService();
            if (questService != null)
                questService.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            if (objectiveEventBus != null)
                objectiveEventBus.OnMiniGameCompleted -= HandleMiniGameCompleted;

            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStart -= HandleDialogueStarted;
                dialogueManager.OnDialogueEnd -= HandleDialogueEnded;
            }

            if (questService != null)
                questService.OnLevelCompleted -= HandleLevelCompleted;

            ReleaseCompletionInteraction();
        }

        public void SetInteractionPrompt(string message)
        {
            if (interactionPrompt == null)
                return;

            interactionPrompt.text = message;
            interactionPrompt.gameObject.SetActive(isOpenWorldHudVisible && !string.IsNullOrEmpty(message));
        }

        private void HandleMiniGameCompleted(QuestObjectiveEvents.MiniGameCompleted e)
        {
            SetStatus(GetCompletionMessage(e.GameId));
        }

        private void HandleDialogueStarted() => SetStatus("Dialogue started");
        private void HandleDialogueEnded() => SetStatus("Dialogue completed");
        private void HandleLevelCompleted()
        {
            completionDismissed = false;
            SetStatus("All lessons completed. MVP level finished.");
            UpdateCompletionPanelContent();
            ApplyState(PortfolioGameFlowState.LevelCompleted);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private static string GetCompletionMessage(string gameId)
        {
            return gameId switch
            {
                "line_match" => "Line Match completed. Coach Ben is unlocked.",
                "letter_ordering" => "Letter Ordering completed. Guide Nora is unlocked.",
                "word_ordering" => "Word Ordering completed. MVP quest chain finished.",
                _ => $"Completed: {gameId}"
            };
        }

        private void EnsureFlowCoordinator()
        {
            flowCoordinator = GetComponent<PortfolioGameFlowCoordinator>();
            if (flowCoordinator == null)
                flowCoordinator = gameObject.AddComponent<PortfolioGameFlowCoordinator>();
        }

        private void EnsureDebugOverlay()
        {
            debugOverlay = GetComponent<PortfolioDemoDebugOverlay>();
            if (debugOverlay == null)
                debugOverlay = gameObject.AddComponent<PortfolioDemoDebugOverlay>();
        }

        private void EnsurePlayerPanel()
        {
            if (playerPanelRoot != null)
                return;

            Transform host = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                host = canvas.transform;

            GameObject panel = new GameObject("Multiplayer Players Panel", typeof(RectTransform));
            panel.transform.SetParent(host, false);

            playerPanelRoot = panel.GetComponent<RectTransform>();
            playerPanelRoot.anchorMin = new Vector2(0f, 1f);
            playerPanelRoot.anchorMax = new Vector2(0f, 1f);
            playerPanelRoot.pivot = new Vector2(0f, 1f);
            playerPanelRoot.anchoredPosition = new Vector2(24f, -118f);
            playerPanelRoot.sizeDelta = new Vector2(280f, 132f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.72f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(
                "Title",
                panel.transform,
                "Players",
                17f,
                FontStyles.Bold,
                new Color(0.86f, 0.97f, 1f, 1f),
                TextAlignmentOptions.Left);

            GameObject list = new GameObject("Rows", typeof(RectTransform));
            list.transform.SetParent(panel.transform, false);
            playerListRoot = list.GetComponent<RectTransform>();

            VerticalLayoutGroup listLayout = list.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 4f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            RefreshPlayerPanel(force: true);
        }

        private void CacheOpenWorldHudRoots()
        {
            openWorldHudRoots.Clear();

            AddRoot("Title");
            AddRoot("Controls");

            if (interactionPrompt != null)
                AddRoot(interactionPrompt.gameObject);

            if (statusText != null)
                AddRoot(statusText.gameObject);

            if (demoBriefingRoot != null)
                AddRoot(demoBriefingRoot.gameObject);

            if (playerPanelRoot != null)
                AddRoot(playerPanelRoot.gameObject);
        }

        private void AddRoot(string childName)
        {
            Transform root = transform.Find(childName);
            if (root == null)
                root = FindChildByName(transform, childName);

            if (root != null)
                AddRoot(root.gameObject);
        }

        private void AddRoot(GameObject root)
        {
            if (root != null && root != gameObject && !openWorldHudRoots.Contains(root))
                openWorldHudRoots.Add(root);
        }

        private void EnsureDemoBriefingPanel()
        {
            if (demoBriefingRoot != null)
                return;

            Transform host = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                host = canvas.transform;

            GameObject panel = new GameObject("Demo Briefing Panel", typeof(RectTransform));
            panel.transform.SetParent(host, false);

            demoBriefingRoot = panel.GetComponent<RectTransform>();
            demoBriefingRoot.anchorMin = new Vector2(1f, 1f);
            demoBriefingRoot.anchorMax = new Vector2(1f, 1f);
            demoBriefingRoot.pivot = new Vector2(1f, 1f);
            demoBriefingRoot.anchoredPosition = new Vector2(-24f, -24f);
            demoBriefingRoot.sizeDelta = new Vector2(470f, 178f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.78f);
            panelBackground.raycastTarget = false;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(
                "Title",
                panel.transform,
                "English Quest Portfolio Demo",
                18f,
                FontStyles.Bold,
                new Color(0.96f, 0.84f, 0.38f, 1f),
                TextAlignmentOptions.Left);

            TextMeshProUGUI body = CreateText(
                "Body",
                panel.transform,
                "Open-world English quest slice: talk to NPCs, complete 3 learning mini-games in order, and test 2-player Photon Fusion multiplayer.\nControls: WASD move, Mouse look, Space jump, E interact.",
                15f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Left,
                wrap: true);
            body.lineSpacing = 8f;
        }

        private void EnsureCompletionPanel()
        {
            if (completionPanelRoot != null)
                return;

            Transform host = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                host = canvas.transform;

            GameObject panel = new GameObject("Completion Panel", typeof(RectTransform));
            panel.transform.SetParent(host, false);

            completionPanelRoot = panel.GetComponent<RectTransform>();
            completionPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            completionPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            completionPanelRoot.pivot = new Vector2(0.5f, 0.5f);
            completionPanelRoot.anchoredPosition = Vector2.zero;
            completionPanelRoot.sizeDelta = new Vector2(900f, 420f);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.02f, 0.05f, 0.07f, 0.96f);

            completionCanvasGroup = panel.AddComponent<CanvasGroup>();
            completionCanvasGroup.alpha = 0f;
            completionCanvasGroup.interactable = false;
            completionCanvasGroup.blocksRaycasts = false;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 28, 28);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateText(
                "Title",
                panel.transform,
                "Level Complete",
                34f,
                FontStyles.Bold,
                new Color(1f, 0.84f, 0.36f, 1f),
                TextAlignmentOptions.Center);

            completionBodyText = CreateText(
                "Body",
                panel.transform,
                "",
                22f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Center,
                wrap: true);
            completionBodyText.lineSpacing = 8f;

            GameObject buttons = new("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttons.transform.SetParent(panel.transform, false);

            HorizontalLayoutGroup buttonsLayout = buttons.GetComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 18f;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childControlHeight = false;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            replayButton = CreateActionButton(buttons.transform, "Replay From Start", ReplayFromStart);
            closeCompletionButton = CreateActionButton(buttons.transform, "Close", HideCompletionPanel);

            panel.SetActive(false);
        }

        private void RefreshPlayerPanel(bool force = false)
        {
            EnsurePlayerPanel();

            if (!isOpenWorldHudVisible)
            {
                SetPlayerPanelVisible(false);
                return;
            }

            NetworkRunner runner = ResolveRunner();
            if (runner == null || !runner.IsRunning)
            {
                SetPlayerPanelVisible(false);
                return;
            }

            SetPlayerPanelVisible(true);

            var displayNames = new List<string>();
            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
            {
                string displayName = ResolvePlayerDisplayName(runner, player);
                if (player == runner.LocalPlayer)
                    displayName += "  (You)";

                displayNames.Add(displayName);
            }

            if (displayNames.Count == 0)
                displayNames.Add("Waiting for players...");

            string panelHash = string.Join("|", displayNames);
            if (!force && panelHash == lastPlayerPanelHash)
                return;

            lastPlayerPanelHash = panelHash;
            EnsurePlayerRows(displayNames.Count);

            for (int i = 0; i < playerRows.Count; i++)
            {
                bool isActive = i < displayNames.Count;
                playerRows[i].transform.parent.gameObject.SetActive(isActive);
                if (isActive)
                    playerRows[i].text = displayNames[i];
            }
        }

        private void EnsurePlayerRows(int count)
        {
            while (playerRows.Count < count)
            {
                int rowIndex = playerRows.Count;
                GameObject row = new GameObject($"Player Row {rowIndex + 1}", typeof(RectTransform));
                row.transform.SetParent(playerListRoot, false);

                Image rowBackground = row.AddComponent<Image>();
                rowBackground.color = rowIndex == 0
                    ? new Color(0.13f, 0.55f, 0.48f, 0.52f)
                    : new Color(1f, 1f, 1f, 0.08f);

                HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(8, 8, 4, 4);
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = false;

                TextMeshProUGUI label = CreateText(
                    "Name",
                    row.transform,
                    "",
                    15f,
                    FontStyles.Normal,
                    Color.white,
                    TextAlignmentOptions.Left);
                playerRows.Add(label);
            }
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            bool wrap = false)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return label;
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

        private static string ResolvePlayerDisplayName(NetworkRunner runner, PlayerRef player)
        {
            NetworkObject playerObject = runner.GetPlayerObject(player);
            if (playerObject != null && playerObject.TryGetComponent(out PlayerNameSync nameSync))
            {
                string syncedName = nameSync.PlayerName.ToString();
                if (!string.IsNullOrWhiteSpace(syncedName))
                    return syncedName;
            }

            return $"Player {player.PlayerId}";
        }

        private bool IsAnyMiniGameOpen()
        {
            foreach (LetterConnectionScreenView view in FindObjectsByType<LetterConnectionScreenView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (view != null && view.gameObject.activeInHierarchy)
                    return true;
            }

            foreach (WordGamePanelView view in FindObjectsByType<WordGamePanelView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (view != null && view.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private void ApplyOpenWorldHudVisibility(bool isVisible)
        {
            if (isOpenWorldHudVisible == isVisible)
                return;

            isOpenWorldHudVisible = isVisible;

            if (openWorldHudRoots.Count == 0)
                CacheOpenWorldHudRoots();

            foreach (GameObject root in openWorldHudRoots)
            {
                if (root == null)
                    continue;

                bool targetVisible = isVisible;
                if (interactionPrompt != null && root == interactionPrompt.gameObject)
                    targetVisible = isVisible && !string.IsNullOrEmpty(interactionPrompt.text);

                if (root.activeSelf != targetVisible)
                    root.SetActive(targetVisible);
            }

            if (!isVisible && interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);

            if (isVisible)
                RefreshPlayerPanel(force: true);
        }

        private void ApplyState(PortfolioGameFlowState state)
        {
            bool showOpenWorldHud = state != PortfolioGameFlowState.Dialogue &&
                                    state != PortfolioGameFlowState.MiniGame &&
                                    (state != PortfolioGameFlowState.LevelCompleted || completionDismissed);

            ApplyOpenWorldHudVisibility(showOpenWorldHud);

            bool showCompletion = state == PortfolioGameFlowState.LevelCompleted && !completionDismissed;
            SetCompletionPanelVisible(showCompletion);
        }

        private void SetPlayerPanelVisible(bool isVisible)
        {
            isVisible &= isOpenWorldHudVisible;

            if (playerPanelRoot != null && playerPanelRoot.gameObject.activeSelf != isVisible)
                playerPanelRoot.gameObject.SetActive(isVisible);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private PortfolioGameFlowState CurrentStateOrFallback()
        {
            return flowCoordinator != null
                ? flowCoordinator.CurrentState
                : PortfolioGameFlowState.OpenWorld;
        }

        private void ResolveQuestService()
        {
            if (questService != null)
                return;

            ServiceLocator.For(this)?.TryGet(out questService);
            if (questService == null && QuestManager.HasInstance)
                questService = QuestManager.Instance;
        }

        private void UpdateCompletionPanelContent()
        {
            if (completionBodyText == null)
                return;

            completionBodyText.text =
                "You finished the full English Quest MVP slice.\n\n" +
                "Teacher Ada introduced the first letters.\n" +
                "Coach Ben reinforced vocabulary through missing letters.\n" +
                "Guide Nora completed the flow with the final lesson.\n\n" +
                "You can now restart the prototype from the beginning.";
        }

        private void ReplayFromStart()
        {
            if (QuestManager.HasInstance)
                QuestManager.Instance.ResetAllProgress();

            if (QuestManager.HasInstance &&
                QuestManager.Instance.TryGetComponent(out QuestProgressPersistence persistence))
            {
                persistence.ClearSavedProgress();
            }

            SetStatus("Progress reset. Start again from Teacher Ada.");
            completionDismissed = false;
            HideCompletionPanel();

            if (flowCoordinator != null)
                flowCoordinator.RefreshNow(force: true);
        }

        private void HideCompletionPanel()
        {
            completionDismissed = true;
            SetCompletionPanelVisible(false);
        }

        private void SetCompletionPanelVisible(bool isVisible)
        {
            if (completionPanelRoot == null)
                return;

            if (isVisible)
            {
                completionPanelRoot.SetAsLastSibling();
                completionPanelRoot.gameObject.SetActive(true);

                if (completionCanvasGroup != null)
                {
                    completionCanvasGroup.alpha = 1f;
                    completionCanvasGroup.interactable = true;
                    completionCanvasGroup.blocksRaycasts = true;
                }

                AcquireCompletionInteraction();
                return;
            }

            ReleaseCompletionInteraction();

            if (completionCanvasGroup != null)
            {
                completionCanvasGroup.alpha = 0f;
                completionCanvasGroup.interactable = false;
                completionCanvasGroup.blocksRaycasts = false;
            }

            completionPanelRoot.gameObject.SetActive(false);
        }

        private void AcquireCompletionInteraction()
        {
            if (completionInteractionOwned)
                return;

            if (!TryResolveCompletionLockSystem(out completionLockSystem))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            completionLockSystem.Lock(this,
                PlayerLockSystem.LockType.Movement,
                PlayerLockSystem.LockType.Camera,
                PlayerLockSystem.LockType.Interaction,
                PlayerLockSystem.LockType.Cursor,
                PlayerLockSystem.LockType.GameplayInput);

            completionInteractionOwned = true;
        }

        private void ReleaseCompletionInteraction()
        {
            if (!completionInteractionOwned || completionLockSystem == null)
                return;

            completionLockSystem.Unlock(this,
                PlayerLockSystem.LockType.Movement,
                PlayerLockSystem.LockType.Camera,
                PlayerLockSystem.LockType.Interaction,
                PlayerLockSystem.LockType.Cursor,
                PlayerLockSystem.LockType.GameplayInput);

            completionInteractionOwned = false;
            completionLockSystem = null;
        }

        private bool TryResolveCompletionLockSystem(out IPlayerLockSystem lockSystem)
        {
            if (ServiceLocator.For(this)?.TryGet(out lockSystem) == true && lockSystem != null)
                return true;

            foreach (PlayerInteraction interaction in FindObjectsByType<PlayerInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interaction == null || !interaction.isActiveAndEnabled)
                    continue;

                if (ServiceLocator.For(interaction)?.TryGet(out lockSystem) == true && lockSystem != null)
                    return true;
            }

            lockSystem = null;
            return false;
        }

        private static Button CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 64f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.52f, 0.46f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            CreateText(
                "Label",
                buttonObject.transform,
                label,
                22f,
                FontStyles.Bold,
                Color.white,
                TextAlignmentOptions.Center).rectTransform.StretchToParent();

            return button;
        }
    }
}

