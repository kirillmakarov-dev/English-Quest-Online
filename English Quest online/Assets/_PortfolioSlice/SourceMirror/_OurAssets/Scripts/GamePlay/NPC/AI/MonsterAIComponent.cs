using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Fusion-free chase-and-attack brain for monster NPCs.
/// Works standalone (local/offline) or alongside <see cref="NetworkedMonsterAISync"/> for multiplayer.
/// </summary>
[RequireComponent(typeof(NPCVisualLODController))]
public class MonsterAIComponent : MonoBehaviour
{
    public enum MonsterAIState
    {
        Idle = CombatAIState.Idle,
        Chase = CombatAIState.Chase,
        Attack = CombatAIState.Attack
    }

    [SerializeField] private MonsterBehaviorConfigSO _config;
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _attackAudioSource;

    private readonly CombatAIBrain _brain = new();
    private readonly CombatAIContext _ctx = new();
    private readonly MeleeAttackUtilityAction _meleeAttack = new();
    private readonly NPCNavigationHelper _navigation = new();
    private readonly NPCAnimatorDriver _animatorDriver = new();

    private HealthComponent _health;
    private NPCVisualLODController _visualLOD;
    private bool _localMode;
    private readonly CombatAILODRunner _lodRunner = new();

    public MonsterAIState CurrentState => (MonsterAIState)_brain.CurrentState;
    public bool IsDead => _ctx.IsDead;
    public Transform CurrentTarget => _ctx.CurrentTarget;
    public MonsterBehaviorConfigSO Config => _config;

    /// <summary>Fired when the internal state machine changes.</summary>
    public event Action<MonsterAIState> OnStateChanged;

    /// <summary>
    /// Fired once per attack when windup completes. Does not apply damage —
    /// <see cref="NetworkedMonsterAISync"/> or the local handler decides how to apply it.
    /// </summary>
    public event Action<Transform, float, DamageInfo> OnAttackHit;

    private void Awake()
    {
        if (_navAgent == null)
            _navAgent = GetComponent<NavMeshAgent>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _health = GetComponent<HealthComponent>();

        if (_visualLOD == null)
            _visualLOD = GetComponent<NPCVisualLODController>();

        if (_visualLOD == null)
            _visualLOD = gameObject.AddComponent<NPCVisualLODController>();

        EnsureAttackAudioSource();

        _ctx.Owner = transform;
        _ctx.NavAgent = _navAgent;
        _ctx.Navigation = _navigation;
        _ctx.AnimatorDriver = _animatorDriver;
        // Wire config in Awake: Fusion FixedUpdateNetwork can run before Start.
        _ctx.Config = _config;
        _animatorDriver.Initialize(_animator, _config);

        // Combat actions first so equal scores never fall through to Idle while a target exists.
        _brain.RegisterAction(new ChaseUtilityAction());
        _brain.RegisterAction(_meleeAttack);
        _brain.RegisterAction(new IdleUtilityAction());
        _brain.OnStateChanged += HandleBrainStateChanged;
    }

    private void Start()
    {
        _localMode = GetComponent<NetworkedMonsterAISync>() == null;

        _ctx.Config = _config;
        _ctx.OnAttackHit = (target, damage, info) => OnAttackHit?.Invoke(target, damage, info);
        _ctx.OnAttackSwingStarted = PlayAttackSound;
        _ctx.OnTargetCleared = HandleTargetCleared;
        _animatorDriver.SetConfig(_config);

        if (_localMode)
            OnAttackHit += HandleLocalAttackHit;

        if (_health != null)
        {
            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDied += HandleDied;
        }

        ConfigureNavAgent();
    }

