using TMPro;
using EnglishQuest.PortfolioDemo;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Npc + "/Quest NPC Indicator")]
public class QuestNpcIndicator : MonoBehaviour
{
    private static readonly Vector3 DefaultMarkerOffset = new Vector3(0f, 2.25f, 0f);

    private IQuestService _questService;
    private IQuestAvailabilityService _availabilityService;
    private string _npcId;
    private GameObject _defaultMarker;
    private TMP_Text _defaultLabel;
    private QuestNpcIndicatorState _currentState = QuestNpcIndicatorState.None;
    private Vector3 _defaultMarkerBaseScale = Vector3.one;

    public void Configure(string newNpcId)
    {
        if (!string.IsNullOrEmpty(newNpcId))
            _npcId = newNpcId;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(_npcId) && TryGetComponent(out NpcQuestGiver questGiver))
            Configure(questGiver.NpcId);

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
            ? availabilityService.GetBestIndicator(_npcId)
            : QuestNpcIndicatorState.None;
        _currentState = state;
        SetDefaultMarker(state);
    }

    private void EnsureDefaultMarker()
    {
        if (_defaultMarker != null)
            return;

        _defaultMarker = new GameObject("Quest Status Indicator");
        _defaultMarker.transform.SetParent(transform, false);
        _defaultMarker.transform.localPosition = DefaultMarkerOffset;

        _defaultLabel = _defaultMarker.AddComponent<TextMeshPro>();
        _defaultLabel.alignment = TextAlignmentOptions.Center;
        _defaultLabel.fontSize = 4f;
        _defaultLabel.fontStyle = FontStyles.Bold;
        _defaultLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _defaultLabel.outlineWidth = 0.25f;
        _defaultLabel.outlineColor = Color.black;
        _defaultLabel.characterSpacing = 6f;
        _defaultLabel.textWrappingMode = TextWrappingModes.NoWrap;

        NameTagBillboard billboard = _defaultMarker.AddComponent<NameTagBillboard>();
        billboard.Configure(NameTagBillboard.FacingMode.TowardCamera, new Vector3(0f, 180f, 0f));
        _defaultMarkerBaseScale = _defaultMarker.transform.localScale;
    }

    private void SetDefaultMarker(QuestNpcIndicatorState state)
    {
        if (_defaultMarker == null || _defaultLabel == null)
            return;

        bool active = state != QuestNpcIndicatorState.None;
        _defaultMarker.SetActive(active);
        if (!active)
            return;

        _defaultLabel.text = PortfolioThemeResources.GetNpcIndicatorText(state);
        _defaultLabel.color = PortfolioThemeResources.GetNpcIndicatorColor(state);
    }

    private void Update()
    {
        if (_defaultMarker == null || !_defaultMarker.activeSelf)
            return;

        float pulse = _currentState switch
        {
            QuestNpcIndicatorState.TurnIn => 1f + Mathf.Sin(Time.unscaledTime * 5.4f) * 0.08f,
            QuestNpcIndicatorState.Available => 1f + Mathf.Sin(Time.unscaledTime * 3.8f) * 0.05f,
            QuestNpcIndicatorState.InProgress => 1f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.03f,
            _ => 1f
        };

        _defaultMarker.transform.localScale = _defaultMarkerBaseScale * pulse;
    }
}
