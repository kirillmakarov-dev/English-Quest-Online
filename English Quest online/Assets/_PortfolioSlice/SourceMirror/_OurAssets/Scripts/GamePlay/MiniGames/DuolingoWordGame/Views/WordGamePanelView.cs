using System;
using System.Collections;
using System.Collections.Generic;
using EnglishQuest.PortfolioDemo;
using Puzzle.Gameplay.Features.WordReveal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    public class WordGamePanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _promptLabel;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private Transform _tileContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Canvas _screenCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private int _openSortingOrder = 100;
        [SerializeField] private float _fadeDuration = 0.18f;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _subtitleLabel;

        public event Action CloseRequested;

        private readonly List<SlotView> _slotViews = new List<SlotView>();
        private readonly List<TileView> _tileViews = new List<TileView>();
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (_screenCanvas == null)
                _screenCanvas = GetComponentInParent<Canvas>(true);

            ResolveCoreReferences();
            EnsurePresentationLabels();
            ApplyOpenState(false);
            ApplyTheme();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void OnEnable() => EnsureCanvasReady(true);

        private void OnDisable() => ApplyOpenState(false);

        public IReadOnlyList<SlotView> SlotViews => _slotViews;
        public IReadOnlyList<TileView> TileViews => _tileViews;

        public bool Build(string prompt, SlotDefinition[] slots, TileDefinition[] tiles, IWordGameViewFactory factory)
        {
            ResolveCoreReferences(forceRefresh: true);
            ClearChildren();

            if (_slotContainer == null || _tileContainer == null || factory == null ||
                string.IsNullOrWhiteSpace(prompt) || slots == null || slots.Length == 0 || tiles == null || tiles.Length == 0)
            {
                AppLog.Error(
                    $"[WordGamePanelView] Cannot build word game. prompt='{prompt}', slots={(slots != null ? slots.Length : -1)}, tiles={(tiles != null ? tiles.Length : -1)}, promptLabel={(_promptLabel != null)}, slotContainer={(_slotContainer != null)}, tileContainer={(_tileContainer != null)}, factory={(factory != null)}.",
                    this);
                ApplyOpenState(false);
                return false;
            }

            if (_titleLabel != null)
                _titleLabel.text = ResolveTitle(prompt);

            if (_subtitleLabel != null)
                _subtitleLabel.text = ResolveSubtitle(prompt);

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

            return _slotViews.Count > 0 && _tileViews.Count > 0;
        }

        public void SetWordRevealDatabase(WordTranslationDatabaseSO database)
        {
            ResolveCoreReferences();

            if (_promptLabel == null)
                return;

            _promptLabel.GetComponent<WordRevealSetup>()?.SetDatabase(database);
        }

        public void Open()
        {
            EnsureHierarchyActive();
            ResolveCoreReferences();
            EnsureCanvasReady(true);
            gameObject.SetActive(true);
            StartFade(isOpen: true);
        }

        public void Close()
        {
            EnsureCanvasReady(false);

            if (!isActiveAndEnabled)
            {
                ApplyOpenState(false);
                return;
            }

            StartFade(isOpen: false);
        }

        private void ApplyOpenState(bool isOpen)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = isOpen ? 1f : 0f;
            _canvasGroup.interactable = isOpen;
            _canvasGroup.blocksRaycasts = isOpen;
        }

        private void ApplyTheme()
        {
            PortfolioThemeResources.ApplyPanelSprite(GetComponent<Image>(), PortfolioThemeResources.DialogueCardSprite);
            PortfolioThemeResources.ApplySecondaryButtonStyle(_closeButton);

            PortfolioThemeResources.ApplySectionHeading(_titleLabel);
            PortfolioThemeResources.ApplyBodyLabel(_subtitleLabel);

            if (_promptLabel != null)
            {
                _promptLabel.color = PortfolioThemeResources.WarmHeadingColor;
                _promptLabel.fontStyle = FontStyles.Bold;
                _promptLabel.fontSize = Mathf.Max(_promptLabel.fontSize, 32f);
            }
        }

        private void ResolveCoreReferences()
        {
            ResolveCoreReferences(forceRefresh: false);
        }

        private void ResolveCoreReferences(bool forceRefresh)
        {
            if (_promptLabel == null || forceRefresh)
            {
                Transform prompt = FindDescendantByName(transform, "PromptLable") ??
                                   FindDescendantByName(transform, "PromptLabel");
                if (prompt != null)
                    _promptLabel = prompt.GetComponent<TextMeshProUGUI>();
            }

            if (_slotContainer == null || forceRefresh)
            {
                Transform slotContainer = FindDescendantByName(transform, "slot container");
                if (slotContainer != null)
                    _slotContainer = slotContainer;
            }

            if (_tileContainer == null || forceRefresh)
            {
                Transform tileContainer = FindDescendantByName(transform, "tile container");
                if (tileContainer != null)
                    _tileContainer = tileContainer;
            }

            if (_closeButton == null || forceRefresh)
            {
                Transform close = FindDescendantByName(transform, "CloseButton") ??
                                  FindDescendantByName(transform, "Pause Button");
                if (close != null)
                    _closeButton = close.GetComponent<Button>();
            }
        }

        private void EnsurePresentationLabels()
        {
            if (_titleLabel == null)
            {
                _titleLabel = CreateRuntimeLabel(
                    "Lesson Title",
                    new Vector2(36f, -26f),
                    new Vector2(780f, 34f),
                    16f,
                    FontStyles.Bold,
                    PortfolioThemeResources.WarmHeadingColor);
            }

            if (_subtitleLabel == null)
            {
                _subtitleLabel = CreateRuntimeLabel(
                    "Lesson Subtitle",
                    new Vector2(36f, -58f),
                    new Vector2(780f, 28f),
                    14f,
                    FontStyles.Normal,
                    PortfolioThemeResources.MutedTextColor);
            }
        }

        private TextMeshProUGUI CreateRuntimeLabel(string name, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(transform, false);

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

        private static string ResolveTitle(string prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt) && prompt.Contains("_"))
                return "Lesson Exercise - Complete the missing answer";

            return "Lesson Exercise - Arrange the correct answer";
        }

        private static string ResolveSubtitle(string prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt) && prompt.Contains("_"))
                return "Fill the missing piece and reinforce the new vocabulary.";

            return "Read the prompt, place the answer in order, and finish the lesson cleanly.";
        }

        private void StartFade(bool isOpen)
        {
            if (_canvasGroup == null || _fadeDuration <= 0f)
            {
                ApplyOpenState(isOpen);
                if (!isOpen)
                    gameObject.SetActive(false);
                return;
            }

            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeRoutine(isOpen));
        }

        private IEnumerator FadeRoutine(bool isOpen)
        {
            float start = _canvasGroup.alpha;
            float target = isOpen ? 1f : 0f;
            float elapsed = 0f;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }

            _canvasGroup.alpha = target;
            _canvasGroup.interactable = isOpen;
            _canvasGroup.blocksRaycasts = isOpen;

            if (!isOpen)
                gameObject.SetActive(false);

            _fadeRoutine = null;
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

            ClearContainer(_slotContainer);
            ClearContainer(_tileContainer);
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null)
                return;

            var children = new List<GameObject>();
            foreach (Transform child in container)
            {
                if (child != null)
                    children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                if (child == null)
                    continue;

                child.SetActive(false);
                Destroy(child);
            }
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }
    }
}
