using EnglishQuest.QuestSystem;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    [DefaultExecutionOrder(-10000)]
    public sealed class PortfolioDemoHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI interactionPrompt;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private QuestObjectiveEventBus objectiveEventBus;
        [SerializeField] private DialogueManager dialogueManager;
        [Header("Manual UI Theme Sprites")]
        [Tooltip("Explicit portfolio UI sprites. Empty fields stay empty; no Resources fallback or automatic replacement is used.")]
        [SerializeField] private PortfolioThemeSpriteSet themeSprites;

        private const float PlayerPanelRefreshInterval = 0.5f;
        private readonly List<GameObject> openWorldHudRoots = new();
        private readonly List<TextMeshProUGUI> playerRows = new();
        private readonly List<Image> playerRowBackgrounds = new();

        [Header("Manual HUD Scene References")]
        [SerializeField] private RectTransform headerCardRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI controlsText;
        [SerializeField] private TextMeshProUGUI headerEyebrowText;
        [SerializeField] private RectTransform demoBriefingRoot;
        [SerializeField] private TextMeshProUGUI demoBriefingTitleText;
        [SerializeField] private TextMeshProUGUI demoBriefingBodyText;
        [SerializeField] private RectTransform playerPanelRoot;
        [SerializeField] private RectTransform playerListRoot;
        [SerializeField] private TextMeshProUGUI playerPanelTitleText;
        [SerializeField] private TextMeshProUGUI playerPanelSupportText;
        [SerializeField] private RectTransform completionPanelRoot;
        [SerializeField] private TextMeshProUGUI completionEyebrowText;
        [SerializeField] private TextMeshProUGUI completionTitleText;
        [SerializeField] private TextMeshProUGUI completionSupportText;
        [SerializeField] private TextMeshProUGUI completionBodyText;
        [SerializeField] private TextMeshProUGUI completionFooterText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button closeCompletionButton;
        [SerializeField] private RectTransform transitionOverlayRoot;
        [SerializeField] private CanvasGroup transitionOverlayCanvasGroup;
        [SerializeField] private TextMeshProUGUI transitionOverlayEyebrowText;
        [SerializeField] private TextMeshProUGUI transitionOverlayTitleText;
        [SerializeField] private TextMeshProUGUI transitionOverlayBodyText;
        private float nextPlayerPanelRefreshTime;
        private string lastPlayerPanelHash = "";
        private string lastBriefingHash = "";
        private bool isOpenWorldHudVisible = true;
        private PortfolioGameFlowCoordinator flowCoordinator;
        private PortfolioDemoDebugOverlay debugOverlay;
        private IQuestService questService;
        private bool completionDismissed;
        private Canvas completionCanvas;
        private CanvasGroup completionCanvasGroup;
        private IPlayerLockSystem completionLockSystem;
        private bool completionInteractionOwned;
        private PortfolioOptionalCoopStudyCircle optionalCoopStudyCircle;
        private Coroutine completionPanelRoutine;
        private Coroutine transitionOverlayRoutine;
        private PortfolioGameFlowState lastAppliedState = (PortfolioGameFlowState)(-1);

        private readonly struct PlayerPanelEntry
        {
            public PlayerPanelEntry(string displayName, string status, bool isLocalPlayer)
            {
                DisplayName = displayName;
                Status = status;
                IsLocalPlayer = isLocalPlayer;
            }

            public string DisplayName { get; }
            public string Status { get; }
            public bool IsLocalPlayer { get; }
        }

        private void Awake()
        {
            PortfolioThemeResources.SetSprites(themeSprites);
            ResolveSceneBindings();
            EnsureFlowCoordinator();
            EnsureHeaderPresentation();
            EnsureDemoBriefingPanel();
            EnsurePlayerPanel();
            EnsureCompletionPanel();
            EnsureTransitionOverlay();
            EnsureDebugOverlay();
            EnsureOptionalCoopStudyCircle();
            CacheOpenWorldHudRoots();
            ApplyState(CurrentStateOrFallback());
        }

        private void OnValidate()
        {
            PortfolioThemeResources.SetSprites(themeSprites);
        }

        private void Update()
        {
            ApplyState(CurrentStateOrFallback());

            if (!isOpenWorldHudVisible)
                return;

            if (Time.unscaledTime < nextPlayerPanelRefreshTime)
                return;

            nextPlayerPanelRefreshTime = Time.unscaledTime + PlayerPanelRefreshInterval;
            RefreshDemoBriefing();
            RefreshPlayerPanel();
        }

        private void OnEnable()
        {
            ResolveSceneBindings();

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

            if (completionPanelRoutine != null)
                StopCoroutine(completionPanelRoutine);

            if (transitionOverlayRoutine != null)
                StopCoroutine(transitionOverlayRoutine);
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
            completionDismissed = !HasConfiguredCompletionPanel();
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
        }

        private void EnsureDebugOverlay()
        {
            debugOverlay = GetComponent<PortfolioDemoDebugOverlay>();
        }

        private void EnsureOptionalCoopStudyCircle()
        {
            if (optionalCoopStudyCircle == null)
            {
                optionalCoopStudyCircle = FindFirstObjectByType<PortfolioOptionalCoopStudyCircle>(
                    FindObjectsInactive.Include);
            }
        }

        private void EnsureHeaderPresentation()
        {
            ResolveSceneBindings();
            if (headerCardRoot == null)
            {
                Transform headerTransform = FindChildByName(transform, "Header Presentation Card");
                if (headerTransform != null)
                    headerCardRoot = headerTransform.GetComponent<RectTransform>();
            }

            if (headerCardRoot != null)
            {
                if (headerEyebrowText == null)
                {
                    Transform eyebrowTransform = FindChildByName(headerCardRoot, "Eyebrow");
                    if (eyebrowTransform != null)
                        headerEyebrowText = eyebrowTransform.GetComponent<TextMeshProUGUI>();
                }

                if (titleText == null)
                {
                    Transform titleTransform = FindChildByName(headerCardRoot, "Title");
                    if (titleTransform != null)
                        titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                }

                if (controlsText == null)
                {
                    Transform controlsTransform = FindChildByName(headerCardRoot, "Controls");
                    if (controlsTransform != null)
                        controlsText = controlsTransform.GetComponent<TextMeshProUGUI>();
                }

                if (statusText == null)
                {
                    Transform statusTransform = FindChildByName(headerCardRoot, "System Status");
                    if (statusTransform != null)
                        statusText = statusTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private void EnsurePlayerPanel()
        {
            if (playerPanelRoot == null)
            {
                Transform panel = FindChildByName(transform, "Multiplayer Players Panel");
                if (panel != null)
                    playerPanelRoot = panel.GetComponent<RectTransform>();
            }

            if (playerPanelRoot == null)
                return;

            if (playerPanelTitleText == null)
                playerPanelTitleText = FindChildText(playerPanelRoot, "Title");

            if (playerPanelSupportText == null)
                playerPanelSupportText = FindChildText(playerPanelRoot, "Support");

            if (playerListRoot == null)
            {
                Transform rows = FindChildByName(playerPanelRoot, "Rows");
                if (rows != null)
                    playerListRoot = rows.GetComponent<RectTransform>();
            }

            CacheExistingPlayerRows();

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

            if (headerCardRoot != null)
                AddRoot(headerCardRoot.gameObject);

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
            if (demoBriefingRoot == null)
            {
                Transform panel = FindChildByName(transform, "Demo Briefing Panel");
                if (panel != null)
                    demoBriefingRoot = panel.GetComponent<RectTransform>();
            }

            if (demoBriefingRoot == null)
                return;

            if (demoBriefingTitleText == null)
                demoBriefingTitleText = FindChildText(demoBriefingRoot, "Current Step");

            if (demoBriefingBodyText == null)
                demoBriefingBodyText = FindChildText(demoBriefingRoot, "Body");

            RefreshDemoBriefing(force: true);
        }

        private void EnsureCompletionPanel()
        {
            if (completionPanelRoot == null)
            {
                Transform panel = FindChildByName(transform, "Completion Panel");
                if (panel != null)
                    completionPanelRoot = panel.GetComponent<RectTransform>();
            }

            if (completionPanelRoot == null)
                return;

            completionCanvas = completionPanelRoot.GetComponent<Canvas>();
            completionCanvasGroup = completionPanelRoot.GetComponent<CanvasGroup>();
            completionEyebrowText ??= FindChildText(completionPanelRoot, "Eyebrow") ??
                                      FindChildText(completionPanelRoot, "Section Label");
            completionTitleText ??= FindChildText(completionPanelRoot, "Title");
            completionSupportText ??= FindChildText(completionPanelRoot, "Support") ??
                                      FindChildText(completionPanelRoot, "Current Step");
            completionBodyText ??= FindChildText(completionPanelRoot, "Body");
            completionFooterText ??= FindChildText(completionPanelRoot, "Footer");

            if (replayButton == null)
                replayButton = FindChildButton(completionPanelRoot, "Replay From Start Button") ??
                               FindChildButton(completionPanelRoot, "Start Again Button");

            if (closeCompletionButton == null)
                closeCompletionButton = FindChildButton(completionPanelRoot, "Close Button") ??
                                        FindChildButton(completionPanelRoot, "Continue Button");

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(ReplayFromStart);
                replayButton.onClick.AddListener(ReplayFromStart);
            }

            if (closeCompletionButton != null)
            {
                closeCompletionButton.onClick.RemoveListener(HideCompletionPanel);
                closeCompletionButton.onClick.AddListener(HideCompletionPanel);
            }
        }

        private void EnsureTransitionOverlay()
        {
            if (transitionOverlayRoot == null)
            {
                Transform overlay = FindChildByName(transform, "MiniGame Transition Overlay");
                if (overlay != null)
                    transitionOverlayRoot = overlay.GetComponent<RectTransform>();
            }

            if (transitionOverlayRoot == null)
                return;

            transitionOverlayCanvasGroup ??= transitionOverlayRoot.GetComponent<CanvasGroup>();
            transitionOverlayEyebrowText ??= FindChildText(transitionOverlayRoot, "Eyebrow");
            transitionOverlayTitleText ??= FindChildText(transitionOverlayRoot, "Title");
            transitionOverlayBodyText ??= FindChildText(transitionOverlayRoot, "Body");
        }

        private void RefreshPlayerPanel(bool force = false)
        {
            if (playerPanelRoot == null || playerListRoot == null)
                return;

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

            var sessionPlayers = new List<PlayerRef>();
            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
                sessionPlayers.Add(player);

            if (playerPanelTitleText != null)
                playerPanelTitleText.text = PortfolioPlayerStatusFormatter.GetPlayerPanelTitle(sessionPlayers.Count);

            if (playerPanelSupportText != null)
                playerPanelSupportText.text = PortfolioPlayerStatusFormatter.GetPlayerPanelSupportText(sessionPlayers.Count);

            var entries = new List<PlayerPanelEntry>();
            for (int index = 0; index < sessionPlayers.Count; index++)
            {
                PlayerRef player = sessionPlayers[index];
                string displayName = ResolvePlayerDisplayName(runner, player);
                bool isLocalPlayer = player == runner.LocalPlayer;
                if (isLocalPlayer)
                    displayName += "  (You)";

                string rawStatus = ResolvePlayerStatus(runner, player);
                entries.Add(new PlayerPanelEntry(
                    displayName,
                    PortfolioPlayerStatusFormatter.DecorateSessionStatus(
                        rawStatus,
                        connectedPlayerCount: sessionPlayers.Count,
                        isLocalPlayer: isLocalPlayer),
                    isLocalPlayer));
            }

            if (entries.Count == 0)
            {
                entries.Add(new PlayerPanelEntry(
                    "Local player",
                    PortfolioPlayerStatusFormatter.PreparingLocalPlayerStatus,
                    true));
            }

            var hashParts = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
                hashParts.Add($"{entries[i].DisplayName}|{entries[i].Status}|{entries[i].IsLocalPlayer}");

            string panelHash = string.Join("||", hashParts);
            if (!force && panelHash == lastPlayerPanelHash)
                return;

            lastPlayerPanelHash = panelHash;
            CacheExistingPlayerRows();

            for (int i = 0; i < playerRows.Count; i++)
            {
                bool isActive = i < entries.Count;
                playerRows[i].transform.parent.gameObject.SetActive(isActive);
                if (isActive)
                {
                    PlayerPanelEntry entry = entries[i];
                    playerRows[i].text = $"<b>{entry.DisplayName}</b>\n<size=80%>{entry.Status}</size>";
                    playerRowBackgrounds[i].color = entry.IsLocalPlayer
                        ? new Color(0.13f, 0.55f, 0.48f, 0.52f)
                        : new Color(1f, 1f, 1f, 0.08f);
                }
            }
        }

        private void RefreshDemoBriefing(bool force = false)
        {
            if (!isOpenWorldHudVisible || demoBriefingTitleText == null || demoBriefingBodyText == null)
                return;

            (string title, string body) = BuildDemoBriefingContent();
            string nextHash = $"{title}|{body}";
            if (!force && nextHash == lastBriefingHash)
                return;

            lastBriefingHash = nextHash;
            demoBriefingTitleText.text = title;
            demoBriefingBodyText.text = body;
        }

        private void CacheExistingPlayerRows()
        {
            if (playerListRoot == null || playerRows.Count > 0)
                return;

            for (int i = 0; i < playerListRoot.childCount; i++)
            {
                Transform row = playerListRoot.GetChild(i);
                TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                Image background = row.GetComponent<Image>();
                if (label == null || background == null)
                    continue;

                playerRows.Add(label);
                playerRowBackgrounds.Add(background);
            }
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
            return PortfolioSessionPlayerUtility.ResolveDisplayName(runner, player);
        }

        private static string ResolvePlayerStatus(NetworkRunner runner, PlayerRef player)
        {
            return PortfolioSessionPlayerUtility.ResolveActivityStatus(runner, player);
        }

        private (string Title, string Body) BuildDemoBriefingContent()
        {
            if (questService == null)
                ResolveQuestService();

            NetworkRunner runner = ResolveRunner();
            int connectedPlayerCount = runner != null && runner.IsRunning
                ? CountSessionPlayers(runner)
                : 0;
            QuestInfo highlightedQuest = FindFirstQuest(QuestState.IN_PROGRESS, QuestState.CAN_FINISH) ??
                                        FindFirstQuest(QuestState.CAN_START) ??
                                        FindFirstQuest(QuestState.REQUIREMENTS_NOT_MET);

            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: runner != null && runner.IsRunning,
                isLevelCompleted: questService != null && questService.IsLevelCompleted,
                connectedPlayerCount: connectedPlayerCount,
                highlightedQuestState: highlightedQuest?.state,
                questHeadline: GetQuestHeadline(highlightedQuest),
                npcDisplayName: GetNpcDisplayName(highlightedQuest),
                objectiveSummary: BuildQuestObjectiveSummary(highlightedQuest),
                optionalCoopLine: BuildOptionalCoopBriefingLine(runner));

            return (content.Title, content.Body);
        }

        private string BuildOptionalCoopBriefingLine(NetworkRunner runner)
        {
            EnsureOptionalCoopStudyCircle();
            if (optionalCoopStudyCircle == null)
                return null;

            if (!optionalCoopStudyCircle.TryGetSnapshot(
                    runner,
                    runner != null ? runner.LocalPlayer : default,
                    out PortfolioOptionalCoopActivitySnapshot snapshot))
            {
                snapshot = new PortfolioOptionalCoopActivitySnapshot(
                    optionalCoopStudyCircle.ActivityName,
                    connectedPlayers: 0,
                    playersInside: 0,
                    requiredPlayers: 2,
                    isLocalPlayerInside: false,
                    isGroupActive: false);
            }

            return PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(snapshot);
        }

        private static int CountSessionPlayers(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return 0;

            int count = 0;
            foreach (PlayerRef _ in FusionCoSessionRunners.EnumeratePlayers(runner))
                count++;

            return count;
        }

        private QuestInfo FindFirstQuest(params QuestState[] states)
        {
            if (questService == null)
                return null;

            IReadOnlyList<QuestInfo> quests = questService.AllQuests;
            if (quests == null)
                return null;

            for (int i = 0; i < quests.Count; i++)
            {
                QuestInfo quest = quests[i];
                if (quest == null || !quest.UsesObjectives())
                    continue;

                for (int s = 0; s < states.Length; s++)
                {
                    if (quest.state == states[s])
                        return quest;
                }
            }

            return null;
        }

        private static string GetQuestHeadline(QuestInfo quest)
        {
            if (quest == null)
                return "Lesson";

            return !string.IsNullOrWhiteSpace(quest.displayName)
                ? quest.displayName.Trim()
                : "Lesson";
        }

        private static string GetNpcDisplayName(QuestInfo quest)
        {
            if (quest != null &&
                quest.TryGetDefinition(out QuestDefinitionSO definition) &&
                !string.IsNullOrWhiteSpace(definition.giverNpcId))
            {
                return definition.giverNpcId switch
                {
                    "teacher_ada" => "Teacher Ada",
                    "coach_ben" => "Coach Ben",
                    "guide_nora" => "Guide Nora",
                    _ => definition.giverNpcId
                };
            }

            return "the active NPC";
        }

        private string BuildQuestObjectiveSummary(QuestInfo quest)
        {
            if (quest == null || questService == null)
                return "Complete the active lesson objective.";

            string objectiveText = ActiveQuestDisplayHelper.BuildObjectiveText(quest, questService);
            string progressText = ActiveQuestDisplayHelper.BuildProgressText(quest, questService);

            if (!string.IsNullOrWhiteSpace(progressText))
                return $"{objectiveText} ({progressText})";

            return objectiveText;
        }

        private bool IsAnyMiniGameOpen()
        {
            foreach (QuestMiniGameRuntimeBase runtime in FindObjectsByType<QuestMiniGameRuntimeBase>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (runtime != null && runtime.IsMiniGameOpen)
                    return true;
            }

            return false;
        }

        private void ApplyOpenWorldHudVisibility(bool isVisible)
        {
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
            {
                RefreshDemoBriefing(force: true);
                RefreshPlayerPanel(force: true);
            }
        }

        private void ApplyState(PortfolioGameFlowState state)
        {
            if (state != lastAppliedState)
            {
                HandleStateTransition(lastAppliedState, state);
                lastAppliedState = state;
            }

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

        internal static Transform FindChildByName(Transform root, string childName)
        {
            if (ReferenceEquals(root, null) || string.IsNullOrEmpty(childName))
                return null;

            HashSet<int> visited = new();
            Stack<Transform> pending = new();
            pending.Push(root);

            while (pending.Count > 0)
            {
                Transform current = pending.Pop();
                if (ReferenceEquals(current, null))
                    continue;

                int id = current.GetInstanceID();
                if (!visited.Add(id))
                    continue;

                for (int i = current.childCount - 1; i >= 0; i--)
                {
                    Transform child = current.GetChild(i);
                    if (ReferenceEquals(child, null))
                        continue;

                    if (child.name == childName)
                        return child;

                    pending.Push(child);
                }
            }

            return null;
        }

        private static TextMeshProUGUI FindChildText(Transform root, string childName)
        {
            Transform child = FindChildByName(root, childName);
            return ReferenceEquals(child, null) ? null : child.GetComponent<TextMeshProUGUI>();
        }

        private static Button FindChildButton(Transform root, string childName)
        {
            Transform child = FindChildByName(root, childName);
            return ReferenceEquals(child, null) ? null : child.GetComponent<Button>();
        }

        private PortfolioGameFlowState CurrentStateOrFallback()
        {
            if (flowCoordinator != null)
                return flowCoordinator.CurrentState;

            ResolveQuestService();

            if (questService != null && questService.IsLevelCompleted)
                return PortfolioGameFlowState.LevelCompleted;

            if (IsAnyMiniGameOpen())
                return PortfolioGameFlowState.MiniGame;

            if (dialogueManager != null && dialogueManager.IsDialogueActive)
                return PortfolioGameFlowState.Dialogue;

            return PortfolioGameFlowState.OpenWorld;
        }

        private void HandleStateTransition(PortfolioGameFlowState previousState, PortfolioGameFlowState currentState)
        {
            if (currentState == PortfolioGameFlowState.MiniGame &&
                previousState != PortfolioGameFlowState.MiniGame)
            {
                (string title, string body) = BuildMiniGameTransitionContent();
                ShowTransitionOverlay(title, body);
            }
        }

        private void ResolveQuestService()
        {
            if (questService != null)
                return;

            ServiceLocator.For(this)?.TryGet(out questService);
            if (questService == null && QuestManager.HasInstance)
                questService = QuestManager.Instance;
        }

        private void ResolveSceneBindings()
        {
            if (titleText == null)
            {
                Transform titleTransform = FindChildByName(transform, "Title");
                if (titleTransform != null)
                    titleText = titleTransform.GetComponent<TextMeshProUGUI>();
            }

            if (controlsText == null)
            {
                Transform controlsTransform = FindChildByName(transform, "Controls");
                if (controlsTransform != null)
                    controlsText = controlsTransform.GetComponent<TextMeshProUGUI>();
            }

            if (interactionPrompt == null)
            {
                Transform promptTransform = FindChildByName(transform, "Interaction Prompt");
                if (promptTransform != null)
                    interactionPrompt = promptTransform.GetComponent<TextMeshProUGUI>();
            }

            if (statusText == null)
            {
                Transform statusTransform = FindChildByName(transform, "System Status");
                if (statusTransform != null)
                    statusText = statusTransform.GetComponent<TextMeshProUGUI>();
            }

            if (objectiveEventBus == null)
                objectiveEventBus = FindFirstObjectByType<QuestObjectiveEventBus>(FindObjectsInactive.Include);

            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        }

        private void UpdateCompletionPanelContent()
        {
            if (completionTitleText != null)
                completionTitleText.text = "English Quest MVP Finished";

            if (completionSupportText != null)
                completionSupportText.text = "Three connected learning beats are now fully playable in one clean open-world slice.";

            if (completionBodyText != null)
            {
                completionBodyText.text =
                    "Teacher Ada introduced the first letters.\n" +
                    "Coach Ben reinforced vocabulary through missing letters.\n" +
                    "Guide Nora completed the flow with the final lesson.";
            }

            if (completionFooterText != null)
            {
                completionFooterText.text =
                    "This is a portfolio prototype: solo completion remains valid, multiplayer stays optional, and you can restart from the beginning to replay the full flow.";
            }
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

            if (isVisible && !HasConfiguredCompletionPanel())
            {
                completionDismissed = true;
                ReleaseCompletionInteraction();

                if (completionPanelRoot.gameObject.activeSelf)
                    completionPanelRoot.gameObject.SetActive(false);

                return;
            }

            if (completionPanelRoutine != null)
                StopCoroutine(completionPanelRoutine);

            if (isVisible)
            {
                bool hasInteractiveButtons = replayButton != null || closeCompletionButton != null;
                HideTransitionOverlayImmediate();
                completionPanelRoot.SetAsLastSibling();
                completionPanelRoot.gameObject.SetActive(true);
                if (completionCanvas != null)
                {
                    completionCanvas.enabled = true;
                    completionCanvas.overrideSorting = true;
                    completionCanvas.sortingOrder = 5000;
                }

                if (completionCanvasGroup != null)
                {
                    completionCanvasGroup.interactable = hasInteractiveButtons;
                    completionCanvasGroup.blocksRaycasts = hasInteractiveButtons;
                }

                if (hasInteractiveButtons)
                    AcquireCompletionInteraction();
                else
                    ReleaseCompletionInteraction();

                completionPanelRoutine = StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 1f, 0.24f, deactivateOnComplete: false));
                return;
            }

            ReleaseCompletionInteraction();
            completionPanelRoutine = StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 0f, 0.18f, deactivateOnComplete: true, completionPanelRoot.gameObject));
        }

        private bool HasConfiguredCompletionPanel()
        {
            if (completionPanelRoot == null)
                return false;

            if (completionTitleText != null ||
                completionSupportText != null ||
                completionBodyText != null ||
                completionFooterText != null ||
                replayButton != null ||
                closeCompletionButton != null)
            {
                return true;
            }

            return completionPanelRoot.childCount > 0;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration, bool deactivateOnComplete, GameObject deactivateTarget = null)
        {
            if (canvasGroup == null)
            {
                if (deactivateOnComplete && deactivateTarget != null)
                    deactivateTarget.SetActive(false);
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            bool isCompletionOpening = canvasGroup == completionCanvasGroup && targetAlpha > 0.99f;

            canvasGroup.interactable = isCompletionOpening;
            canvasGroup.blocksRaycasts = isCompletionOpening;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            bool isVisible = targetAlpha > 0.99f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;

            if (deactivateOnComplete && deactivateTarget != null)
                deactivateTarget.SetActive(false);

            if (canvasGroup == completionCanvasGroup)
                completionPanelRoutine = null;
        }

        private (string Title, string Body) BuildMiniGameTransitionContent()
        {
            QuestInfo quest = FindFirstQuest(QuestState.IN_PROGRESS, QuestState.CAN_FINISH) ??
                             FindFirstQuest(QuestState.CAN_START) ??
                             FindFirstQuest(QuestState.REQUIREMENTS_NOT_MET);

            string npcName = GetNpcDisplayName(quest);
            string headline = GetQuestHeadline(quest);
            string objective = BuildQuestObjectiveSummary(quest);

            return ($"{npcName} - {headline}", objective);
        }

        private void ShowTransitionOverlay(string title, string body)
        {
            EnsureTransitionOverlay();
            if (transitionOverlayRoot == null || transitionOverlayCanvasGroup == null)
                return;

            if (transitionOverlayTitleText != null)
                transitionOverlayTitleText.text = title;

            if (transitionOverlayBodyText != null)
                transitionOverlayBodyText.text = body;

            if (transitionOverlayRoutine != null)
                StopCoroutine(transitionOverlayRoutine);

            transitionOverlayRoutine = StartCoroutine(PlayTransitionOverlayRoutine());
        }

        private IEnumerator PlayTransitionOverlayRoutine()
        {
            transitionOverlayRoot.gameObject.SetActive(true);
            transitionOverlayRoot.SetAsLastSibling();
            transitionOverlayCanvasGroup.alpha = 0f;

            float inDuration = 0.16f;
            float holdDuration = 0.42f;
            float outDuration = 0.2f;

            yield return LerpCanvasAlpha(transitionOverlayCanvasGroup, 0f, 1f, inDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return LerpCanvasAlpha(transitionOverlayCanvasGroup, 1f, 0f, outDuration);

            transitionOverlayRoot.gameObject.SetActive(false);
            transitionOverlayRoutine = null;
        }

        private void HideTransitionOverlayImmediate()
        {
            if (transitionOverlayRoutine != null)
            {
                StopCoroutine(transitionOverlayRoutine);
                transitionOverlayRoutine = null;
            }

            if (transitionOverlayCanvasGroup != null)
            {
                transitionOverlayCanvasGroup.alpha = 0f;
                transitionOverlayCanvasGroup.interactable = false;
                transitionOverlayCanvasGroup.blocksRaycasts = false;
            }

            if (transitionOverlayRoot != null)
                transitionOverlayRoot.gameObject.SetActive(false);
        }

        private static IEnumerator LerpCanvasAlpha(CanvasGroup canvasGroup, float start, float target, float duration)
        {
            if (canvasGroup == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }

            canvasGroup.alpha = target;
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

    }

    /// <summary>
    /// Runtime styling bridge for the portfolio dialogue UI.
    /// Keeps the portfolio presentation layer outside of DialogueManager core logic.
    /// </summary>
    public sealed class PortfolioDialogueVisualBridge : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private TextMeshProUGUI subtitleText;

        private CanvasGroup dialogueCanvasGroup;
        private RectTransform dialoguePanelRect;
        private Vector2 dialoguePanelBasePosition;
        private Coroutine panelRevealRoutine;
        private bool wasDialogueVisible;
        private string lastSpeakerName = string.Empty;

        private void Awake()
        {
            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);

            EnsureDialoguePresentation();
            ApplyDialogueTheme();
            SyncDialogueVisibilityInstant();
        }

        private void OnEnable()
        {
            EnsureDialoguePresentation();
            ApplyDialogueTheme();
            SyncDialogueVisibilityInstant();
        }

        private void Update()
        {
            bool isVisible = dialogueManager != null &&
                             dialogueManager.dialoguePanel != null &&
                             dialogueManager.dialoguePanel.activeInHierarchy;

            if (isVisible != wasDialogueVisible)
            {
                wasDialogueVisible = isVisible;
                if (isVisible)
                    StartDialogueReveal();
            }

            string currentSpeakerName = dialogueManager.nameText != null ? dialogueManager.nameText.text : string.Empty;
            if (currentSpeakerName != lastSpeakerName)
            {
                lastSpeakerName = currentSpeakerName;
                UpdateSubtitle(currentSpeakerName);
            }
        }

        private void ApplyDialogueTheme()
        {
            if (dialogueManager == null || dialogueManager.dialoguePanel == null)
                return;

            UpdateSubtitle(dialogueManager.nameText != null ? dialogueManager.nameText.text : string.Empty);
        }

        private void EnsureDialoguePresentation()
        {
            if (dialogueManager == null || dialogueManager.dialoguePanel == null)
                return;

            dialoguePanelRect = dialogueManager.dialoguePanel.GetComponent<RectTransform>();
            if (dialoguePanelRect == null)
                return;

            dialoguePanelBasePosition = dialoguePanelRect.anchoredPosition;

            dialogueCanvasGroup = dialogueManager.dialoguePanel.GetComponent<CanvasGroup>();

            if (subtitleText == null)
            {
                Transform subtitleTransform = PortfolioDemoHud.FindChildByName(
                    dialogueManager.dialoguePanel.transform,
                    "Portfolio Subtitle");
                if (subtitleTransform != null)
                    subtitleText = subtitleTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        private void UpdateSubtitle(string speakerName)
        {
            if (subtitleText == null)
                return;

            subtitleText.text = ResolveDialogueSubtitle(speakerName);
        }

        private static string ResolveDialogueSubtitle(string speakerName)
        {
            return speakerName switch
            {
                "Teacher Ada" => "Lesson 1 - Learn the first two letters",
                "Coach Ben" => "Lesson 2 - Fill the missing letter and grow vocabulary",
                "Guide Nora" => "Lesson 3 - Build the final answer with confidence",
                _ when !string.IsNullOrWhiteSpace(speakerName) => "Active lesson briefing",
                _ => "Quest dialogue"
            };
        }

        private void SyncDialogueVisibilityInstant()
        {
            bool isVisible = dialogueManager != null &&
                             dialogueManager.dialoguePanel != null &&
                             dialogueManager.dialoguePanel.activeInHierarchy;

            wasDialogueVisible = isVisible;

            if (dialogueCanvasGroup != null)
                dialogueCanvasGroup.alpha = isVisible ? 1f : 0f;

            if (dialoguePanelRect != null)
                dialoguePanelRect.anchoredPosition = dialoguePanelBasePosition;
        }

        private void StartDialogueReveal()
        {
            if (dialogueCanvasGroup == null || dialoguePanelRect == null)
                return;

            if (panelRevealRoutine != null)
                StopCoroutine(panelRevealRoutine);

            panelRevealRoutine = StartCoroutine(PlayDialogueRevealRoutine());
        }

        private IEnumerator PlayDialogueRevealRoutine()
        {
            float duration = 0.22f;
            float elapsed = 0f;
            Vector2 startPosition = dialoguePanelBasePosition + new Vector2(0f, -18f);
            dialoguePanelRect.anchoredPosition = startPosition;
            dialogueCanvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
                dialoguePanelRect.anchoredPosition = Vector2.Lerp(startPosition, dialoguePanelBasePosition, eased);
                yield return null;
            }

            dialogueCanvasGroup.alpha = 1f;
            dialoguePanelRect.anchoredPosition = dialoguePanelBasePosition;
            panelRevealRoutine = null;
        }

    }
}

