using Fusion;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : NetworkBehaviour, IMovementState
{
    [Header("Feel Feedbacks")]
    [FormerlySerializedAs("_landingFeel")]
    [SerializeField] private MMF_Player _jumpFeel;
    private bool _wasGroundedLastFrame;

    [Header("Form Settings")]
    [SerializeField] private AnimalFormDefinitionSO _currentForm;

    [Header("Rotation Settings")]
    [SerializeField] private float _inPlaceRotationSpeed = 10f;
    [SerializeField] private float _lookDirectionDeadzone = 0.001f;
    [SerializeField] private float _movementInputDeadzone = 0.01f;

    [Header("References")]
    [SerializeField] private NetworkCharacterController _characterController;
    [SerializeField] private GroundDetector _groundDetector; // Optional, kept for reference
    [SerializeField] private PlayerFormSwitcher _formSwitcher;
    
    private MovementContext _ctx;
    
    // State
    [Networked] private NetworkButtons _buttonsPrevious { get; set; }
    
    // Made Networked so PlayerAnimator can read it correctly on Proxies
    [Networked] public NetworkBool IsGrounded { get; set; }
    [Networked] public float VerticalVelocity { get; set; }

    // Explicit implementations so NetworkBool/float satisfy the IMovementState bool contract
    bool IMovementState.IsGrounded => IsGrounded;
    float IMovementState.VerticalVelocity => VerticalVelocity;

    public override void Spawned()
    {        
        ResolveReferences();
        InitializeContext();
        SubscribeToEvents();

        // Set initial stats
        if (_currentForm != null)
        {
            ApplyForm(_currentForm);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_ctx == null) return;

        UpdateEnvironmentState();

        // GetInput hydrates the input struct with data from OnInput
        if (GetInput(out NetworkInputData input))
        {
            UpdateInputState(input);
            ExecuteMovementStrategy(input);
            UpdateKinematicsState();
            // Store current buttons to compare against next frame for "ButtonDown" logic
            _buttonsPrevious = input.Buttons;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_formSwitcher != null)
        {
            _formSwitcher.OnFormChanged -= HandleFormChanged;
        }
        if (_groundDetector != null)
        {
            _groundDetector.OnGroundedStatusChanged -= HandleGroundedStatusChanged;
        }
    }

    #region Initialization

    private void ResolveReferences()
    {
        if (_characterController == null) _characterController = PlayerRoot.Resolve<NetworkCharacterController>(this);
        if (_groundDetector == null) _groundDetector = GetComponent<GroundDetector>();
        if (_formSwitcher == null) _formSwitcher = PlayerRoot.Resolve<PlayerFormSwitcher>(this);
    }

    private void InitializeContext()
    {
        // Initialize Context
        if (_characterController != null)
        {
            Transform check = _groundDetector ? _groundDetector.GroundCheck : null;
            float radius = _groundDetector ? _groundDetector.GroundRadius : 0.2f;
            LayerMask mask = _groundDetector ? _groundDetector.GroundLayer : (LayerMask)1;

            _ctx = new MovementContext(
                transform, 
                _characterController, 
                check, 
                radius, 
                mask
            );
        }
    }

    private void SubscribeToEvents()
    {
        if (_formSwitcher != null)
        {
            _formSwitcher.OnFormChanged += HandleFormChanged;
            if (_formSwitcher.CurrentForm != null)
            {
                HandleFormChanged(_formSwitcher.CurrentForm);
            }
        }

        if (_groundDetector != null)
        {
            _groundDetector.OnGroundedStatusChanged += HandleGroundedStatusChanged;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleGroundedStatusChanged(bool isGrounded)
    {
        // Sync networked state
        IsGrounded = isGrounded;
    }

    private void HandleFormChanged(AnimalFormDefinitionSO form)
    {
        _currentForm = form;

        if (form != null)
        {
            ApplyForm(form);
        }
        
        if (_characterController == null) _characterController = PlayerRoot.Resolve<NetworkCharacterController>(this);
    }

    #endregion

    #region Logic

    private void ApplyForm(AnimalFormDefinitionSO form)
    {
        if (_ctx == null) return;

        _ctx.moveSpeed = form.moveSpeed;
        _ctx.jumpImpulse = form.jumpImpulse;
        
        // Update NCC Settings
        if (_characterController != null)
        {
            _characterController.maxSpeed = form.moveSpeed;
            _characterController.jumpImpulse = form.jumpImpulse;
            
            // Note: Collider resizing (radius/height) usually happens here if needed
        }
    }

    private void UpdateEnvironmentState()
    {
        // We trigger the detector to check environment, it will callback if state changes
        if (_groundDetector != null)
        {
            // Pass current IsGrounded state to handle rollbacks correctly
            _groundDetector.Detect(transform.position, IsGrounded);
        }
        
        // Update Context
        _ctx.grounded = IsGrounded;
        _ctx.deltaTime = Runner.DeltaTime;
    }

    private void PerformGroundCheck()
    {
        // Deprecated in favor of GroundDetector observer pattern
    }

    private void UpdateInputState(NetworkInputData input)
    {
        // Populate Context Input
        _ctx.inputX = input.MoveDirection.x;
        _ctx.inputZ = input.MoveDirection.z;
        _ctx.lookDirection = input.LookDirection;
        _ctx.jumpPressed = input.Buttons.IsSet(InputButton.Jump) && !_buttonsPrevious.IsSet(InputButton.Jump);
        _ctx.sprintHeld = input.Buttons.IsSet(InputButton.Sprint);
        // Add other inputs as needed
    }

    private void ExecuteMovementStrategy(NetworkInputData input)
    {
        // Execute Strategy
        if (_currentForm != null && _currentForm.movementStrategy != null)
        {
            _currentForm.movementStrategy.FixedMove(_ctx);

            HandleRotation(input.LookDirection);
            
            // Trigger Jump Animation if needed
            if (_ctx.grounded && _ctx.jumpPressed)
            {
                // Animation triggers usually go here
                if (_jumpFeel != null)
                {
                    _jumpFeel.PlayFeedbacks();
                }
            }
        }
        else
        {
            // Create fallback or default behavior if form is missing
            HandleFallbackMovement(input.MoveDirection);
        }
    }

    private void HandleRotation(Vector3 lookDirection)
    {
        // If not moving, we allow rotating in place based on look direction
        // When moving, NCC handles rotation automatically in Move() usually

        bool hasMovementInput = Mathf.Abs(_ctx.inputX) > _movementInputDeadzone || Mathf.Abs(_ctx.inputZ) > _movementInputDeadzone;
        if (_characterController != null && !hasMovementInput && lookDirection.sqrMagnitude > _lookDirectionDeadzone)
        {
             Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
             _characterController.transform.rotation = Quaternion.Slerp(
                 _characterController.transform.rotation,
                 targetRotation,
                 _inPlaceRotationSpeed * Runner.DeltaTime
             );
        }
    }

    private void UpdateKinematicsState()
    {
        if (_characterController != null)
        {
            VerticalVelocity = _characterController.Velocity.y;
        }
    }

    private void HandleFallbackMovement(Vector3 direction)
    {
        if (_characterController != null)
        {
            _characterController.Move(direction);
        }
    }

    #endregion
}
