using TMPro;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Npc + "/Quest NPC Indicator")]
public class QuestNpcIndicator : MonoBehaviour
{
    [SerializeField] private string npcId;
    [SerializeField] private GameObject availableMarker;
    [SerializeField] private GameObject inProgressMarker;
    [SerializeField] private GameObject turnInMarker;
    [SerializeField] private TMP_Text availableLabel;
    [SerializeField] private TMP_Text inProgressLabel;
    [SerializeField] private TMP_Text turnInLabel;

    private IQuestService _questService;
    private IQuestAvailabilityService _availabilityService;

    private void Start()
    {
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
        TrySubscribeToQuestEvents();

        IQuestAvailabilityService availabilityService = ResolveAvailabilityService();
        QuestNpcIndicatorState state = availabilityService != null
            ? availabilityService.GetBestIndicator(npcId)
            : QuestNpcIndicatorState.None;

        SetMarker(availableMarker, availableLabel, "!", state == QuestNpcIndicatorState.Available);
        SetMarker(inProgressMarker, inProgressLabel, "...", state == QuestNpcIndicatorState.InProgress);
        SetMarker(turnInMarker, turnInLabel, "?", state == QuestNpcIndicatorState.TurnIn);
    }

    private static void SetMarker(GameObject marker, TMP_Text label, string text, bool active)
    {
        if (marker != null)
            marker.SetActive(active);

        if (label != null && active)
            label.text = text;
    }
}
