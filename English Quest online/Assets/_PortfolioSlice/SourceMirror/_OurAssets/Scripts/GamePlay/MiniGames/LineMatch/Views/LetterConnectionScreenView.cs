using System;
using System.Collections;
using System.Collections.Generic;
using EnglishQuest.PortfolioDemo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionScreenView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private Canvas screenCanvas;
        [SerializeField] private RectTransform rootPanel;
        [SerializeField] private RectTransform lettersContainer;
        [SerializeField] private RectTransform wordsContainer;
        [SerializeField] private RectTransform lineLayer;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private int openSortingOrder = 100;
        [SerializeField] private float fadeDuration = 0.18f;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI lettersSectionLabel;
        [SerializeField] private TextMeshProUGUI wordsSectionLabel;

        private readonly List<LetterItemView> letterViews = new List<LetterItemView>();
        private readonly List<WordSlotView> wordSlotViews = new List<WordSlotView>();
        private readonly List<ConnectionLineView> committedLines = new List<ConnectionLineView>();

        private ILetterConnectionViewFactory viewFactory;
        private ConnectionLineView temporaryLine;
        private Canvas parentCanvas;
        private Coroutine fadeRoutine;

        public RectTransform RootPanel => rootPanel != null ? rootPanel : transform as RectTransform;
        public RectTransform LettersContainer => lettersContainer;
        public RectTransform WordsContainer => wordsContainer;
        public RectTransform LinesContainer => lineLayer;
        public IReadOnlyList<LetterItemView> LetterViews => letterViews;
        public IReadOnlyList<WordSlotView> WordSlotViews => wordSlotViews;

        public event Action CloseRequested;
        public event Action RestartRequested;

        private void Awake()
        {
            if (screenCanvas == null)
            {
                screenCanvas = GetComponentInParent<Canvas>(true);
            }

            parentCanvas = GetComponentInParent<Canvas>();
            EnsurePresentationLabels();
            EnsureCanvasReady(gameObject.activeInHierarchy);
            ApplyCanvasGroupState(gameObject.activeInHierarchy);
            ApplyTheme();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (completePanel != null)
            {
                completePanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EnsureCanvasReady(true);
        }

        private void OnDisable()
        {
            ApplyCanvasGroupState(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        public void Open()
        {
            EnsureHierarchyActive();
            EnsureCanvasReady(true);
            gameObject.SetActive(true);
            StartFade(isOpen: true);

            if (completePanel != null)
            {
                completePanel.SetActive(false);
            }
        }

        public void Close()
        {
            CancelTemporaryLine();
            EnsureCanvasReady(false);
            StartFade(isOpen: false);
        }

        public void Show()
        {
            Open();
        }

        public void Hide()
        {
            Close();
        }

        public void ShowCompleted()
        {
            if (completePanel != null)
            {
                completePanel.SetActive(true);
            }
        }

        public void Build(LetterConnectionLevelConfigSO levelConfig, ILetterConnectionViewFactory factory)
        {
            viewFactory = factory;
            Clear();

            if (levelConfig == null || factory == null)
            {
                return;
            }

            for (int i = 0; i < levelConfig.Letters.Count; i++)
            {
                LetterData letterData = levelConfig.Letters[i];
                if (letterData == null)
                {
                    continue;
                }

                LetterItemView letterView = factory.CreateLetterItem(lettersContainer, letterData.Id, letterData.Value, false);
                if (letterView != null)
                {
                    letterViews.Add(letterView);
                }
            }

            for (int i = 0; i < levelConfig.WordTasks.Count; i++)
            {
                WordTaskData wordTaskData = levelConfig.WordTasks[i];
                if (wordTaskData == null)
                {
                    continue;
                }

                WordSlotView slotView = factory.CreateWordSlot(wordsContainer, wordTaskData.Id, wordTaskData.GetMaskedWord(), wordTaskData.Image);
                if (slotView != null)
                {
                    wordSlotViews.Add(slotView);
                }
            }
        }

        public void Clear()
        {
            CancelTemporaryLine();

            for (int i = 0; i < committedLines.Count; i++)
            {
                if (committedLines[i] != null)
                {
                    Destroy(committedLines[i].gameObject);
                }
            }

            committedLines.Clear();
            letterViews.Clear();
            wordSlotViews.Clear();

            ClearChildren(lettersContainer);
            ClearChildren(wordsContainer);
            ClearChildren(lineLayer);

            if (completePanel != null)
            {
                completePanel.SetActive(false);
            }
        }

        public void ClearLines()
        {
            CancelTemporaryLine();

            for (int i = 0; i < committedLines.Count; i++)
            {
                if (committedLines[i] != null)
                {
                    Destroy(committedLines[i].gameObject);
                }
            }

            committedLines.Clear();
        }

        public void ClearDynamicContent()
        {
            Clear();
        }

        public void BeginTemporaryLine(LetterItemView letterView)
        {
            CancelTemporaryLine();

            if (letterView == null || viewFactory == null || lineLayer == null)
            {
                return;
            }

            temporaryLine = viewFactory.CreateConnectionLine(lineLayer);
            if (temporaryLine == null)
            {
                return;
            }

            Vector2 startPoint = GetLocalPointFromWorld(letterView.AnchorPoint.position);
            temporaryLine.SetStart(startPoint);
            temporaryLine.SetEnd(startPoint);
            temporaryLine.SetVisible(true);
        }

        public void UpdateTemporaryLine(Vector2 screenPosition)
        {
            if (temporaryLine == null)
            {
                return;
            }

            temporaryLine.SetEnd(GetLocalPointFromScreen(screenPosition));
        }

        public void CancelTemporaryLine()
        {
            if (temporaryLine == null)
            {
                return;
            }

            Destroy(temporaryLine.gameObject);
            temporaryLine = null;
        }

        public void CommitConnection(LetterItemView letterView, WordSlotView slotView)
        {
            if (letterView == null || slotView == null || viewFactory == null || lineLayer == null)
            {
                CancelTemporaryLine();
                return;
            }

            ConnectionLineView lineView = viewFactory.CreateConnectionLine(lineLayer);
            if (lineView != null)
            {
                lineView.SetStart(GetLocalPointFromWorld(letterView.AnchorPoint.position));
                lineView.SetEnd(GetLocalPointFromWorld(slotView.AnchorPoint.position));
                lineView.SetVisible(true);
                committedLines.Add(lineView);
            }

            CancelTemporaryLine();
        }

        public void PlayWrongMatchFeedback(WordSlotView slotView)
        {
            if (slotView != null)
            {
                slotView.PlayWrongFeedback();
            }
        }

        public WordSlotView GetSlotUnderPointer(Vector2 screenPosition)
        {
            Camera eventCamera = GetEventCamera();

            for (int i = 0; i < wordSlotViews.Count; i++)
            {
                WordSlotView slotView = wordSlotViews[i];
                if (slotView != null && slotView.IsPointerOverDropZone(screenPosition, eventCamera))
                {
                    return slotView;
                }
            }

            return null;
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private Vector2 GetLocalPointFromScreen(Vector2 screenPoint)
        {
            return UIPositionUtility.ScreenToLocalPoint(lineLayer, screenPoint, GetEventCamera());
        }

        private Vector2 GetLocalPointFromWorld(Vector3 worldPosition)
        {
            return UIPositionUtility.WorldToLocalPoint(lineLayer, worldPosition, GetEventCamera());
        }

        private Camera GetEventCamera()
        {
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return parentCanvas.worldCamera;
        }

        private void ApplyCanvasGroupState(bool isOpen)
        {
            if (rootCanvasGroup == null)
            {
                return;
            }

            rootCanvasGroup.alpha = isOpen ? 1f : 0f;
            rootCanvasGroup.interactable = isOpen;
            rootCanvasGroup.blocksRaycasts = isOpen;
        }

        private void ApplyTheme()
        {
            if (rootPanel != null)
                PortfolioThemeResources.ApplyPanelSprite(rootPanel.GetComponent<Image>(), PortfolioThemeResources.DialogueCardSprite);

            if (completePanel != null)
                PortfolioThemeResources.ApplyPanelSprite(completePanel.GetComponent<Image>(), PortfolioThemeResources.CompletionCardSprite);

            PortfolioThemeResources.ApplySecondaryButtonStyle(closeButton);
            PortfolioThemeResources.ApplyPrimaryButtonStyle(restartButton);
            PortfolioThemeResources.ApplySectionHeading(titleLabel);
            PortfolioThemeResources.ApplyBodyLabel(subtitleLabel);
            PortfolioThemeResources.ApplySectionHeading(lettersSectionLabel);
            PortfolioThemeResources.ApplySectionHeading(wordsSectionLabel);
        }

        private void EnsurePresentationLabels()
        {
            if (titleLabel == null)
                titleLabel = CreateRuntimeLabel("Lesson Title", new Vector2(34f, -26f), new Vector2(640f, 34f), 16f, FontStyles.Bold, PortfolioThemeResources.WarmHeadingColor);

            if (subtitleLabel == null)
                subtitleLabel = CreateRuntimeLabel("Lesson Subtitle", new Vector2(34f, -58f), new Vector2(700f, 28f), 14f, FontStyles.Normal, PortfolioThemeResources.MutedTextColor);

            if (lettersSectionLabel == null)
                lettersSectionLabel = CreateRuntimeLabel("Letters Label", new Vector2(34f, -108f), new Vector2(220f, 26f), 14f, FontStyles.Bold, PortfolioThemeResources.AccentMintColor);

            if (wordsSectionLabel == null)
                wordsSectionLabel = CreateRuntimeLabel("Words Label", new Vector2(704f, -108f), new Vector2(220f, 26f), 14f, FontStyles.Bold, PortfolioThemeResources.AccentMintColor);

            titleLabel.text = "Lesson Exercise - Match letters to words";
            subtitleLabel.text = "Drag the correct letter to each word and complete the first learning beat.";
            lettersSectionLabel.text = "LETTER BANK";
            wordsSectionLabel.text = "WORD TARGETS";
        }

        private TextMeshProUGUI CreateRuntimeLabel(string name, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style, Color color)
        {
            Transform host = rootPanel != null ? rootPanel : transform;
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(host, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private void StartFade(bool isOpen)
        {
            if (rootCanvasGroup == null || fadeDuration <= 0f)
            {
                ApplyCanvasGroupState(isOpen);
                if (!isOpen)
                    gameObject.SetActive(false);
                return;
            }

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(isOpen));
        }

        private IEnumerator FadeRoutine(bool isOpen)
        {
            float start = rootCanvasGroup.alpha;
            float target = isOpen ? 1f : 0f;
            float elapsed = 0f;

            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                rootCanvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }

            rootCanvasGroup.alpha = target;
            rootCanvasGroup.interactable = isOpen;
            rootCanvasGroup.blocksRaycasts = isOpen;

            if (!isOpen)
                gameObject.SetActive(false);

            fadeRoutine = null;
        }

        private void EnsureHierarchyActive()
        {
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                current = current.parent;
            }
        }

        private void EnsureCanvasReady(bool isOpen)
        {
            if (screenCanvas == null)
            {
                screenCanvas = GetComponentInParent<Canvas>(true);
            }

            if (screenCanvas == null)
            {
                return;
            }

            screenCanvas.enabled = true;
            screenCanvas.overrideSorting = true;
            screenCanvas.sortingOrder = openSortingOrder;

            GraphicRaycaster raycaster = screenCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = isOpen;
            }
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
