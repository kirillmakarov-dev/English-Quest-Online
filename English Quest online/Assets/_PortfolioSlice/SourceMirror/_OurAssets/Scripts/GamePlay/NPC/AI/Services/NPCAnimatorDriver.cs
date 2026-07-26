using UnityEngine;

/// <summary>
/// Drives integer animator locomotion states with a temporary hit-react overlay.
/// </summary>
public class NPCAnimatorDriver
{
    private Animator _animator;
    private ICombatAIConfig _config;
    private bool _isDead;
    private bool _isPlayingHitAnim;
    private float _hitAnimTimer;

    public bool IsPlayingHitAnim => _isPlayingHitAnim;

    public void Initialize(Animator animator, ICombatAIConfig config)
    {
        _animator = animator;
        _config = config;
    }

    public void SetConfig(ICombatAIConfig config)
    {
        _config = config;
    }

    public void SetDead(bool isDead)
    {
        _isDead = isDead;
        if (_isDead)
            _isPlayingHitAnim = false;
    }

    public void ApplyLocomotion(int stateValue)
    {
        if (_isDead || _config == null || _animator == null || _isPlayingHitAnim)
            return;

        _animator.SetInteger(_config.animatorStateName, stateValue);
    }

    public void ApplyNetworkState(CombatAIState state)
    {
        if (_isDead || _config == null || _animator == null)
            return;

        int animState = state switch
        {
            CombatAIState.Chase => _config.runState,
            CombatAIState.Attack => _config.attackState,
            _ => _config.idleState
        };

        _animator.SetInteger(_config.animatorStateName, animState);
    }

    public void PlayAttackAnimation()
    {
        if (_isDead || _config == null || _animator == null)
            return;

        _animator.SetInteger(_config.animatorStateName, _config.attackState);

        if (!string.IsNullOrEmpty(_config.attackAnimStateName))
            _animator.Play(_config.attackAnimStateName, 0, 0f);
    }

    public void PlayHitAnimation()
    {
        if (_config == null || _animator == null)
            return;

        _isPlayingHitAnim = true;
        _hitAnimTimer = _config.hitAnimDuration;
        _animator.SetInteger(_config.animatorStateName, _config.hitState);
    }

    public void PlayDeathAnimation()
    {
        if (_config == null || _animator == null)
            return;

        _isPlayingHitAnim = false;
        _animator.SetInteger(_config.animatorStateName, _config.deathState);
    }

    public void TickHitAnimation(float deltaTime, CombatAIState currentState)
    {
        if (!_isPlayingHitAnim)
            return;

        _hitAnimTimer -= deltaTime;
        if (_hitAnimTimer > 0f)
            return;

        _isPlayingHitAnim = false;

        int resumeState = currentState switch
        {
            CombatAIState.Chase => _config.runState,
            CombatAIState.Attack => _config.attackState,
            _ => _config.idleState
        };
        ApplyLocomotion(resumeState);
    }
}
