using TMPro;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Npc + "/Quest NPC Indicator")]
public class QuestNpcIndicator : MonoBehaviour
{
    [SerializeField] private string npcId;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 2.25f, 0f);
    [SerializeField] private GameObject availableMarker;
    [SerializeField] private GameObject inProgressMarker;
    [SerializeField] private GameObject turnInMarker;
    [SerializeField] private GameObject lockedMarker;
    [SerializeField] private TMP_Text availableLabel;
    [SerializeField] private TMP_Text inProgressLabel;
    [SerializeField] private TMP_Text turnInLabel;
    [SerializeField] private TMP_Text lockedLabel;

    private IQuestService _questService;
    private IQuestAvailabilityService _availabilityService;
    private GameObject _defaultMarker;
    private TMP_Text _defaultLabel;

    public void Configure(string newNpcId)
    {
        if (!string.IsNullOrEmpty(newNpcId))
            npcId = newNpcId;
    }

    private void Start()
    {
        EnsureDefaultMarker();
        TrySubscribeToQuestEvents();
        RefreshIndicator();
    }

    private void OnDestroy()
    {
        if (_questService != null)
        {
            _questService.OnQuestStarted -= OnQuestChanged;
            _questService.OnQuestUpdated -= OnQuestChanged;
            _questService.OnQuestCompleted -= OnQuestChanged;
            _questService.OnQuestStateChanged -= OnQuestChanged;
        }
    }

    private void OnQuestChanged(QuestInfo _) => RefreshIndicator();

    private void TrySubscribeToQuestEvents()
    {
        if (_questService != null)
            return;

        if (!ServiceLocator.For(this).TryGet(out _questService))
            return;

        _questService.OnQuestStarted += OnQuestChanged;
        _questService.OnQuestUpdated += OnQuestChanged;
        _questService.OnQuestCompleted += OnQuestChanged;
        _questService.OnQuestStateChanged += OnQuestChanged;
    }

    private IQuestAvailabilityService ResolveAvailabilityService()
    {
        if (_availabilityService == null)
            ServiceLocator.For(this).TryGet(out _availabilityService);
        return _availabilityService;
    }

    private void RefreshIndicator()
    {
        EnsureDefaultMarker();
        TrySubscribeToQuestEvents();

        IQuestAvailabilityService availabilityService = ResolveAvailabilityService();
        QuestNpcIndicatorState state = availabilityService != null
            ? availabilityService.GetBestIndicator(npcId)
            : QuestNpcIndicatorState.None;

        bool usesDefaultMarker = HasNoAssignedMarkers();
        if (usesDefaultMarker)
            SetDefaultMarker(state);

        SetMarker(availableMarker, availableLabel, "!", state == QuestNpcIndicatorState.Available && !usesDefaultMarker);
        SetMarker(inProgressMarker, inProgressLabel, "...", state == QuestNpcIndicatorState.InProgress && !usesDefaultMarker);
        SetMarker(turnInMarker, turnInLabel, "?", state == QuestNpcIndicatorState.TurnIn && !usesDefaultMarker);
        SetMarker(lockedMarker, lockedLabel, "LOCKED", state == QuestNpcIndicatorState.Locked && !usesDefaultMarker);
    }

    private static void SetMarker(GameObject marker, TMP_Text label, string text, bool active)
    {
        if (marker != null)
            marker.SetActive(active);

        if (label != null && active)
            label.text = text;
    }

    private bool HasNoAssignedMarkers()
    {
        return availableMarker == null &&
               inProgressMarker == null &&
               turnInMarker == null &&
               lockedMarker == null &&
               availableLabel == null &&
               inProgressLabel == null &&
               turnInLabel == null &&
               lockedLabel == null;
    }

    private void EnsureDefaultMarker()
    {
        if (!HasNoAssignedMarkers() || _defaultMarker != null)
            return;

        _defaultMarker = new GameObject("Quest Status Indicator");
        _defaultMarker.transform.SetParent(transform, false);
        _defaultMarker.transform.localPosition = markerOffset;

        _defaultLabel = _defaultMarker.AddComponent<TextMeshPro>();
        _defaultLabel.alignment = TextAlignmentOptions.Center;
        _defaultLabel.fontSize = 4f;
        _defaultLabel.fontStyle = FontStyles.Bold;
        _defaultLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _defaultLabel.outlineWidth = 0.25f;
        _defaultLabel.outlineColor = Color.black;

        _defaultMarker.AddComponent<NameTagBillboard>();
    }

    private void SetDefaultMarker(QuestNpcIndicatorState state)
    {
        if (_defaultMarker == null || _defaultLabel == null)
            return;

        switch (state)
        {
            case QuestNpcIndicatorState.Available:
                SetDefaultMarker("!", new Color(1f, 0.82f, 0.15f), true);
                break;
            case QuestNpcIndicatorState.InProgress:
                SetDefaultMarker("...", new Color(0.35f, 0.75f, 1f), true);
                break;
            case QuestNpcIndicatorState.TurnIn:
                SetDefaultMarker("?", new Color(0.35f, 1f, 0.45f), true);
                break;
            case QuestNpcIndicatorState.Locked:
                SetDefaultMarker("LOCKED", new Color(0.7f, 0.7f, 0.7f), true);
                break;
            default:
                SetDefaultMarker(string.Empty, Color.white, false);
                break;
        }
    }

    private void SetDefaultMarker(string text, Color color, bool active)
    {
        _defaultMarker.SetActive(active);
        if (!active)
            return;

        _defaultLabel.text = text;
        _defaultLabel.color = color;
    }
}
