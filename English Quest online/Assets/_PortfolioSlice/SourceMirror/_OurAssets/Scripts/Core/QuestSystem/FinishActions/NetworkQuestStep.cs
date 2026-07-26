using Fusion;
using UnityEngine;

/// <summary>
/// Opt-in network sync for a single QuestStep. Add this component to the same
/// GameObject as a QuestStep to share that ONE step's completion across all clients.
///
/// How it works (Opt-In Networking pattern):
///   - QuestStep (MonoBehaviour) owns all gameplay logic and stays untouched.
///   - QuestManager (IQuestService) has no network knowledge whatsoever.
///   - This NetworkBehaviour listens to QuestStep.OnStepFinished and broadcasts
///     the completion via RPC so every client advances their quest together.
///   - On remote clients, completion is triggered through QuestStep.CompleteExternally()
///     which feeds back into the normal event chain (QuestStep → QuestManager).
///   - [Networked] property persists the finished flag for late-joining clients.
///
/// Usage:
///   - Add to a QuestStep GameObject to sync ONLY that step.
///   - Other steps on the same quest remain local-only.
///   - Do NOT combine with NetworkQuestInfo on the same quest (both would advance it).
///
/// Inspector setup:
///   - _stepIndex : the index of this step in the parent QuestInfo.questSteps.
/// </summary>
[RequireComponent(typeof(QuestStep))]
[AddComponentMenu(QuestSystemComponentMenuPaths.Network + "/Network Quest Step")]
[DisallowMultipleComponent] 
public class NetworkQuestStep : NetworkBehaviour
{
    [Header("Quest Context")]
    [Tooltip("The index of this step in the parent QuestInfo.questSteps.")]
    [SerializeField] private int _stepIndex;

    private QuestStep _step;

    // Persists the finished state for late-joining clients.
    [Networked] private NetworkBool NetworkedIsFinished { get; set; }

    public override void Spawned()
    {
        _step = GetComponent<QuestStep>();
        _step.OnStepFinished += HandleStepFinished;

        // Apply persisted state for late-joining clients.
        if (!HasStateAuthority && NetworkedIsFinished && !_step.IsFinished)
            _step.CompleteExternally();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_step != null)
            _step.OnStepFinished -= HandleStepFinished;
    }

    private void HandleStepFinished(QuestStep step, string finalState)
    {
        if (HasStateAuthority)
            NetworkedIsFinished = true;

        if (Object != null && Object.IsValid)
            RPC_NotifyStepFinished();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_NotifyStepFinished()
    {
        // CompleteExternally is idempotent via the isFinished guard inside FinishStep.
        if (_step != null && !_step.IsFinished)
            _step.CompleteExternally();
    }
}
