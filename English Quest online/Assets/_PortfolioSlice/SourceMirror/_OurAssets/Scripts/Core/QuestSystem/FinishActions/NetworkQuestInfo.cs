using Fusion;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Wraps step index and quest state as a single network struct so both values
/// always replicate atomically in the same Fusion snapshot. This prevents the
/// callback from reading a new StepIndex paired with a stale QuestState (or
/// vice versa) when the two properties would otherwise arrive in different frames.
/// </summary>
public struct QuestNetworkState : INetworkStruct
{
    public int StepIndex;
    public int QuestState;
}

/// <summary>
/// Opt-in network sync for a QuestInfo. Add this component to the same GameObject
/// as a QuestInfo to keep ALL clients on the same quest state and step index.
///
/// How it works (Opt-In Networking pattern):
///   - QuestInfo (MonoBehaviour) owns all gameplay logic and stays untouched.
///   - QuestManager (IQuestService) has no network knowledge whatsoever.
///   - This NetworkBehaviour observes QuestManager events (state authority only)
///     and writes [Networked] properties that replicate to all clients.
///   - Remote clients receive state via RPC and via [Networked] on late join.
///   - All sync is driven entirely through QuestInfo and QuestManager's existing
///     public API — no network methods were added to either.
///
/// Usage:
///   - Add to the GameObject that has QuestInfo to sync the WHOLE quest.
///   - Leave a QuestInfo without this component to keep it local-only.
/// </summary>
[RequireComponent(typeof(QuestInfo))]
[AddComponentMenu(QuestSystemComponentMenuPaths.Network + "/Network Quest Info")]
public class NetworkQuestInfo : NetworkBehaviour
{
    private QuestInfo _quest;

    // Single struct keeps StepIndex and QuestState atomic: both values always
    // replicate together in one snapshot so OnNetworkedStateChanged never reads
    // a mismatched pair. Only state authority writes this.
    [Networked, OnChangedRender(nameof(OnNetworkedStateChanged))]
    private QuestNetworkState NetworkedQuestData { get; set; }

    public override void Spawned()
    {
        _quest = GetComponent<QuestInfo>();

        if (ServiceLocator.For(this).TryGet<IQuestService>(out var qs))
        {
            qs.OnQuestStarted   += HandleQuestEvent;
            qs.OnQuestUpdated   += HandleQuestEvent;
            qs.OnQuestCompleted += HandleQuestEvent;
        }

        // Apply persisted networked state for late-joining clients.
        var data = NetworkedQuestData;
        if (!HasStateAuthority && data.StepIndex > 0)
            ApplyNetworkedState(data.StepIndex, (QuestState)data.QuestState);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!this)
            return;

        ServiceLocator locator = ServiceLocator.For(this);
        if (locator == null || !locator.TryGet<IQuestService>(out var qs) || qs == null)
            return;

        qs.OnQuestStarted   -= HandleQuestEvent;
        qs.OnQuestUpdated   -= HandleQuestEvent;
        qs.OnQuestCompleted -= HandleQuestEvent;
    }

    // Any peer reports progress. The authority is the single writer of [Networked] state,
    // so there are no race conditions between simultaneous RPC senders.
    private void HandleQuestEvent(QuestInfo quest)
    {
        if (quest != _quest || Object == null || !Object.IsValid) return;
        RPC_ReportQuestState(quest.currentStepIndex, (int)quest.state);
    }

    // All peers → Authority: authority serialises all writes; state never races.
    // Accepts an update only when it represents a genuine forward transition:
    //   - a higher step index always wins (quest progressed further), OR
    //   - same step index with a higher quest state (e.g. IN_PROGRESS → CAN_FINISH).
    // This prevents a stale or out-of-order RPC from overwriting newer authority state.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportQuestState(int stepIndex, int questState)
    {
        var current = NetworkedQuestData;
        if (stepIndex < current.StepIndex) return;                          // step regressed — reject
        if (stepIndex == current.StepIndex && questState <= current.QuestState) return; // same step, no forward state change — reject

        NetworkedQuestData = new QuestNetworkState { StepIndex = stepIndex, QuestState = questState };
    }

    // Called on all peers by Fusion when NetworkedQuestData replicates.
    // Because StepIndex and QuestState are in one struct they always arrive together.
    // Not called on spawn — the Spawned() late-join check handles that case.
    // Safe to run on state authority too: ApplyNetworkedState is idempotent and
    // its inner guards no-op when the quest is already at the target state.
    private void OnNetworkedStateChanged()
    {
        var data = NetworkedQuestData;
        ApplyNetworkedState(data.StepIndex, (QuestState)data.QuestState);
    }

    // Drives the local quest forward using only QuestInfo and QuestManager's
    // existing public API. No network methods were added to either class.
    private void ApplyNetworkedState(int targetStepIndex, QuestState targetState)
    {
        if (_quest == null) return;
        if (!ServiceLocator.For(this).TryGet<IQuestService>(out var qs)) return;

        InitializeQuestIfStarted(targetState, qs);

        AdvanceToStepIndex(targetStepIndex);

        ApplyQuestFinalization(targetState, qs);
    }

    private void ApplyQuestFinalization(QuestState targetState, IQuestService qs)
    {
        // Apply the final state via the existing public API.
        if (CanFinalizeQuest(targetState))
        {
            _quest.SetState(QuestState.CAN_FINISH);
            qs.FinishQuest(_quest);
        }
    }

    private bool CanFinalizeQuest(QuestState targetState)
    {
        return (targetState == QuestState.FINISHED || targetState == QuestState.CAN_FINISH)
                    && _quest.state != QuestState.FINISHED;
    }

    private void AdvanceToStepIndex(int targetStepIndex)
    {
        // Silently fast-forward past completed steps to match the authority's index.
        while (IsCurrentStepBeforeTarget(targetStepIndex))
        {
            _quest.StoreQuestStepState(new QuestStepState("", QuestStepStatus.COMPLETED), _quest.currentStepIndex);
            _quest.MoveToNextStep();
        }
    }

    private bool IsCurrentStepBeforeTarget(int targetStepIndex)
    {
        return _quest.currentStepIndex < targetStepIndex && _quest.CurrentStepExists();
    }

    private void InitializeQuestIfStarted(QuestState targetState, IQuestService qs)
    {
        // Start the quest locally if the authority has already started it.
        if (CanInitializeQuest(targetState))
        {
            _quest.SetState(QuestState.CAN_START);
            qs.StartQuest(_quest);
        }
    }

    private bool CanInitializeQuest(QuestState targetState)
    {
        return (_quest.state == QuestState.CAN_START || _quest.state == QuestState.REQUIREMENTS_NOT_MET)
            && targetState != QuestState.CAN_START && targetState != QuestState.REQUIREMENTS_NOT_MET;
    }
}

