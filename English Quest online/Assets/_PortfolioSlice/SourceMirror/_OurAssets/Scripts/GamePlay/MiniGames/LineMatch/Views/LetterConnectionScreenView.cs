using System;
using System.Collections.Generic;
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

        private readonly List<LetterItemView> letterViews = new List<LetterItemView>();
        private readonly List<WordSlotView> wordSlotViews = new List<WordSlotView>();
        private readonly List<ConnectionLineView> committedLines = new List<ConnectionLineView>();

        private ILetterConnectionViewFactory viewFactory;
        private ConnectionLineView temporaryLine;
        private Canvas parentCanvas;

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
            EnsureCanvasReady(gameObject.activeInHierarchy);
            ApplyCanvasGroupState(gameObject.activeInHierarchy);

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
            ApplyCanvasGroupState(true);
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
            ApplyCanvasGroupState(true);

            if (completePanel != null)
            {
                completePanel.SetActive(false);
            }
        }

        public void Close()
        {
            CancelTemporaryLine();
            EnsureCanvasReady(false);

            if (rootCanvasGroup != null)
            {
                ApplyCanvasGroupState(false);
            }

            gameObject.SetActive(false);
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
