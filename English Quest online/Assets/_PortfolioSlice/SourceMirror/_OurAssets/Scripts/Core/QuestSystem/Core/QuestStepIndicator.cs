using TargetIndicators;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Drop on any GameObject that should display a world-space indicator arrow for a specific QuestStep.
/// Assign the step and the TargetIndicatorManager in the Inspector — the rest is automatic.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.Root + "/Quest Step Indicator")]
public class QuestStepIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The quest step this indicator tracks.")]
    [SerializeField] QuestStep _questStep;

    [Tooltip("Where the arrow points. Leave empty to use this object's position.")]
    [SerializeField] Transform _indicatorTarget;

    [Header("Settings")]
    [Tooltip("Show the indicator while this step is the active, incomplete step.")]
    [SerializeField] bool _showWhenStepIsActive = true;

    [Tooltip("Show the indicator after this step has been completed.")]
    [SerializeField] bool _showWhenStepIsFinished = false;

    IQuestService _questService;
    TargetIndicatorManager _targetIndicatorManager;
    TargetIndicatorId _indicatorId;
    bool _isShowing;

    void Start()
    {
        if (_questStep == null)
        {
            AppLog.Error($"[QuestStepIndicator] QuestStep is not assigned on '{gameObject.name}'.", this);
            return;
        }

        if (!ServiceLocator.For(this).TryGet<TargetIndicatorManager>(out _targetIndicatorManager))
        {
            AppLog.Error($"[QuestStepIndicator] TargetIndicatorManager not found in ServiceLocator on '{gameObject.name}'. Add a TargetIndicatorService component to the TargetIndicatorManager's GameObject.", this);
            return;
        }

        if (ServiceLocator.For(this).TryGet<IQuestService>(out _questService))
        {
            _questService.OnQuestStarted  += OnQuestChanged;
            _questService.OnQuestUpdated  += OnQuestChanged;
            _questService.OnQuestCompleted += OnQuestChanged;
        }

        _questStep.OnStepFinished += OnStepFinished;

        RefreshIndicator();
    }

    void OnDestroy()
    {
        if (_questService != null)
        {
            _questService.OnQuestStarted   -= OnQuestChanged;
            _questService.OnQuestUpdated   -= OnQuestChanged;
            _questService.OnQuestCompleted -= OnQuestChanged;
        }

        if (_questStep != null)
            _questStep.OnStepFinished -= OnStepFinished;

        HideIndicator();
    }

    void OnQuestChanged(QuestInfo _) => RefreshIndicator();

    void OnStepFinished(QuestStep _, string __) => RefreshIndicator();

    void RefreshIndicator()
    {
        if (ShouldShow())
            ShowIndicator();
        else
            HideIndicator();
    }

    bool ShouldShow()
    {
        if (_questStep.IsFinished)   return _showWhenStepIsFinished;
        if (_questStep.StepIsActive) return _showWhenStepIsActive;
        return false;
    }

    void ShowIndicator()
    {
        if (_isShowing) return;

        var target = _indicatorTarget != null ? _indicatorTarget : transform;

        if (_targetIndicatorManager.TryAddTarget(target, out var indicator))
        {
            _indicatorId = indicator.Id;
            _isShowing = true;
        }
    }

    void HideIndicator()
    {
        if (!_isShowing) return;

        _targetIndicatorManager.TryRemoveTarget(_indicatorId);
        _isShowing = false;
    }
}
