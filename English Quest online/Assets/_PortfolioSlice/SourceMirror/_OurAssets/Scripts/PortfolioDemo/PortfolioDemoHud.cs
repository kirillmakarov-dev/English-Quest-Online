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
        private TextMeshProUGUI completionEyebrowText;
        private TextMeshProUGUI completionTitleText;
        private TextMeshProUGUI completionSupportText;
        private TextMeshProUGUI completionFooterText;
        private RectTransform transitionOverlayRoot;
        private CanvasGroup transitionOverlayCanvasGroup;
        private TextMeshProUGUI transitionOverlayEyebrowText;
        private TextMeshProUGUI transitionOverlayTitleText;
        private TextMeshProUGUI transitionOverlayBodyText;
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
            ResolveSceneBindings();
            EnsureFlowCoordinator();
            EnsureDialogueThemeBridge();
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
                headerCardRoot.sizeDelta = new Vector2(900f, 146f);

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
            titleText.fontSize = 30f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.97f, 0.98f, 1f, 1f);
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.lineSpacing = -10f;
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(0f, 1f);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(24f, -34f);
            titleText.rectTransform.sizeDelta = new Vector2(790f, 66f);

            controlsText.text =
                "3 learning stages · Optional 2 Players via Photon Fusion\nWASD Move · Mouse Look · Space Jump · E Interact";
            controlsText.fontSize = 17f;
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
            playerPanelRoot.sizeDelta = new Vector2(356f, 188f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.78f);
            PortfolioThemeResources.ApplyPanelSprite(panelBackground, PortfolioThemeResources.PlayerCardSprite);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 12);
            layout.spacing = 8f;
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
            demoBriefingRoot.sizeDelta = new Vector2(520f, 236f);

            Image panelBackground = panel.AddComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.78f);
            panelBackground.raycastTarget = false;
            PortfolioThemeResources.ApplyPanelSprite(panelBackground, PortfolioThemeResources.BriefingCardSprite);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.spacing = 9f;
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
            completionPanelRoot.sizeDelta = new Vector2(960f, 500f);

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

            completionEyebrowText = CreateText(
                "Eyebrow",
                panel.transform,
                "PORTFOLIO DEMO COMPLETE",
                14f,
                FontStyles.Bold,
                new Color(0.98f, 0.82f, 0.36f, 1f),
                TextAlignmentOptions.Center);

            completionTitleText = CreateText(
                "Title",
                panel.transform,
                "English Quest MVP Finished",
                34f,
                FontStyles.Bold,
                new Color(0.98f, 0.99f, 1f, 1f),
                TextAlignmentOptions.Center,
                wrap: true);

            completionSupportText = CreateText(
                "Support",
                panel.transform,
                "",
                18f,
                FontStyles.Normal,
                new Color(0.56f, 0.95f, 0.82f, 1f),
                TextAlignmentOptions.Center);

            completionBodyText = CreateText(
                "Body",
                panel.transform,
                "",
                21f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Center,
                wrap: true);
            completionBodyText.lineSpacing = 8f;

            completionFooterText = CreateText(
                "Footer",
                panel.transform,
                "",
                15f,
                FontStyles.Italic,
                new Color(0.79f, 0.88f, 0.93f, 0.95f),
                TextAlignmentOptions.Center,
                wrap: true);
            completionFooterText.lineSpacing = 5f;

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
            replayButton.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 72f);
            closeCompletionButton.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 72f);
            PortfolioThemeResources.ApplyPrimaryButtonStyle(replayButton);
            PortfolioThemeResources.ApplySecondaryButtonStyle(closeCompletionButton);

            panel.SetActive(false);
        }

        private void EnsureTransitionOverlay()
        {
            if (transitionOverlayRoot != null)
                return;

            Transform host = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                host = canvas.transform;

            GameObject overlay = new GameObject("MiniGame Transition Overlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(host, false);

            transitionOverlayRoot = overlay.GetComponent<RectTransform>();
            transitionOverlayRoot.StretchToParent();
            transitionOverlayRoot.SetAsLastSibling();

            Image background = overlay.GetComponent<Image>();
            background.color = new Color(0.03f, 0.06f, 0.08f, 0.86f);
            background.raycastTarget = false;

            transitionOverlayCanvasGroup = overlay.GetComponent<CanvasGroup>();
            transitionOverlayCanvasGroup.alpha = 0f;
            transitionOverlayCanvasGroup.interactable = false;
            transitionOverlayCanvasGroup.blocksRaycasts = false;

            GameObject card = new GameObject("Transition Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(overlay.transform, false);

            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 240f);

            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.02f, 0.05f, 0.07f, 0.94f);
            PortfolioThemeResources.ApplyPanelSprite(cardImage, PortfolioThemeResources.DialogueCardSprite);

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 30, 30);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            transitionOverlayEyebrowText = CreateText(
                "Eyebrow",
                card.transform,
                "LESSON TRANSITION",
                14f,
                FontStyles.Bold,
                new Color(0.98f, 0.82f, 0.36f, 1f),
                TextAlignmentOptions.Center);

            transitionOverlayTitleText = CreateText(
                "Title",
                card.transform,
                "Preparing Lesson",
                34f,
                FontStyles.Bold,
                new Color(0.98f, 0.99f, 1f, 1f),
                TextAlignmentOptions.Center,
                wrap: true);

            transitionOverlayBodyText = CreateText(
                "Body",
                card.transform,
                "",
                18f,
                FontStyles.Normal,
                new Color(0.78f, 0.9f, 0.95f, 0.98f),
                TextAlignmentOptions.Center,
                wrap: true);
            transitionOverlayBodyText.lineSpacing = 6f;

            overlay.SetActive(false);
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
                PortfolioThemeResources.ApplyPanelSprite(rowBackground, PortfolioThemeResources.SecondaryButtonSprite, new Color(1f, 1f, 1f, 0.09f));
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
            if (completionBodyText == null || completionTitleText == null || completionSupportText == null || completionFooterText == null)
                return;

            completionTitleText.text = "English Quest MVP Finished";
            completionSupportText.text = "Three connected learning beats are now fully playable in one clean open-world slice.";
            completionBodyText.text =
                "Teacher Ada introduced the first letters.\n" +
                "Coach Ben reinforced vocabulary through missing letters.\n" +
                "Guide Nora completed the flow with the final lesson.";
            completionFooterText.text =
                "This is a portfolio prototype: solo completion remains valid, multiplayer stays optional, and you can restart from the beginning to replay the full flow.";
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

            if (completionPanelRoutine != null)
                StopCoroutine(completionPanelRoutine);

            if (isVisible)
            {
                completionPanelRoot.SetAsLastSibling();
                completionPanelRoot.gameObject.SetActive(true);
                AcquireCompletionInteraction();
                completionPanelRoutine = StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 1f, 0.24f, deactivateOnComplete: false));
                return;
            }

            ReleaseCompletionInteraction();
            completionPanelRoutine = StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 0f, 0.18f, deactivateOnComplete: true, completionPanelRoot.gameObject));
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

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

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

            transitionOverlayTitleText.text = title;
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
        private CanvasGroup dialogueCanvasGroup;
        private RectTransform dialoguePanelRect;
        private Vector2 dialoguePanelBasePosition;
        private TextMeshProUGUI subtitleText;
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

            if (dialogueManager == null || dialogueManager.choiceContainer == null)
                return;

            string currentSpeakerName = dialogueManager.nameText != null ? dialogueManager.nameText.text : string.Empty;
            if (currentSpeakerName != lastSpeakerName)
            {
                lastSpeakerName = currentSpeakerName;
                UpdateSubtitle(currentSpeakerName);
            }

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

            if (dialogueManager.nameText != null)
            {
                dialogueManager.nameText.fontSize = 28f;
                dialogueManager.nameText.alignment = TextAlignmentOptions.TopLeft;
            }

            if (dialogueManager.dialogueText != null)
            {
                dialogueManager.dialogueText.fontSize = 24f;
                dialogueManager.dialogueText.lineSpacing = 6f;
                dialogueManager.dialogueText.color = new Color(0.96f, 0.98f, 1f, 1f);
                dialogueManager.dialogueText.rectTransform.anchoredPosition = new Vector2(40f, -118f);
                dialogueManager.dialogueText.rectTransform.sizeDelta = new Vector2(920f, 116f);
            }

            UpdateSubtitle(dialogueManager.nameText != null ? dialogueManager.nameText.text : string.Empty);

            StyleChoiceButtons();
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
            if (dialogueCanvasGroup == null)
                dialogueCanvasGroup = dialogueManager.dialoguePanel.AddComponent<CanvasGroup>();

            if (subtitleText == null)
            {
                GameObject subtitleObject = new GameObject("Portfolio Subtitle", typeof(RectTransform));
                subtitleObject.transform.SetParent(dialogueManager.dialoguePanel.transform, false);
                subtitleText = subtitleObject.AddComponent<TextMeshProUGUI>();
                subtitleText.fontSize = 14f;
                subtitleText.fontStyle = FontStyles.Bold;
                subtitleText.color = new Color(0.56f, 0.95f, 0.82f, 0.98f);
                subtitleText.alignment = TextAlignmentOptions.TopLeft;
                subtitleText.raycastTarget = false;
                subtitleText.textWrappingMode = TextWrappingModes.NoWrap;

                RectTransform rect = subtitleText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(40f, -66f);
                rect.sizeDelta = new Vector2(900f, 24f);
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

