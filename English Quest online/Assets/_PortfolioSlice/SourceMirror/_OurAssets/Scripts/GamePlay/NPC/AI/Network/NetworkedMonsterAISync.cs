using Fusion;
using UnityEngine;

/// <summary>
/// Replicates <see cref="MonsterAIComponent"/> over Photon Fusion Shared Mode.
/// This is the ONLY monster AI file that imports Fusion.
/// </summary>
[RequireComponent(typeof(MonsterAIComponent))]
public class NetworkedMonsterAISync : NetworkBehaviour, IStateAuthorityChanged
{
    private MonsterAIComponent _ai;
    private UnityEngine.AI.NavMeshAgent _navAgent;
    private HealthComponent _health;
    private readonly CombatAILODRunner _lodRunner = new();
    private readonly NPCNavigationHelper _navigation = new();
    private bool _attackHandlerSubscribed;
    private bool _authorityHandlersSubscribed;

    [Networked, OnChangedRender(nameof(OnNetworkedStateChanged))]
    private byte NetworkedState { get; set; }

    public override void Spawned()
    {
        _ai = GetComponent<MonsterAIComponent>();
        _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _health = GetComponent<HealthComponent>();

        ApplyAuthorityState();
        UpdateAuthorityHandlers();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeAuthorityHandlers();
        UnsubscribeAttackHandler();
    }

    public void StateAuthorityChanged()
    {
        _lodRunner.ResetOnAuthorityChange();
        ApplyAuthorityState();
        UpdateAuthorityHandlers();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || _ai == null || _ai.IsDead || _ai.Config == null)
            return;

        NPCDistanceLODSettings lodSettings = _ai.Config.GetAILodSettings();

        if (_lodRunner.ShouldRecalculateLOD(lodSettings))
            _lodRunner.TryRecalculateLOD(transform, Runner, lodSettings, _ai, HandleAITierChanged);

        if (!_lodRunner.ShouldTick(lodSettings))
            return;

