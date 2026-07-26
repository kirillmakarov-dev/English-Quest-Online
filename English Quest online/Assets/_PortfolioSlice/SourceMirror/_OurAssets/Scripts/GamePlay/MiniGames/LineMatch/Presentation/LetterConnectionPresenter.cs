using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionPresenter : IDisposable
    {
        private readonly LetterConnectionScreenView screenView;
        private readonly ILetterConnectionViewFactory viewFactory;
        private readonly IConnectionValidationStrategy validationStrategy;
        private readonly ICompletionChecker completionChecker;
        private readonly LevelSession levelSession;
        private readonly LetterConnectionLevelConfigSO levelConfig;
        private readonly Dictionary<string, LetterItemView> letterViewsById = new Dictionary<string, LetterItemView>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WordSlotView> wordSlotViewsById = new Dictionary<string, WordSlotView>(StringComparer.OrdinalIgnoreCase);

        private LetterItemView activeDraggedLetterView;
        private WordSlotView highlightedSlotView;
        private bool isLevelCompleted;

        public LetterConnectionPresenter(LetterConnectionScreenView screenView, ILetterConnectionViewFactory viewFactory,
            IConnectionValidationStrategy validationStrategy, ICompletionChecker completionChecker,
            LevelSession levelSession, LetterConnectionLevelConfigSO levelConfig)
        {
            this.screenView = screenView;
            this.viewFactory = viewFactory;
            this.validationStrategy = validationStrategy;
            this.completionChecker = completionChecker;
            this.levelSession = levelSession;
            this.levelConfig = levelConfig;
        }

        public event Action LevelCompleted;
        public event Action Hidden;

        public void Initialize()
        {
            UnbindEvents();
            isLevelCompleted = false;

            if (screenView != null)
            {
                screenView.Build(levelConfig, viewFactory);
            }

            CacheViews();
            BindEvents();
            RefreshAllViews();
        }

        public void Show()
        {
            screenView?.Open();
            RefreshAllViews();
        }

        public void Hide()
        {
            CancelCurrentDrag();
            screenView?.Close();
            Hidden?.Invoke();
        }

        public void ResetSession()
        {
            CancelCurrentDrag();
            isLevelCompleted = false;
            levelSession?.Reset();
            RefreshAllViews();
        }

        public void Dispose()
        {
            UnbindEvents();
            CancelCurrentDrag();

            levelSession?.Dispose();

            letterViewsById.Clear();
            wordSlotViewsById.Clear();
        }

        private void CacheViews()
        {
            letterViewsById.Clear();
            wordSlotViewsById.Clear();

            if (screenView == null)
            {
                return;
            }

            for (int i = 0; i < screenView.LetterViews.Count; i++)
            {
                LetterItemView view = screenView.LetterViews[i];
                if (view == null || string.IsNullOrWhiteSpace(view.Id) || letterViewsById.ContainsKey(view.Id))
                {
                    continue;
                }

                letterViewsById.Add(view.Id, view);
            }

            for (int i = 0; i < screenView.WordSlotViews.Count; i++)
            {
                WordSlotView view = screenView.WordSlotViews[i];
                if (view == null || string.IsNullOrWhiteSpace(view.Id) || wordSlotViewsById.ContainsKey(view.Id))
                {
                    continue;
                }

                wordSlotViewsById.Add(view.Id, view);
            }
        }

        private void BindEvents()
        {
            if (screenView != null)
            {
                screenView.CloseRequested += HandleCloseRequested;
                screenView.RestartRequested += HandleRestartRequested;

                for (int i = 0; i < screenView.LetterViews.Count; i++)
                {
                    LetterItemView letterView = screenView.LetterViews[i];
                    if (letterView == null)
                    {
                        continue;
                    }

                    letterView.DragStarted += HandleDragStarted;
                    letterView.Dragged += HandleDragged;
                    letterView.DragEnded += HandleDragEnded;
                }
            }

            if (levelSession != null)
            {
                levelSession.SessionChanged += HandleSessionChanged;
                levelSession.OnConnectionSucceeded += HandleConnectionSucceeded;
                levelSession.OnConnectionFailed += HandleConnectionFailed;
                levelSession.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        private void UnbindEvents() 
        {
            if (screenView != null)
            {
                screenView.CloseRequested -= HandleCloseRequested;
                screenView.RestartRequested -= HandleRestartRequested;

                for (int i = 0; i < screenView.LetterViews.Count; i++)
                {
                    LetterItemView letterView = screenView.LetterViews[i];
                    if (letterView == null)
                    {
                        continue;
                    }

                    letterView.DragStarted -= HandleDragStarted;
                    letterView.Dragged -= HandleDragged;
                    letterView.DragEnded -= HandleDragEnded;
                }
            }

            if (levelSession != null)
            {
                levelSession.SessionChanged -= HandleSessionChanged;
                levelSession.OnConnectionSucceeded -= HandleConnectionSucceeded;
                levelSession.OnConnectionFailed -= HandleConnectionFailed;
                levelSession.OnLevelCompleted -= HandleLevelCompleted;
            }
        }

        private void HandleCloseRequested()
        {
            Hide();
        }

        private void HandleRestartRequested()
        {
            screenView?.ClearLines();
            ResetSession();
            screenView?.Open();
        }

        private void HandleDragStarted(LetterItemView letterView, PointerEventData eventData)
        {
            if (letterView == null || eventData == null)
            {
                return;
            }

            LetterState letterState = GetLetterState(letterView.Id);
            if (letterState == null || letterState.IsUsed)
            {
                screenView?.CancelTemporaryLine();
                return;
            }

            activeDraggedLetterView = letterView;
            screenView?.BeginTemporaryLine(letterView);
            screenView?.UpdateTemporaryLine(eventData.position);
        }

        private void HandleDragged(LetterItemView letterView, PointerEventData eventData)
        {
            if (activeDraggedLetterView == null || activeDraggedLetterView != letterView || eventData == null)
            {
                return;
            }

            screenView?.UpdateTemporaryLine(eventData.position);
            UpdateSlotHighlight(screenView != null ? screenView.GetSlotUnderPointer(eventData.position) : null);
        }

        private void HandleDragEnded(LetterItemView letterView, PointerEventData eventData)
        {
            if (activeDraggedLetterView == null || activeDraggedLetterView != letterView || eventData == null)
            {
                CancelCurrentDrag();
                return;
            }

            WordSlotView slotView = screenView != null ? screenView.GetSlotUnderPointer(eventData.position) : null;

            if (!TryHandleConnection(letterView, slotView))
            {
                screenView?.CancelTemporaryLine();
            }

            ClearSlotHighlight();
            activeDraggedLetterView = null;
        }

        private bool TryHandleConnection(LetterItemView letterView, WordSlotView slotView)
        {
            if (letterView == null || slotView == null || levelSession == null || validationStrategy == null)
            {
                return false;
            }

            LetterState letterState = GetLetterState(letterView.Id);
            WordSlotState slotState = GetWordSlotState(slotView.Id);

            if (letterState == null || slotState == null)
            {
                return false;
            }

            if (!validationStrategy.IsValid(letterState, slotState))
            {
                levelSession.NotifyConnectionFailed(letterState, slotState);
                return false;
            }

            return levelSession.TryConnect(letterState.LetterId, slotState.SlotId);
        }

        private void HandleConnectionSucceeded(LetterState letterState, WordSlotState slotState)
        {
            if (screenView == null || letterState == null || slotState == null)
            {
                return;
            }

            if (letterViewsById.TryGetValue(letterState.LetterId, out LetterItemView letterView))
            {
                letterView.SetUsed(true);
            }

            if (wordSlotViewsById.TryGetValue(slotState.SlotId, out WordSlotView slotView))
            {
                slotView.InsertLetterVisual(slotState.GetDisplayWord());

                if (letterView != null)
                {
                    screenView.CommitConnection(letterView, slotView);
                }
            }

            EvaluateCompletion();
        }

        private void HandleConnectionFailed(LetterState _, WordSlotState slotState)
        {
            screenView?.CancelTemporaryLine();

            if (slotState != null && wordSlotViewsById.TryGetValue(slotState.SlotId, out WordSlotView slotView))
            {
                screenView?.PlayWrongMatchFeedback(slotView);
            }
        }

        private void HandleLevelCompleted()
        {
            EvaluateCompletion();
        }

        private void HandleSessionChanged()
        {
            RefreshAllViews();
        }

        private void RefreshAllViews()
        {
            if (levelSession == null)
            {
                return;
            }

            for (int i = 0; i < levelSession.Letters.Count; i++)
            {
                LetterState state = levelSession.Letters[i];
                if (state == null || !letterViewsById.TryGetValue(state.LetterId, out LetterItemView letterView))
                {
                    continue;
                }

                letterView.Bind(state.LetterId, state.Value, state.IsUsed);
            }

            for (int i = 0; i < levelSession.WordSlots.Count; i++)
            {
                WordSlotState state = levelSession.WordSlots[i];
                if (state == null || !wordSlotViewsById.TryGetValue(state.SlotId, out WordSlotView slotView))
                {
                    continue;
                }

                slotView.SetWordText(state.GetDisplayWord());
            }
        }

        private void EvaluateCompletion()
        {
            if (isLevelCompleted || completionChecker == null || levelSession == null)
            {
                return;
            }

            if (!completionChecker.IsCompleted(levelSession))
            {
                return;
            }

            isLevelCompleted = true;
            screenView?.ShowCompleted();
            LevelCompleted?.Invoke();
        }

        private void UpdateSlotHighlight(WordSlotView slotView)
        {
            if (highlightedSlotView == slotView)
            {
                return;
            }

            ClearSlotHighlight();
            highlightedSlotView = slotView;

            if (highlightedSlotView != null)
            {
                highlightedSlotView.SetHighlighted(true);
            }
        }

        private void ClearSlotHighlight()
        {
            if (highlightedSlotView == null)
            {
                return;
            }

            highlightedSlotView.SetHighlighted(false);
            highlightedSlotView = null;
        }

        private void CancelCurrentDrag()
        {
            activeDraggedLetterView = null;
            ClearSlotHighlight();
            screenView?.CancelTemporaryLine();
        }

        private LetterState GetLetterState(string letterId)
        {
            return levelSession?.GetLetterState(letterId);
        }

        private WordSlotState GetWordSlotState(string slotId)
        {
            return levelSession?.GetWordSlotState(slotId);
        }
    }
}