    private void OnDestroy()
    {
        _brain.OnStateChanged -= HandleBrainStateChanged;

        if (_localMode)
            OnAttackHit -= HandleLocalAttackHit;

        if (_health != null)
        {
            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        if (!_localMode)
            return;

        if (_config == null || _ctx.IsDead)
            return;

        NPCDistanceLODSettings lodSettings = _config.GetAILodSettings();

        if (_lodRunner.ShouldRecalculateLOD(lodSettings))
            _lodRunner.TryRecalculateLOD(transform, null, lodSettings, this, HandleLocalAITierChanged);

        if (!_lodRunner.ShouldTick(lodSettings))
            return;

        RefreshLocalTarget();
        Tick(Time.deltaTime);
    }

    public void SetTarget(Transform target)
    {
        if (!IsValidTarget(target))
        {
            _ctx.CurrentTarget = null;
            return;
        }

        _ctx.CurrentTarget = target;
    }

    public void ClearTarget()
    {
        _ctx.CurrentTarget = null;
        _brain.ForceIdle(_ctx);
    }

    /// <summary>Stops movement and resets the brain when entering distance LOD dormancy.</summary>
    public void SuspendForDistanceLOD()
    {
        _navigation.Stop(_navAgent);
        _navigation.ResetPath(_navAgent);
        _brain.ForceIdle(_ctx);
    }

    /// <summary>
    /// Drops pending melee damage when the monster is struck during attack windup.
    /// Called from local damage handling and networked authority sync.
    /// </summary>
    public void CancelPendingAttackHit()
    {
        _meleeAttack.CancelPendingHit(_ctx);
    }

    /// <summary>Advances the utility brain. Call from Update (local) or FixedUpdateNetwork (network sync).</summary>
    public void Tick(float deltaTime)
    {
        if (_ctx.IsDead || _config == null)
            return;

        _animatorDriver.TickHitAnimation(deltaTime, _brain.CurrentState);
        _brain.Tick(_ctx, deltaTime);
    }

    /// <summary>
    /// Triggers the hit animation without interrupting AI logic.
    /// Call on proxy clients from NetworkedMonsterAISync to replicate the visual.
    /// </summary>
    public void PlayHitAnimation()
    {
        _animatorDriver.PlayHitAnimation();
    }

    /// <summary>Offline target scan — nearest collider with HealthComponent or Player tag within aggro range.</summary>
    public void FindLocalTarget()
    {
        RefreshLocalTarget();
    }

    /// <summary>Re-scans aggro each frame so target loss does not strand the brain in Idle.</summary>
    public void RefreshLocalTarget()
    {
        if (_config == null)
            return;

        Transform found = NPCTargetScanner.FindNearestAggroTarget(
            transform,
            _config.aggroRange,
            _config.aggroPlayersOnly);
        if (found != null)
        {
            _ctx.CurrentTarget = found;
            return;
        }

        if (_ctx.CurrentTarget != null &&
            IsTargetWithinAggro(_ctx.CurrentTarget) &&
            IsValidTarget(_ctx.CurrentTarget))
            return;

        _ctx.CurrentTarget = null;
    }

    public bool IsTargetWithinAggro(Transform target)
    {
        if (target == null || _config == null)
            return false;

        return _navigation.DistanceToTarget(transform, target) <= _config.aggroRange;
    }

    public bool IsTargetWithinLeash(Transform target)
    {
        if (target == null || _config == null)
            return false;

        return _navigation.DistanceToTarget(transform, target) <= _config.leashRange;
    }

    /// <summary>Applies animator state from replicated network data on proxy clients.</summary>
    public void NetworkApplyAnimatorState(MonsterAIState state)
    {
        if (_visualLOD != null && _visualLOD.IsVisualHidden)
            return;

        _animatorDriver.ApplyNetworkState((CombatAIState)state);
    }

    /// <summary>Plays the attack animator state without running AI logic (proxy clients).</summary>
    public void NetworkPlayAttackAnimation()
    {
        if (_visualLOD != null && _visualLOD.IsVisualHidden)
            return;

        _animatorDriver.PlayAttackAnimation();
        PlayAttackSound();
    }

    /// <summary>Plays the configured attack SFX with positional 3D audio.</summary>
    public void PlayAttackSound()
    {
        if (_config == null || _attackAudioSource == null)
            return;

        if (_visualLOD != null && _visualLOD.IsVisualHidden)
            return;

        AudioClip clip = _config.GetRandomAttackSound();
        if (clip == null)
            return;

        _attackAudioSource.PlayOneShot(clip, _config.attackSoundVolume);
    }

    private void EnsureAttackAudioSource()
    {
        if (_attackAudioSource == null)
            _attackAudioSource = GetComponent<AudioSource>();

        if (_attackAudioSource != null)
            return;

        _attackAudioSource = gameObject.AddComponent<AudioSource>();
        _attackAudioSource.playOnAwake = false;
        _attackAudioSource.spatialBlend = 1f;
        _attackAudioSource.minDistance = 2f;
        _attackAudioSource.maxDistance = 25f;
    }

    private void ConfigureNavAgent()
    {
        _navigation.ConfigureStoppingDistance(_navAgent, _config);
    }

    private void HandleBrainStateChanged(CombatAIState state)
    {
        OnStateChanged?.Invoke((MonsterAIState)state);
    }

    private void HandleTargetCleared()
    {
        _brain.ForceIdle(_ctx);
    }

    private bool IsValidTarget(Transform target)
    {
        if (target == null || _config == null)
            return false;

        return NPCTargetScanner.IsValidAggroTarget(transform, target, _config.aggroPlayersOnly);
    }

    private void HandleLocalAttackHit(Transform target, float damage, DamageInfo info)
    {
        if (target == null || !IsValidTarget(target))
            return;

        var health = CombatTargetHealthResolver.FindHealth(target);
        if (health == null)
            return;

        health.TakeDamage(damage, info);

        if (!health.IsAlive)
            health.NotifyDied(new DeathContext(gameObject));
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (_ctx.IsDead || (_health != null && !_health.IsAlive))
            return;

        if (_localMode)
        {
            CancelPendingAttackHit();
            _lodRunner.ForceCombatWake();
            if (_lodRunner.IsDormant && _navAgent != null)
            {
                _navAgent.enabled = true;
                _navigation.Resume(_navAgent);
            }
        }

        PlayHitAnimation();
    }

    private void HandleDied(DeathContext _)
    {
        if (_ctx.IsDead)
            return;

        _ctx.IsDead = true;
        _animatorDriver.SetDead(true);
        _ctx.CurrentTarget = null;
        _navigation.Stop(_navAgent);
        _navigation.ResetPath(_navAgent);

        if (_navAgent != null)
            _navAgent.enabled = false;

        _animatorDriver.PlayDeathAnimation();
    }

    private void HandleLocalAITierChanged(NPCDistanceTier previousTier, NPCDistanceTier newTier)
    {
        if (_navAgent == null)
            return;

        if (newTier == NPCDistanceTier.Dormant)
        {
            SuspendForDistanceLOD();
            _navAgent.enabled = false;
            return;
        }

        if (previousTier == NPCDistanceTier.Dormant)
        {
            _navAgent.enabled = true;
            _navigation.Resume(_navAgent);
        }
    }
}