        RefreshNetworkTarget();
        _ai.Tick(Runner.DeltaTime);
    }

    private void HandleAuthorityStateChanged(MonsterAIComponent.MonsterAIState state)
    {
        NetworkedState = (byte)state;
    }

    private void OnNetworkedStateChanged()
    {
        if (HasStateAuthority || _ai == null)
            return;

        _ai.NetworkApplyAnimatorState((MonsterAIComponent.MonsterAIState)NetworkedState);
    }

    private void HandleDamageTakenForLOD(float amount, DamageInfo info)
    {
        if (!HasStateAuthority || _ai == null || _ai.IsDead)
            return;

        _ai.CancelPendingAttackHit();

        _lodRunner.ForceCombatWake();

        if (_lodRunner.IsDormant && _navAgent != null)
        {
            _navAgent.enabled = true;
            _navigation.Resume(_navAgent);
        }
    }

    private void HandleAITierChanged(NPCDistanceTier previousTier, NPCDistanceTier newTier)
    {
        if (_navAgent == null)
            return;

        if (newTier == NPCDistanceTier.Dormant)
        {
            _ai.SuspendForDistanceLOD();
            _navAgent.enabled = false;
            return;
        }

        if (previousTier == NPCDistanceTier.Dormant)
        {
            _navAgent.enabled = true;
            _navigation.Resume(_navAgent);
        }
    }

    private void HandleAttackHit(Transform target, float damage, DamageInfo info)
    {
        if (!HasStateAuthority || target == null || _ai == null || !IsValidTarget(target))
            return;

        RPC_PlayAttack();

        var healthSync = CombatTargetHealthResolver.FindHealthSync(target);
        if (healthSync != null)
        {
            healthSync.RequestDamage(damage, info, gameObject);
            return;
        }

        var health = CombatTargetHealthResolver.FindHealth(target);
        if (health != null)
        {
            health.TakeDamage(damage, info);

            if (!health.IsAlive)
                health.NotifyDied(new DeathContext(gameObject));
        }
    }

    private void ApplyAuthorityState()
    {
        bool isAuthority = HasStateAuthority;

        if (_navAgent != null)
            _navAgent.enabled = isAuthority && !_lodRunner.IsDormant;

        if (isAuthority)
        {
            SubscribeAttackHandler();
            NetworkedState = (byte)_ai.CurrentState;
        }
        else
        {
            UnsubscribeAttackHandler();
            _ai.NetworkApplyAnimatorState((MonsterAIComponent.MonsterAIState)NetworkedState);
        }
    }

    private void SubscribeAttackHandler()
    {
        if (_attackHandlerSubscribed || _ai == null)
            return;

        _ai.OnAttackHit += HandleAttackHit;
        _attackHandlerSubscribed = true;
    }

    private void UnsubscribeAttackHandler()
    {
        if (!_attackHandlerSubscribed || _ai == null)
            return;

        _ai.OnAttackHit -= HandleAttackHit;
        _attackHandlerSubscribed = false;
    }

    private void UpdateAuthorityHandlers()
    {
        if (HasStateAuthority)
            SubscribeAuthorityHandlers();
        else
            UnsubscribeAuthorityHandlers();
    }

    private void SubscribeAuthorityHandlers()
    {
        if (_authorityHandlersSubscribed || _ai == null)
            return;

        _ai.OnStateChanged += HandleAuthorityStateChanged;
        if (_health != null)
            _health.OnDamageTaken += HandleDamageTakenForLOD;

        _authorityHandlersSubscribed = true;
    }

    private void UnsubscribeAuthorityHandlers()
    {
        if (!_authorityHandlersSubscribed || _ai == null)
            return;

        _ai.OnStateChanged -= HandleAuthorityStateChanged;
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTakenForLOD;

        _authorityHandlersSubscribed = false;
    }

    private void RefreshNetworkTarget()
    {
        if (_ai == null || _ai.Config == null)
            return;

        if (_ai.Config.aggroPlayersOnly)
        {
            Transform nearestPlayer = FindNearestPlayerTransform(_ai.Config.aggroRange);
            if (nearestPlayer != null)
            {
                _ai.SetTarget(nearestPlayer);
                return;
            }

            _ai.ClearTarget();
            return;
        }

        Transform nearest = NPCTargetScanner.FindNearestAggroTarget(
            transform,
            _ai.Config.aggroRange,
            playersOnly: false);
        if (nearest != null)
        {
            _ai.SetTarget(nearest);
            return;
        }

        if (_ai.CurrentTarget != null &&
            _ai.IsTargetWithinAggro(_ai.CurrentTarget) &&
            IsValidTarget(_ai.CurrentTarget))
            return;

        _ai.ClearTarget();
    }

    private bool IsValidTarget(Transform target)
    {
        if (_ai == null || _ai.Config == null || target == null)
            return false;

        return NPCTargetScanner.IsValidAggroTarget(transform, target, _ai.Config.aggroPlayersOnly);
    }

    private Transform FindNearestPlayerTransform(float aggroRange)
    {
        if (Runner == null || aggroRange <= 0f)
            return null;

        Transform nearest = null;
        float nearestSqDist = float.MaxValue;
        float aggroSq = aggroRange * aggroRange;
        Vector3 monsterPos = transform.position;

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
            if (playerObj == null)
                continue;

            Vector3 delta = playerObj.transform.position - monsterPos;
            delta.y = 0f;
            float sqDist = delta.sqrMagnitude;
            if (sqDist > aggroSq || sqDist >= nearestSqDist)
                continue;

            nearestSqDist = sqDist;
            nearest = playerObj.transform;
        }

        if (nearest != null)
            return nearest;

        Transform localFallback = LocalPlayerRegistry.GetLocalTransform(Runner);
        if (localFallback == null)
            return null;

        Vector3 localDelta = localFallback.position - monsterPos;
        localDelta.y = 0f;
        return localDelta.sqrMagnitude <= aggroSq ? localFallback : null;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_PlayAttack()
    {
        if (_ai == null)
            _ai = GetComponent<MonsterAIComponent>();

        _ai.NetworkPlayAttackAnimation();
    }
}
