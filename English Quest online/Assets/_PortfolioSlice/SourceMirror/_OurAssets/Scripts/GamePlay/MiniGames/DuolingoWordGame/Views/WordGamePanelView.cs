using System;
using System.Collections.Generic;
using Puzzle.Gameplay.Features.WordReveal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Root view for the word game panel.
    /// Owns the prompt label, slot container, and tile container.
    /// WordGameBootstrap (GameplayUIBase) owns player locking; this class only manages visuals.
    /// </summary>
    public class WordGamePanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _promptLabel;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private Transform _tileContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Canvas _screenCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private int _openSortingOrder = 100;

        /// <summary>Raised when the player presses the close button.</summary>
        public event Action CloseRequested;

        private readonly List<SlotView> _slotViews = new List<SlotView>();
        private readonly List<TileView> _tileViews = new List<TileView>();

        private void Awake()
        {
            if (_screenCanvas == null)
                _screenCanvas = GetComponentInParent<Canvas>(true);

            ApplyOpenState(gameObject.activeInHierarchy);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void OnEnable() => ApplyOpenState(true);

        private void OnDisable() => ApplyOpenState(false);

        public IReadOnlyList<SlotView> SlotViews => _slotViews;
        public IReadOnlyList<TileView> TileViews => _tileViews;

        /// <summary>
        /// Clears all children and spawns new SlotViews and TileViews via the factory.
        /// Call once when a new game round begins.
        /// </summary>
        public void Build(string prompt, SlotDefinition[] slots, TileDefinition[] tiles, IWordGameViewFactory factory)
        {
            ClearChildren();

            if (_promptLabel != null)
                _promptLabel.text = prompt;

            for (int i = 0; i < slots.Length; i++)
            {
                SlotView slotView = factory.CreateSlot(slots[i], i, _slotContainer);
                _slotViews.Add(slotView);
            }

            foreach (TileDefinition tileDef in tiles)
            {
                TileView tileView = factory.CreateTile(tileDef, _tileContainer);
                _tileViews.Add(tileView);
            }
        }

        /// <summary>
        /// Forwards a word-reveal database to the <see cref="WordRevealSetup"/> sitting on
        /// the prompt label's GameObject. Call this whenever the step changes so the
        /// tap-to-translate feature uses the correct database for the current prompt.
        /// Passing null disables word reveal for the current step.
        /// </summary>
        public void SetWordRevealDatabase(WordTranslationDatabaseSO database)
        {
            if (_promptLabel == null) return;
            _promptLabel.GetComponent<WordRevealSetup>()?.SetDatabase(database);
        }

        public void Open()
        {
            EnsureHierarchyActive();
            EnsureCanvasReady(true);
            gameObject.SetActive(true);
            ApplyOpenState(true);
        }

        public void Close()
        {
            EnsureCanvasReady(false);
            ApplyOpenState(false);
            gameObject.SetActive(false);
        }

        private void ApplyOpenState(bool isOpen)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = isOpen ? 1f : 0f;
            _canvasGroup.interactable = isOpen;
            _canvasGroup.blocksRaycasts = isOpen;
        }

        private void EnsureHierarchyActive()
        {
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                current = current.parent;
            }
        }

        private void EnsureCanvasReady(bool isOpen)
        {
            if (_screenCanvas == null)
                _screenCanvas = GetComponentInParent<Canvas>(true);

            if (_screenCanvas == null)
                return;

            _screenCanvas.enabled = true;
            _screenCanvas.overrideSorting = true;
            _screenCanvas.sortingOrder = _openSortingOrder;

            GraphicRaycaster raycaster = _screenCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = isOpen;
        }

        private void ClearChildren()
        {
            _slotViews.Clear();
            _tileViews.Clear();

            foreach (Transform child in _slotContainer)
                Destroy(child.gameObject);

            foreach (Transform child in _tileContainer)
                Destroy(child.gameObject);
        }
    }
}
