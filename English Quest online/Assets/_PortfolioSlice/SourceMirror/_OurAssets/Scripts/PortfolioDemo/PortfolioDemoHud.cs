using EnglishQuest.QuestSystem;
using Fusion;
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
        private readonly List<Image> playerRowBackgrounds = new();

        private RectTransform headerCardRoot;
        private RectTransform demoBriefingRoot;
        private TextMeshProUGUI demoBriefingTitleText;
        private TextMeshProUGUI demoBriefingBodyText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI controlsText;
        private TextMeshProUGUI headerEyebrowText;
        private RectTransform playerPanelRoot;
        private RectTransform playerListRoot;
        private TextMeshProUGUI playerPanelTitleText;
        private TextMeshProUGUI playerPanelSupportText;
        private RectTransform completionPanelRoot;
        private float nextPlayerPanelRefreshTime;
        private string lastPlayerPanelHash = "";
        private string lastBriefingHash = "";
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
        private PortfolioOptionalCoopStudyCircle optionalCoopStudyCircle;

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
            ResolveSceneBindings();
            EnsureFlowCoordinator();
            EnsureDialogueThemeBridge();
            EnsureHeaderPresentation();
            EnsureDemoBriefingPanel();
            EnsurePlayerPanel();
            EnsureCompletionPanel();
            EnsureDebugOverlay();
            EnsureOptionalCoopStudyCircle();
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

        private void EnsureDialogueThemeBridge()
        {
            PortfolioDialogueVisualBridge bridge = FindFirstObjectByType<PortfolioDialogueVisualBridge>(FindObjectsInactive.Include);
            if (bridge != null)
                return;

            GameObject host = dialogueManager != null ? dialogueManager.gameObject : gameObject;
            bridge = host.GetComponent<PortfolioDialogueVisualBridge>();
            if (bridge == null)
                host.AddComponent<PortfolioDialogueVisualBridge>();
        }

        private void EnsureOptionalCoopStudyCircle()
        {
            if (optionalCoopStudyCircle == null)
                optionalCoopStudyCircle = PortfolioOptionalCoopStudyCircle.FindOrCreateRuntimeInstance();
        }

        private void EnsureHeaderPresentation()
        {
            ResolveSceneBindings();
            if (titleText == null || controlsText == null)
                return;

            Transform host = titleText.transform.parent;
            if (headerCardRoot == null)
            {
                GameObject card = new GameObject("Header Presentation Card", typeof(RectTransform));
                card.transform.SetParent(host, false);

                headerCardRoot = card.GetComponent<RectTransform>();
                headerCardRoot.anchorMin = new Vector2(0f, 1f);
                headerCardRoot.anchorMax = new Vector2(0f, 1f);
                headerCardRoot.pivot = new Vector2(0f, 1f);
                headerCardRoot.anchoredPosition = new Vector2(16f, -16f);
                headerCardRoot.sizeDelta = new Vector2(860f, 136f);

                Image background = card.AddComponent<Image>();
                background.color = new Color(0.02f, 0.05f, 0.07f, 0.78f);
                background.raycastTarget = false;
                PortfolioThemeResources.ApplyPanelSprite(background, PortfolioThemeResources.HeaderCardSprite);

                GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(card.transform, false);

                RectTransform accentRect = accent.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(0f, 5f);

                Image accentImage = accent.GetComponent<Image>();
                accentImage.color = new Color(0.98f, 0.82f, 0.34f, 0.95f);
                accentImage.raycastTarget = false;

                headerEyebrowText = CreateText(
                    "Eyebrow",
                    card.transform,
                    "PORTFOLIO MVP SLICE",
                    12f,
                    FontStyles.Bold,
                    new Color(0.94f, 0.8f, 0.35f, 1f),
                    TextAlignmentOptions.TopLeft);
                headerEyebrowText.rectTransform.anchorMin = new Vector2(0f, 1f);
                headerEyebrowText.rectTransform.anchorMax = new Vector2(0f, 1f);
                headerEyebrowText.rectTransform.pivot = new Vector2(0f, 1f);
                headerEyebrowText.rectTransform.anchoredPosition = new Vector2(24f, -14f);
                headerEyebrowText.rectTransform.sizeDelta = new Vector2(260f, 24f);
            }

            if (titleText.transform.parent != headerCardRoot)
                titleText.transform.SetParent(headerCardRoot, false);

            if (controlsText.transform.parent != headerCardRoot)
                controlsText.transform.SetParent(headerCardRoot, false);

            if (statusText != null && statusText.transform.parent != headerCardRoot)
                statusText.transform.SetParent(headerCardRoot, false);

            titleText.text = "ENGLISH QUEST ONLINE\nOpen-World Quest Portfolio Slice";
            titleText.fontSize = 28f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.97f, 0.98f, 1f, 1f);
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(0f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(24f, -34f);
            titleText.rectTransform.sizeDelta = new Vector2(760f, 62f);

            controlsText.text =
                "3 learning stages · Optional 2 Players via Photon Fusion\nWASD Move · Mouse Look · Space Jump · E Interact";
            controlsText.fontSize = 16f;
            controlsText.fontStyle = FontStyles.Normal;
            controlsText.color = new Color(0.8f, 0.91f, 0.95f, 0.96f);
            controlsText.alignment = TextAlignmentOptions.TopLeft;
            controlsText.textWrappingMode = TextWrappingModes.Normal;
            controlsText.rectTransform.anchorMin = new Vector2(0f, 1f);
            controlsText.rectTransform.anchorMax = new Vector2(0f, 1f);
            controlsText.rectTransform.pivot = new Vector2(0f, 1f);
            controlsText.rectTransform.anchoredPosition = new Vector2(24f, -92f);
            controlsText.rectTransform.sizeDelta = new Vector2(780f, 38f);

            if (statusText != null)
            {
                statusText.fontSize = 14f;
                statusText.fontStyle = FontStyles.Italic;
                statusText.color = new Color(0.56f, 0.95f, 0.82f, 0.95f);
                statusText.alignment = TextAlignmentOptions.TopLeft;
                statusText.textWrappingMode = TextWrappingModes.Normal;
                statusText.rectTransform.anchorMin = new Vector2(0f, 1f);
                statusText.rectTransform.anchorMax = new Vector2(0f, 1f);
                statusText.rectTransform.pivot = new Vector2(0f, 1f);
                statusText.rectTransform.anchoredPosition = new Vector2(24f, -118f);
                statusText.rectTransform.sizeDelta = new Vector2(760f, 24f);
            }

            headerCardRoot.SetSiblingIndex(0);
            titleText.transform.SetAsLastSibling();
            controlsText.transform.SetAsLastSibling();
            statusText?.transform.SetAsLastSibling();
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
            playerPanelRoot.anchoredPosition = new Vector2(24f, -164f);
            playerPanelRoot.sizeDelta = new Vector2(340f, 172f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.78f);
            PortfolioThemeResources.ApplyPanelSprite(panelBackground, PortfolioThemeResources.PlayerCardSprite);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            playerPanelTitleText = CreateText(
                "Title",
                panel.transform,
                PortfolioPlayerStatusFormatter.SoloPlayerPanelTitle,
                17f,
                FontStyles.Bold,
                new Color(0.86f, 0.97f, 1f, 1f),
                TextAlignmentOptions.Left);

            playerPanelSupportText = CreateText(
                "Support",
                panel.transform,
                PortfolioPlayerStatusFormatter.GetPlayerPanelSupportText(connectedPlayerCount: 1),
                12f,
                FontStyles.Normal,
                new Color(0.78f, 0.87f, 0.9f, 0.92f),
                TextAlignmentOptions.Left,
                wrap: true);

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
            demoBriefingRoot.sizeDelta = new Vector2(500f, 220f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.78f);
            panelBackground.raycastTarget = false;
            PortfolioThemeResources.ApplyPanelSprite(panelBackground, PortfolioThemeResources.BriefingCardSprite);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(
                "Section Label",
                panel.transform,
                "MISSION GUIDE",
                12f,
                FontStyles.Bold,
                new Color(0.96f, 0.84f, 0.38f, 1f),
                TextAlignmentOptions.Left);

            CreateText(
                "Title",
                panel.transform,
                "English Quest Portfolio Demo",
                18f,
                FontStyles.Bold,
                new Color(0.97f, 0.98f, 1f, 1f),
                TextAlignmentOptions.Left);

            demoBriefingTitleText = CreateText(
                "Current Step",
                panel.transform,
                "Next Step",
                16f,
                FontStyles.Bold,
                new Color(0.55f, 0.95f, 0.8f, 1f),
                TextAlignmentOptions.Left,
                wrap: true);

            demoBriefingBodyText = CreateText(
                "Body",
                panel.transform,
                "Open-world English quest slice. Talk to the active NPC, complete the unlocked lesson, and move through the chain from letters to words to a full sentence.\nSolo play is fully supported. A second player is optional and only adds shared presence.",
                14f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Left,
                wrap: true);
            demoBriefingBodyText.lineSpacing = 8f;

            RefreshDemoBriefing(force: true);
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
            PortfolioThemeResources.ApplyPanelSprite(background, PortfolioThemeResources.CompletionCardSprite);

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
            PortfolioThemeResources.ApplyPrimaryButtonStyle(replayButton);
            PortfolioThemeResources.ApplySecondaryButtonStyle(closeCompletionButton);

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
            EnsurePlayerRows(entries.Count);

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
            EnsureDemoBriefingPanel();

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

        private void EnsurePlayerRows(int count)
        {
            while (playerRows.Count < count)
            {
                int rowIndex = playerRows.Count;
                GameObject row = new GameObject($"Player Row {rowIndex + 1}", typeof(RectTransform));
                row.transform.SetParent(playerListRoot, false);

                Image rowBackground = row.AddComponent<Image>();
                rowBackground.color = new Color(1f, 1f, 1f, 0.08f);
                playerRowBackgrounds.Add(rowBackground);

                HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(8, 8, 6, 6);
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
                    TextAlignmentOptions.Left,
                    wrap: true);
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
            {
                RefreshDemoBriefing(force: true);
                RefreshPlayerPanel(force: true);
            }
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
            if (completionBodyText == null)
                return;

            completionBodyText.text =
                "You finished the full English Quest MVP slice.\n\n" +
                "Teacher Ada introduced the first letters.\n" +
                "Coach Ben reinforced vocabulary through missing letters.\n" +
                "Guide Nora completed the flow with the final lesson.\n\n" +
                "This prototype remains solo-first: one player can complete the whole chain alone, and any second player is optional.\n\n" +
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

    /// <summary>
    /// Runtime styling bridge for the portfolio dialogue UI.
    /// Keeps the portfolio presentation layer outside of DialogueManager core logic.
    /// </summary>
    public sealed class PortfolioDialogueVisualBridge : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;

        private int styledChoiceCount = -1;

        private void Awake()
        {
            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);

            ApplyDialogueTheme();
        }

        private void OnEnable()
        {
            ApplyDialogueTheme();
        }

        private void Update()
        {
            if (dialogueManager == null || dialogueManager.choiceContainer == null)
                return;

            int currentCount = dialogueManager.choiceContainer.childCount;
            if (currentCount == styledChoiceCount)
                return;

            styledChoiceCount = currentCount;
            StyleChoiceButtons();
        }

        private void ApplyDialogueTheme()
        {
            if (dialogueManager == null || dialogueManager.dialoguePanel == null)
                return;

            Image panelImage = dialogueManager.dialoguePanel.GetComponent<Image>();
            PortfolioThemeResources.ApplyDialogueSurface(
                panelImage,
                dialogueManager.nameText,
                dialogueManager.dialogueText);

            StyleChoiceButtons();
        }

        private void StyleChoiceButtons()
        {
            if (dialogueManager == null || dialogueManager.choiceContainer == null)
                return;

            int total = dialogueManager.choiceContainer.childCount;
            for (int i = 0; i < total; i++)
            {
                Transform child = dialogueManager.choiceContainer.GetChild(i);
                if (child == null)
                    continue;

                Button button = child.GetComponent<Button>();
                if (button == null)
                    continue;

                bool isPrimary = i == 0 && total == 1;
                if (isPrimary)
                    PortfolioThemeResources.ApplyPrimaryButtonStyle(button);
                else
                    PortfolioThemeResources.ApplySecondaryButtonStyle(button);

                TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.alignment = TextAlignmentOptions.Center;
            }
        }
    }
}

