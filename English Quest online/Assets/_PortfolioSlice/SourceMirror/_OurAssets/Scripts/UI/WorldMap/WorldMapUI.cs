using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class WorldMapUI : GameplayUIBase
{
    protected override PlayerLockSystem.LockType[] LocksToApply => new[]
    {
        PlayerLockSystem.LockType.Movement,
        PlayerLockSystem.LockType.Camera,
        PlayerLockSystem.LockType.Cursor,
        PlayerLockSystem.LockType.GameplayInput
    };

    public bool IsOpen => _isOpen;

    public bool IsInTravelMode =>
        _isOpen
        && _nodeContainer != null
        && !_nodeContainer.gameObject.activeSelf
        && _vehicleIcon != null
        && _vehicleIcon.gameObject.activeSelf;

    public bool IsVehicleVisible =>
        _vehicleIcon != null && _vehicleIcon.gameObject.activeSelf;

    [Header("UI References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Image _mapImage;
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private RectTransform _vehicleIcon;
    [SerializeField] private RectTransform _nodeContainer;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _nodeButtonPrefab;
    [SerializeField] private Image _compassRose;

    [Header("Travel Presentation")]
    [SerializeField] private float _postAnimationHoldSeconds = 0.35f;
    [SerializeField] private float _travelAnimationSmoothing = 14f;
    [SerializeField] private float _travelFadeDurationSeconds = 0.2f;

    private CanvasGroup _fadeCanvasGroup;

    [Header("Colors")]
    [SerializeField] private Color _reachableColor = Color.white;
    [SerializeField] private Color _lockedColor = new(0.55f, 0.55f, 0.55f, 0.65f);
    [SerializeField] private Color _currentColor = new(1f, 0.92f, 0.45f, 1f);

    private readonly List<GameObject> _spawnedNodes = new();
    private WorldMapDefinitionSO _activeMap;
    private string _currentNodeId;
    private PlayerInteraction _activeInteractor;
    private bool _isOpen;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);

        if (_panel != null)
            _panel.SetActive(false);
    }

    public void Open(WorldMapDefinitionSO map, string currentNodeId, PlayerInteraction interactor)
    {
        if (map == null || interactor == null)
            return;

        _activeMap = map;
        _currentNodeId = currentNodeId;
        _activeInteractor = interactor;
        _isOpen = true;

        EnsureCanvasVisible();

        if (_panel != null)
            _panel.SetActive(true);

        PrepareMapLayout();

        if (_mapImage != null)
            _mapImage.sprite = map.MapImage;

        if (_vehicleIcon != null && map.VehicleIcon != null)
        {
            Image vehicleImage = _vehicleIcon.GetComponent<Image>();
            if (vehicleImage != null)
            {
                vehicleImage.sprite = map.VehicleIcon;
                vehicleImage.color = Color.white;
            }
        }

        RebuildNodeButtons();
        PositionVehicleAtNode(currentNodeId);
        RestoreTravelChrome();
        BeginInteraction(interactor);
    }

    public void OpenForPartyFollowTravel(WorldMapDefinitionSO map, string fromNodeId, PlayerInteraction interactor)
    {
        if (map == null || interactor == null)
            return;

        _activeMap = map;
        _currentNodeId = fromNodeId;
        _activeInteractor = interactor;
        _isOpen = true;

        EnsureCanvasVisible();

        if (_panel != null)
            _panel.SetActive(true);

        PrepareMapLayout();

        if (_mapImage != null)
            _mapImage.sprite = map.MapImage;

        if (_vehicleIcon != null && map.VehicleIcon != null)
        {
            Image vehicleImage = _vehicleIcon.GetComponent<Image>();
            if (vehicleImage != null)
            {
                vehicleImage.sprite = map.VehicleIcon;
                vehicleImage.color = Color.white;
            }
        }

        if (map.TryGetNode(fromNodeId, out WorldMapNodeData fromNode))
            EnterTravelMode(fromNode.NormalizedPosition);
        else
            EnterTravelMode(Vector2.zero);

        ApplyFollowTravelLocks(interactor);
    }

    public void ApplyFollowTravelLocks(PlayerInteraction interactor)
    {
        if (interactor == null)
            return;

        PlayerLockSystem lockSystem = interactor.GetComponentInParent<PlayerLockSystem>();
        if (lockSystem == null)
            return;

        lockSystem.Lock(interactor, LocksToApply);
    }

    public void ReleaseFollowTravelLocks(PlayerInteraction interactor)
    {
        if (interactor == null)
            return;

        PlayerLockSystem lockSystem = interactor.GetComponentInParent<PlayerLockSystem>();
        if (lockSystem == null)
            return;

        lockSystem.Unlock(interactor, LocksToApply);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        PlayerInteraction interactor = _activeInteractor;
        IInteractable boundInteractable = interactor != null ? interactor.ActiveInteraction : null;

        _isOpen = false;
        EndInteraction();
        ClearNodeButtons();
        ShutdownTravelPresentation();

        if (interactor != null && boundInteractable != null)
            interactor.UnlockInteraction(boundInteractable);

        _activeInteractor = null;
    }

    public void CloseAfterTravel()
    {
        PlayerInteraction interactor = _activeInteractor;
        IInteractable boundInteractable = interactor != null ? interactor.ActiveInteraction : null;

        _isOpen = false;
        EndInteraction();
        ClearNodeButtons();
        ShutdownTravelPresentation();

        if (interactor != null && boundInteractable != null)
            interactor.UnlockInteraction(boundInteractable);

        _activeInteractor = null;
    }

    public void PrepareForSceneTransition()
    {
        if (!_isOpen)
            return;

        PlayerInteraction interactor = _activeInteractor;
        IInteractable boundInteractable = interactor != null ? interactor.ActiveInteraction : null;

        _isOpen = false;
        HideTravelPresentation();
        EndInteraction();
        ClearNodeButtons();

        if (_panel != null)
            _panel.SetActive(false);

        if (interactor != null && boundInteractable != null)
            interactor.UnlockInteraction(boundInteractable);

        _activeInteractor = null;
    }

    public void HideTravelPresentation()
    {
        if (_vehicleIcon != null)
            _vehicleIcon.gameObject.SetActive(false);

        if (_panel != null)
            _panel.SetActive(false);
    }

    public void SetCurrentNode(string nodeId) => _currentNodeId = nodeId;

    public void EnterTravelMode(Vector2 fromNormalized)
    {
        if (_nodeContainer != null)
            _nodeContainer.gameObject.SetActive(false);

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(false);

        if (_vehicleIcon != null)
        {
            _vehicleIcon.gameObject.SetActive(true);
            _vehicleIcon.SetAsLastSibling();
        }

        PrepareMapLayout();
        PositionVehicleAtNormalized(fromNormalized);
    }

    public void EnsureFadeOverlayReady()
    {
        ResolveFadeOverlay();

        if (_fadeCanvasGroup != null)
            _fadeCanvasGroup.alpha = 0f;
    }

    private void ResolveFadeOverlay()
    {
        if (_fadeCanvasGroup != null)
        {
            if (!_fadeCanvasGroup.gameObject.activeSelf)
                _fadeCanvasGroup.gameObject.SetActive(true);
            return;
        }

        Transform canvasRoot = transform.parent;
        if (canvasRoot == null)
            return;

        Transform fadeTransform = canvasRoot.Find("FadeOverlay");
        if (fadeTransform == null)
            return;

        fadeTransform.gameObject.SetActive(true);
        _fadeCanvasGroup = fadeTransform.GetComponent<CanvasGroup>();
    }

    public void HideMapForSceneTransition()
    {
        HideTravelPresentation();
        ClearNodeButtons();

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(false);
    }

    public async UniTask FadeOutTravelOverlayAsync()
    {
        EnsureFadeOverlayReady();
        if (_fadeCanvasGroup == null)
            return;

        EnsureCanvasVisible();
        _fadeCanvasGroup.gameObject.SetActive(true);
        _fadeCanvasGroup.blocksRaycasts = true;

        float duration = ResolveTravelFadeDuration();
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _fadeCanvasGroup.alpha = 1f;
        HideMapForSceneTransition();
    }

    public async UniTask FadeInTravelOverlayAsync()
    {
        ResolveFadeOverlay();
        if (_fadeCanvasGroup == null)
            return;

        EnsureCanvasVisible();
        _fadeCanvasGroup.gameObject.SetActive(true);
        _fadeCanvasGroup.blocksRaycasts = true;

        if (_fadeCanvasGroup.alpha < 1f)
            _fadeCanvasGroup.alpha = 1f;

        float duration = ResolveTravelFadeDuration();
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;
        HideTravelCanvasRoot();
    }

    public void DismissTravelOverlayImmediate()
    {
        ResolveFadeOverlay();
        if (_fadeCanvasGroup == null)
        {
            HideTravelCanvasRoot();
            return;
        }

        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;
        _fadeCanvasGroup.gameObject.SetActive(false);
        HideTravelCanvasRoot();
    }

    private void ShutdownTravelPresentation()
    {
        if (_vehicleIcon != null)
            _vehicleIcon.gameObject.SetActive(false);

        if (_nodeContainer != null)
            _nodeContainer.gameObject.SetActive(false);

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(false);

        ResolveFadeOverlay();
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.gameObject.SetActive(false);
        }

        if (_panel != null)
            _panel.SetActive(false);

        HideTravelCanvasRoot();
    }

    private void HideTravelCanvasRoot()
    {
        Canvas canvas = ResolveTravelCanvas();
        if (canvas == null)
            return;

        RectTransform root = canvas.transform as RectTransform;
        if (root != null)
            root.localScale = Vector3.zero;

        canvas.gameObject.SetActive(false);
    }

    private Canvas ResolveTravelCanvas()
    {
        Transform canvasRoot = transform.parent;
        return canvasRoot != null ? canvasRoot.GetComponent<Canvas>() : null;
    }

    private void ResetCanvasAfterTravel()
    {
        HideTravelCanvasRoot();
    }

    private float ResolveTravelFadeDuration()
    {
        if (_travelFadeDurationSeconds > 0f)
            return _travelFadeDurationSeconds;

        if (_fadeCanvasGroup != null
            && _fadeCanvasGroup.TryGetComponent(out SceneFader fader))
        {
            return Mathf.Max(0.01f, fader.FadeDuration);
        }

        return 0.2f;
    }

    public async UniTask PlayTravelAnimationAsync(
        WorldMapRouteData route,
        Vector2 fromNormalized,
        Vector2 toNormalized,
        float duration)
    {
        if (_vehicleIcon == null || _mapRect == null)
            return;

        PrepareMapLayout();
        PositionVehicleAtNormalized(fromNormalized);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        List<Vector2> path = BuildAnimationPath(fromNormalized, route.waypoints, toNormalized);
        if (path.Count < 2)
            return;

        float startTime = Time.unscaledTime;
        Vector2 displayPosition = SamplePath(path, 0f);
        _vehicleIcon.anchoredPosition = displayPosition;

        while (true)
        {
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed >= duration)
                break;

            Vector2 targetPosition = SamplePath(path, Mathf.Clamp01(elapsed / duration));
            float blend = 1f - Mathf.Exp(-_travelAnimationSmoothing * Time.unscaledDeltaTime);
            displayPosition = Vector2.Lerp(displayPosition, targetPosition, blend);
            _vehicleIcon.anchoredPosition = displayPosition;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        _vehicleIcon.anchoredPosition = SamplePath(path, 1f);

        if (_postAnimationHoldSeconds > 0f)
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_postAnimationHoldSeconds),
                ignoreTimeScale: true);
        }
    }

    private void RebuildNodeButtons()
    {
        ClearNodeButtons();

        if (_activeMap == null || _nodeButtonPrefab == null || _nodeContainer == null)
            return;

        HashSet<string> reachable = new(_activeMap.GetReachableNodeIds(_currentNodeId));

        for (int i = 0; i < _activeMap.Nodes.Count; i++)
        {
            WorldMapNodeData node = _activeMap.Nodes[i];
            GameObject buttonObject = Instantiate(_nodeButtonPrefab, _nodeContainer);
            buttonObject.SetActive(true);
            _spawnedNodes.Add(buttonObject);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = NormalizedToAnchored(node.NormalizedPosition);

            bool isCurrent = node.id == _currentNodeId;
            bool isReachable = node.unlockedByDefault && reachable.Contains(node.id);
            Color stateColor = isCurrent ? _currentColor : isReachable ? _reachableColor : _lockedColor;

            Image marker = buttonObject.transform.Find("Marker")?.GetComponent<Image>();
            if (marker != null && node.locationIcon != null)
            {
                marker.sprite = node.locationIcon;
                marker.color = stateColor;
            }
            else if (marker != null)
            {
                marker.color = stateColor;
            }

            Image namePlate = buttonObject.transform.Find("NamePlate")?.GetComponent<Image>();
            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>();
            if (namePlate != null)
            {
                namePlate.color = isReachable || isCurrent ? Color.white : _lockedColor;

                float plateWidth = Mathf.Clamp(36f + node.displayName.Length * 7.5f, 150f, 260f);
                RectTransform plateRect = namePlate.rectTransform;
                plateRect.sizeDelta = new Vector2(plateWidth, 52f);
            }

            if (label != null)
            {
                label.text = node.displayName;
                label.color = isReachable || isCurrent
                    ? Color.white
                    : new Color(0.85f, 0.85f, 0.85f, 0.8f);
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
                continue;

            bool canSelect = !isCurrent && isReachable;
            button.interactable = canSelect;
            if (!canSelect)
                continue;

            string targetNodeId = node.id;
            button.onClick.AddListener(() => OnDestinationClicked(targetNodeId));
        }
    }

    private async void OnDestinationClicked(string targetNodeId)
    {
        if (_activeMap == null || _activeInteractor == null)
            return;

        if (!ServiceLocator.For(_activeInteractor).TryGet<IWorldTravelService>(out var travelService))
            return;

        if (!travelService.CanTravel(_activeInteractor))
            return;

        DisableNodeButtons();
        await travelService.TravelAsync(_activeMap, _currentNodeId, targetNodeId, _activeInteractor);
    }

    private void DisableNodeButtons()
    {
        for (int i = 0; i < _spawnedNodes.Count; i++)
        {
            Button button = _spawnedNodes[i].GetComponent<Button>();
            if (button != null)
                button.interactable = false;
        }
    }

    private void PositionVehicleAtNode(string nodeId)
    {
        if (_vehicleIcon == null || _activeMap == null)
            return;

        if (_activeMap.TryGetNode(nodeId, out WorldMapNodeData node))
            PositionVehicleAtNormalized(node.NormalizedPosition);
    }

    private void PositionVehicleAtNormalized(Vector2 normalized)
    {
        if (_vehicleIcon != null)
            _vehicleIcon.anchoredPosition = NormalizedToAnchored(normalized);
    }

    private void PrepareMapLayout()
    {
        if (_panel != null && !_panel.activeSelf)
            _panel.SetActive(true);

        EnsureCanvasVisible();
        Canvas.ForceUpdateCanvases();

        if (_mapRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_mapRect);
    }

    private void EnsureCanvasVisible()
    {
        Canvas canvas = ResolveTravelCanvas();
        if (canvas == null)
            return;

        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);

        RectTransform root = canvas.transform as RectTransform;
        if (root != null && root.localScale == Vector3.zero)
            root.localScale = Vector3.one;
    }

    private void RestoreTravelChrome()
    {
        if (_nodeContainer != null)
            _nodeContainer.gameObject.SetActive(true);

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(true);
    }

    private Vector2 NormalizedToAnchored(Vector2 normalized)
    {
        Vector2 size = _mapRect.rect.size;
        return new Vector2(
            (normalized.x - 0.5f) * size.x,
            (normalized.y - 0.5f) * size.y);
    }

    private static List<Vector2> BuildAnimationPath(Vector2 from, Vector2[] waypoints, Vector2 to)
    {
        List<Vector2> path = new() { from };
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
                path.Add(waypoints[i]);
        }

        path.Add(to);
        return path;
    }

    private Vector2 SamplePath(List<Vector2> normalizedPath, float t)
    {
        if (normalizedPath.Count == 1)
            return NormalizedToAnchored(normalizedPath[0]);

        float scaled = t * (normalizedPath.Count - 1);
        int index = Mathf.Min(Mathf.FloorToInt(scaled), normalizedPath.Count - 2);
        float localT = scaled - index;
        Vector2 sample = Vector2.Lerp(normalizedPath[index], normalizedPath[index + 1], localT);
        return NormalizedToAnchored(sample);
    }

    private void ClearNodeButtons()
    {
        for (int i = 0; i < _spawnedNodes.Count; i++)
        {
            if (_spawnedNodes[i] != null)
                Destroy(_spawnedNodes[i]);
        }

        _spawnedNodes.Clear();
    }
}
