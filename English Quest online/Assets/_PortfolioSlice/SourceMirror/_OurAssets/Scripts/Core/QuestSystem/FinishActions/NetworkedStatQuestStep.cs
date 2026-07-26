using Fusion;
using UnityEngine;

/// <summary>
/// Opt-in network sync for <see cref="QuestStep_StatThreshold"/>. Add this component to the
/// same GameObject as a QuestStep_StatThreshold to sum every connected player's progress
/// (counted from when the step became active on their machine) into one combined total.
///
/// How it works (Opt-In Networking pattern):
///   - QuestStep_StatThreshold (MonoBehaviour) owns all local gameplay logic and stays untouched.
///   - Each peer reports only its own local progress deltas via OnLocalProgressDelta.
///   - State authority owns [Networked] NetworkedTotalProgress and accumulates every delta.
///   - OnChangedRender pushes the combined total back to every peer so they finish together.
///   - Late joiners: Spawned() immediately applies the current networked total, and each
///     client's own InitializeStep snapshots their local baseline when the step activates.
///
/// Usage:
///   - Add to a QuestStep_StatThreshold GameObject for multiplayer combined progress.
///   - Leave the step without this component for local-only / solo play.
///   - Pair with NetworkQuestStep / NetworkQuestInfo as needed for quest advancement itself;
///     this component only syncs the numeric progress, not quest step completion events.
///     Note: once NetworkedTotalProgress crosses the target, every peer finishes locally via
///     ReportCombinedProgress → FinishStep, which is enough for step completion. If you also
///     need the whole quest to stay in sync for late joiners, use NetworkQuestInfo on the
///     QuestInfo GameObject (same pattern as other networked steps).
/// </summary>
[RequireComponent(typeof(QuestStep_StatThreshold))]
[AddComponentMenu(QuestSystemComponentMenuPaths.Network + "/Networked Stat Quest Step")]
[DisallowMultipleComponent]
public class NetworkedStatQuestStep : NetworkBehaviour
{
    private QuestStep_StatThreshold _step;

    [Networked, OnChangedRender(nameof(OnNetworkedProgressChanged))]
    private int NetworkedTotalProgress { get; set; }

    public override void Spawned()
    {
        _step = GetComponent<QuestStep_StatThreshold>();
        _step.ProgressProvider = () => NetworkedTotalProgress;
        _step.OnLocalProgressDelta += HandleLocalProgressDelta;

        // If the step became active before this NetworkBehaviour spawned, local deltas were
        // never reported. Flush any already-accumulated local progress once.
        if (_step.LocalProgress > 0)
            HandleLocalProgressDelta(_step.LocalProgress);

        // Late joiners (and peers whose step is already active) must see prior combined progress.
        _step.ReportCombinedProgress(NetworkedTotalProgress);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_step == null) return;

        _step.OnLocalProgressDelta -= HandleLocalProgressDelta;
        _step.ProgressProvider = null;
    }

    private void HandleLocalProgressDelta(int delta)
    {
        if (delta <= 0) return;
        if (Object == null || !Object.IsValid) return;

        if (HasStateAuthority)
            ApplyProgressAuthority(delta);
        else
            RPC_ReportProgress(delta);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportProgress(int delta)
    {
        if (!HasStateAuthority) return;
        ApplyProgressAuthority(delta);
    }

    private void ApplyProgressAuthority(int delta)
    {
        if (delta <= 0) return;
        NetworkedTotalProgress += delta;
    }

    private void OnNetworkedProgressChanged()
    {
        if (_step == null) return;
        _step.ReportCombinedProgress(NetworkedTotalProgress);
    }
}
