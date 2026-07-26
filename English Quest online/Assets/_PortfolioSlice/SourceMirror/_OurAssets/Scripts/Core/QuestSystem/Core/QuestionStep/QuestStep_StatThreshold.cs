using System;
using EnglishKingdom.StatsSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Quest step that completes when a chosen <see cref="StatDefinitionSO"/> reaches a target
/// count, counted from the moment this step becomes active (delta, not lifetime total).
///
/// Solo / local: this player's own progress is compared directly against <see cref="_targetCount"/>.
/// Multiplayer: add <see cref="NetworkedStatQuestStep"/> on the same GameObject to sum every
/// connected player's progress via Fusion state authority.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Stat Threshold")]
public class QuestStep_StatThreshold : QuestStep
{
    [Header("Stat")]
    [Tooltip("Which stat to watch. Create assets via Create > English Kingdom > Core > Stats > Stat Definition.")]
    [SerializeField] private StatDefinitionSO _stat;

    [Tooltip("Combined target count required to finish this step.")]
    [SerializeField] private int _targetCount = 1;

    /// <summary>
    /// Fires with this player's own contribution delta when their local watched stat changes.
    /// Consumed by <see cref="NetworkedStatQuestStep"/> when networking is enabled.
    /// </summary>
    public event Action<int> OnLocalProgressDelta;

    /// <summary>
    /// Fires with (currentCombined, target) whenever combined progress updates.
    /// Available for UI / debug listeners; not used by the network bridge.
    /// </summary>
    public event Action<int, int> OnProgressChanged;

    /// <summary>
    /// When set by a network bridge, local stat changes are reported via
    /// <see cref="OnLocalProgressDelta"/> only; combined progress comes from this provider
    /// instead of this player's local contribution.
    /// </summary>
    public Func<int> ProgressProvider { get; set; }

    public int TargetCount => _targetCount;
    public StatDefinitionSO Stat => _stat;

    /// <summary>This player's own progress since the step became active on this machine.</summary>
    public int LocalProgress => _localProgress;

    private IStatsService _statsService;
    private int _statBaseline;
    private int _localProgress;
    private bool _isSubscribed;

    public override void InitializeStep()
    {
        base.InitializeStep();

        if (_stat == null)
        {
            AppLog.Error($"[QuestStep_StatThreshold] No StatDefinitionSO assigned on {name}.");
            return;
        }

        if (_targetCount <= 0)
        {
            AppLog.Warning($"[QuestStep_StatThreshold] Target count is {_targetCount} on {name}. Step will finish immediately if progress is already >= target.");
        }

        if (!ServiceLocator.Global.TryGet(out _statsService))
        {
            AppLog.Error($"[QuestStep_StatThreshold] IStatsService is not registered. Cannot track stats on {name}.");
            return;
        }

        // Defensive unsubscribe so a second InitializeStep (e.g. quest rewind/re-advance)
        // does not stack duplicate callbacks.
        UnsubscribeFromStat();

        _statBaseline = _statsService.Get(_stat);
        _localProgress = 0;
        _statsService.Subscribe(_stat, HandleStatChanged);
        _isSubscribed = true;

        // If a network bridge is already attached, push its current total so late joiners
        // (or re-initialized steps) see other players' prior progress immediately.
        // When the bridge component exists but has not Spawned yet, do not treat local
        // progress as combined — wait for NetworkedStatQuestStep to take ownership.
        if (ProgressProvider != null)
            ReportCombinedProgress(ProgressProvider());
        else if (!HasNetworkBridge)
            ReportCombinedProgress(0);
    }

    /// <summary>
    /// Updates UI listeners and finishes the step once <paramref name="combined"/> reaches the target.
    /// Called by this step in solo play, or by the network bridge when synced.
    /// </summary>
    public void ReportCombinedProgress(int combined)
    {
        if (IsFinished) return;

        OnProgressChanged?.Invoke(combined, _targetCount);

        if (combined >= _targetCount)
            FinishStep();
    }

    private void HandleStatChanged(int oldValue, int newValue)
    {
        if (!StepIsActive || IsFinished) return;

        int newLocalProgress = Mathf.Max(0, newValue - _statBaseline);
        int delta = newLocalProgress - _localProgress;
        if (delta <= 0) return;

        _localProgress = newLocalProgress;
        OnLocalProgressDelta?.Invoke(delta);

        // Solo / local: this player's progress IS the combined progress.
        // Networked: the bridge owns the combined total via ProgressProvider + ReportCombinedProgress.
        // Also skip while a bridge component is present but has not set ProgressProvider yet
        // (Spawned has not run), so we never finish from local-only progress in multiplayer.
        if (ProgressProvider == null && !HasNetworkBridge)
            ReportCombinedProgress(_localProgress);
    }

    private bool HasNetworkBridge => GetComponent<NetworkedStatQuestStep>() != null;

    private void UnsubscribeFromStat()
    {
        if (!_isSubscribed || _statsService == null || _stat == null) return;

        _statsService.Unsubscribe(_stat, HandleStatChanged);
        _isSubscribed = false;
    }

    private void OnDestroy()
    {
        UnsubscribeFromStat();
        OnLocalProgressDelta = null;
        OnProgressChanged = null;
        ProgressProvider = null;
    }
}
