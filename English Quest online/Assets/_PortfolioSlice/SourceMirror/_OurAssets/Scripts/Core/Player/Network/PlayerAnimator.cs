using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private const string SpeedParamName    = "speed";
    private const string GroundedParamName = "Grounded";
    private const string VerticalParamName = "Vertical";

    [Header("References")]
    [SerializeField] private PlayerMovement    _playerMovement;
    [SerializeField] private PlayerFormSwitcher _formSwitcher;

    [Header("Settings")]
    [Tooltip("How fast the float value changes. Higher = smoother/slower.")]
    [SerializeField] private float _dampTime = 0.15f;
    [Tooltip("Additional smoothing for speed calculation from position delta. Higher = more stable.")]
    [SerializeField] private float _speedSmoothing = 10f;
    [Tooltip("Ignore tiny speed changes to reduce jitter.")]
    [SerializeField] private float _speedDeadzone = 0.05f;
    [Tooltip("Maximum allowed speed value to reject sudden spikes (teleports/jitter).")]
    [SerializeField] private float _maxVisualSpeed = 20f;
    [SerializeField] private bool  _useJumpFallParams = true;
    [SerializeField] private string _jumpParamName = "Jumping";
    [SerializeField] private string _fallParamName = "Falling";
    [SerializeField] private float _verticalDeadzone = 0.1f;

    private Animator _animator;
    private int _animSpeedParam;
    private int _animGroundedParam;
    private int _verticalParam;
    private int _jumpParam;
    private int _fallParam;
    private bool _hasJumpParam;
    private bool _hasFallParam;

    private IMovementState _movementState;
    private VisualSpeedCalculator _speedCalculator;
    private readonly List<IAnimatorDriver> _drivers = new List<IAnimatorDriver>();

    /// <summary>
    /// Registers an animator driver. The driver will immediately receive
    /// OnAnimatorAcquired if an animator is already available.
    /// </summary>
    public void RegisterDriver(IAnimatorDriver driver)
    {
        if (_drivers.Contains(driver)) return;

        _drivers.Add(driver);

        if (_animator != null)
            driver.OnAnimatorAcquired(_animator);
    }

    public void UnregisterDriver(IAnimatorDriver driver)
    {
        if (!_drivers.Remove(driver)) return;

        if (_animator != null)
            driver.Drive(_animator);
    }

    public override void Spawned()
    {
        _speedCalculator.Initialize(transform.position);

        _animSpeedParam    = Animator.StringToHash(SpeedParamName);
        _animGroundedParam = Animator.StringToHash(GroundedParamName);
        _verticalParam     = Animator.StringToHash(VerticalParamName);
        _jumpParam         = Animator.StringToHash(_jumpParamName);
        _fallParam         = Animator.StringToHash(_fallParamName);

        if (_playerMovement == null) _playerMovement = PlayerRoot.Resolve<PlayerMovement>(this);
        if (_formSwitcher   == null) _formSwitcher   = PlayerRoot.Resolve<PlayerFormSwitcher>(this);

        _movementState = _playerMovement;

        if (_formSwitcher != null)
        {
            _formSwitcher.OnFormChanged += HandleFormChanged;
            if (_formSwitcher.CurrentForm != null)
                HandleFormChanged(_formSwitcher.CurrentForm);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_formSwitcher != null)
            _formSwitcher.OnFormChanged -= HandleFormChanged;
    }

    private void HandleFormChanged(AnimalFormDefinitionSO form)
    {
        if (_formSwitcher != null)
        {
            _animator = _formSwitcher.CurrentAnimator;
            CacheAnimatorParams();
        }
    }

    public override void Render()
    {
        if (_animator == null)
        {
            if (_formSwitcher != null) _animator = _formSwitcher.CurrentAnimator;
            if (_animator == null) return;
            CacheAnimatorParams();
        }

        bool suppressSpeed = false;
        foreach (IAnimatorDriver driver in _drivers)
        {
            if (driver.SuppressMovementSpeed) { suppressSpeed = true; break; }
        }

        float smoothedSpeed = _speedCalculator.Compute(
            transform.position, _speedSmoothing, _speedDeadzone, _maxVisualSpeed, Time.deltaTime);

        _animator.SetFloat(_animSpeedParam, suppressSpeed ? 0f : smoothedSpeed, _dampTime, Time.deltaTime);

        foreach (IAnimatorDriver driver in _drivers)
            driver.Drive(_animator);

        if (_movementState != null)
        {
            _animator.SetBool(_animGroundedParam, _movementState.IsGrounded);
            _animator.SetFloat(_verticalParam, _movementState.VerticalVelocity);

            if (_useJumpFallParams && (_hasJumpParam || _hasFallParam))
            {
                bool  inAir     = !_movementState.IsGrounded;
                float vertVel   = _movementState.VerticalVelocity;
                bool  isJumping = inAir && vertVel >  _verticalDeadzone;
                bool  isFalling = inAir && vertVel < -_verticalDeadzone;

                if (_hasJumpParam) _animator.SetBool(_jumpParam, isJumping);
                if (_hasFallParam) _animator.SetBool(_fallParam, isFalling);
            }
        }
    }

    private void CacheAnimatorParams()
    {
        _hasJumpParam = AnimatorParamUtils.HasParam(_animator, _jumpParam, AnimatorControllerParameterType.Bool);
        _hasFallParam = AnimatorParamUtils.HasParam(_animator, _fallParam, AnimatorControllerParameterType.Bool);

        foreach (IAnimatorDriver driver in _drivers)
            driver.OnAnimatorAcquired(_animator);
    }
}
