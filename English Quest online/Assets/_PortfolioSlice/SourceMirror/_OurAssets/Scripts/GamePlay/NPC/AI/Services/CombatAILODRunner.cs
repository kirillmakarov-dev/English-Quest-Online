using Fusion;
using UnityEngine;

/// <summary>
/// Distance-based AI tick throttling for utility combat monsters.
/// Used by <see cref="NetworkedMonsterAISync"/> (network) and <see cref="MonsterAIComponent"/> (local).
/// </summary>
public sealed class CombatAILODRunner
{
    private NPCDistanceTier _currentTier = (NPCDistanceTier)(-1);
    private int _tickCounter;
    private int _recalcFrameCounter;
    private bool _combatOverride;

    public NPCDistanceTier CurrentTier =>
        _combatOverride ? NPCDistanceTier.Full : _currentTier;

    public bool IsDormant => CurrentTier == NPCDistanceTier.Dormant;

    public void ForceCombatWake()
    {
        _combatOverride = true;
    }

    public void ResetOnAuthorityChange()
    {
        _currentTier = (NPCDistanceTier)(-1);
        _tickCounter = 0;
        _recalcFrameCounter = 0;
        _combatOverride = false;
    }

    public bool ShouldRecalculateLOD(NPCDistanceLODSettings settings)
    {
        return _recalcFrameCounter++ % settings.recalcIntervalFrames == 0;
    }

    public bool TryRecalculateLOD(
        Transform owner,
        NetworkRunner runner,
        NPCDistanceLODSettings settings,
        MonsterAIComponent ai,
        System.Action<NPCDistanceTier, NPCDistanceTier> onTierChanged)
    {
        bool hadCombatOverride = _combatOverride;
        _combatOverride = ai != null
            && ai.CurrentTarget != null
            && ai.IsTargetWithinLeash(ai.CurrentTarget);

        NPCDistanceTier newTier;
        if (_combatOverride)
        {
            newTier = NPCDistanceTier.Full;
        }
        else
        {
            Transform nearestPlayer = NPCDistanceLOD.FindNearestPlayerTransform(runner, owner.position);
            if (nearestPlayer == null)
                return false;

            float distance = NPCDistanceLOD.HorizontalDistance(owner.position, nearestPlayer.position);
            newTier = NPCDistanceLOD.EvaluateTier(distance, settings);
        }

        if (newTier != _currentTier || hadCombatOverride != _combatOverride)
        {
            NPCDistanceTier previousTier = _currentTier;
            _currentTier = newTier;
            _tickCounter = 0;
            onTierChanged?.Invoke(previousTier, newTier);
        }

        return true;
    }

    public bool ShouldTick(NPCDistanceLODSettings settings)
    {
        if (IsDormant)
            return false;

        int interval = NPCDistanceLOD.TickIntervalForTier(CurrentTier, settings);
        if (interval == int.MaxValue)
            return false;

        return _tickCounter++ % interval == 0;
    }
}
