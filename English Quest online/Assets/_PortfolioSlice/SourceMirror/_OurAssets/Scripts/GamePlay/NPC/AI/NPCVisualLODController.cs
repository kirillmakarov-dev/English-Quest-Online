using Fusion;
using UnityEngine;

public enum NPCVisualTier
{
    Full = 0,
    CulledAnim = 1,
    Hidden = 2
}

/// <summary>
/// Client-local visual LOD for combat monsters. Disables distant renderers and
/// culls animator updates to reduce skinned-mesh cost.
/// </summary>
[DisallowMultipleComponent]
public class NPCVisualLODController : MonoBehaviour
{
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private Animator _animator;

    private MonsterAIComponent _ai;
    private HealthComponent _health;
    private Renderer[] _renderers;
    private NPCVisualTier _currentTier = (NPCVisualTier)(-1);
    private int _recalcFrameCounter;
    private float _forceFullVisualUntil;
    private AnimatorCullingMode _fullAnimatorCullingMode = AnimatorCullingMode.CullUpdateTransforms;

    public bool IsVisualHidden => _currentTier == NPCVisualTier.Hidden;

    private void Awake()
    {
        _ai = GetComponent<MonsterAIComponent>();
        _health = GetComponent<HealthComponent>();

        if (_visualRoot == null)
        {
            Transform visual = transform.Find("Visual");
            if (visual != null)
                _visualRoot = visual;
        }

        if (_animator == null && _visualRoot != null)
            _animator = _visualRoot.GetComponentInChildren<Animator>();

        if (_visualRoot != null)
            _renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);

        if (_animator != null)
            _fullAnimatorCullingMode = _animator.cullingMode;
    }

    private void Start()
    {
        if (_health != null)
            _health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (_ai == null || _ai.Config == null)
            return;

        _forceFullVisualUntil = Time.time + _ai.Config.hitAnimDuration;
        ApplyVisualTier(NPCVisualTier.Full);
    }

    private void Update()
    {
        if (_ai == null || _ai.Config == null)
            return;

        if (_ai.IsDead)
        {
            ApplyVisualTier(NPCVisualTier.Full);
            return;
        }

        MonsterBehaviorConfigSO config = _ai.Config;
        if (_recalcFrameCounter++ % config.visualRecalcIntervalFrames != 0)
            return;

        RecalculateVisualTier(config);
    }

    private void RecalculateVisualTier(MonsterBehaviorConfigSO config)
    {
        if (ShouldForceFullVisual())
        {
            ApplyVisualTier(NPCVisualTier.Full);
            return;
        }

        Transform nearestPlayer = NPCDistanceLOD.FindNearestPlayerTransform(GetRunner(), transform.position);
        if (nearestPlayer == null)
            return;

        float distance = NPCDistanceLOD.HorizontalDistance(transform.position, nearestPlayer.position);
        NPCVisualTier newTier = distance < config.visualFullDistance
            ? NPCVisualTier.Full
            : distance < config.visualHiddenDistance
                ? NPCVisualTier.CulledAnim
                : NPCVisualTier.Hidden;

        ApplyVisualTier(newTier);
    }

    private bool ShouldForceFullVisual()
    {
        if (Time.time < _forceFullVisualUntil)
            return true;

        if (_ai.CurrentState == MonsterAIComponent.MonsterAIState.Attack)
            return true;

        if (_ai.IsDead)
            return true;

        if (_health != null && !_health.IsAlive)
            return true;

        return false;
    }

    private void ApplyVisualTier(NPCVisualTier tier)
    {
        if (tier == _currentTier)
            return;

        _currentTier = tier;

        switch (tier)
        {
            case NPCVisualTier.Full:
                SetRenderersEnabled(true);
                SetAnimatorCulling(_fullAnimatorCullingMode);
                break;

            case NPCVisualTier.CulledAnim:
                SetRenderersEnabled(true);
                SetAnimatorCulling(AnimatorCullingMode.CullCompletely);
                break;

            case NPCVisualTier.Hidden:
                SetRenderersEnabled(false);
                SetAnimatorCulling(AnimatorCullingMode.CullCompletely);
                break;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = enabled;
        }
    }

    private void SetAnimatorCulling(AnimatorCullingMode mode)
    {
        if (_animator == null)
            return;

        _animator.cullingMode = mode;
    }

    private NetworkRunner GetRunner()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        return networkObject != null && networkObject.IsValid ? networkObject.Runner : null;
    }
}
