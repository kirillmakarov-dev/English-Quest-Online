using EnglishQuest.PortfolioDemo;
using Fusion;
using System;
using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityBehaviour = UnityEngine.Behaviour;

/// <summary>
/// Bridges the local Starter Assets controller with Fusion Shared authority.
/// The owning peer drives gameplay; remote peers only render replicated state.
/// </summary>
public sealed class NetworkStarterAssetsPlayer : NetworkBehaviour
{
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }
    [Networked] private Vector3 NetworkVelocity { get; set; }
    [Networked] private float NetworkAnimationSpeed { get; set; }
    [Networked] private float NetworkMotionSpeed { get; set; }
    [Networked] private NetworkBool NetworkGrounded { get; set; }
    [Networked] private NetworkBool NetworkJumping { get; set; }
    [Networked] private NetworkBool NetworkFreeFalling { get; set; }

    [Header("Remote Visual Smoothing")]
    [SerializeField] private float _remotePositionSharpness = 18f;
    [SerializeField] private float _remoteRotationSharpness = 20f;
    [SerializeField] private float _remotePredictionTime = 0.08f;
    [SerializeField] private float _remoteSnapDistance = 3f;

    [Header("Remote Player Collision")]
    [SerializeField] private float _remoteCollisionHeight = 1.8f;
    [SerializeField] private float _remoteCollisionRadius = 0.32f;
    [SerializeField] private Vector3 _remoteCollisionCenter = new(0f, 0.93f, 0f);

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int FreeFallId = Animator.StringToHash("FreeFall");

    private CharacterController _characterController;
    private Animator _animator;
    private PlayerInteraction _interaction;
    private PlayerInteractionController _interactionController;
    private PortfolioPlayerLockService _lockService;
    private UnityBehaviour _starterController;
    private UnityBehaviour _starterInputs;
    private UnityBehaviour _playerInput;
    private Transform _cameraTarget;
    private CinemachineCamera[] _playerVirtualCameras;
    private CapsuleCollider _remoteCollisionBlocker;
    private Vector3 _lastCapturedPosition;
    private bool _lastOwnsPlayer;
    private bool _lastOwnsLocalView;
    private bool _hasCapturedState;
    private bool _hasRemoteVisualState;

    public override void Spawned()
    {
        ResolveComponents();

        RefreshLocalState(force: true);

        if (Object.HasStateAuthority)
            CaptureState();
    }

    private void Update()
    {
        RefreshLocalState(force: false);
    }

    private void LateUpdate()
    {
        if (!NetworkPlayerOwnership.ShouldDriveLocalView(this))
            return;

        BindLocalCamera(isLocal: true);
        EnsureLocalCameraFollow();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
            CaptureState();
    }

    public override void Render()
    {
        if (NetworkPlayerOwnership.OwnsPlayer(this))
            return;

        if (_characterController != null && _characterController.enabled)
            _characterController.enabled = false;

        ApplyRemoteTransform();
        ApplyRemoteAnimation();
    }

    private void ResolveComponents()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _interaction = GetComponent<PlayerInteraction>();
        _interactionController = GetComponent<PlayerInteractionController>();
        _lockService = GetComponent<PortfolioPlayerLockService>();
        _starterController = FindBehaviour("ThirdPersonController");
        _starterInputs = FindBehaviour("StarterAssetsInputs");
        _playerInput = FindBehaviour("PlayerInput");
        _cameraTarget = transform.Find("PlayerCameraRoot");
        _playerVirtualCameras = GetComponentsInChildren<CinemachineCamera>(true);
        _remoteCollisionBlocker = EnsureRemoteCollisionBlocker();
    }

    private void RefreshLocalState(bool force)
    {
        bool ownsPlayer = NetworkPlayerOwnership.OwnsPlayer(this);
        bool ownsLocalView = NetworkPlayerOwnership.ShouldDriveLocalView(this);

        if (!force && ownsPlayer == _lastOwnsPlayer && ownsLocalView == _lastOwnsLocalView)
            return;

        BindLocalCamera(ownsLocalView);
        SetLocalBehaviourState(ownsPlayer, ownsLocalView);
        _lastOwnsPlayer = ownsPlayer;
        _lastOwnsLocalView = ownsLocalView;
    }

    private void BindLocalCamera(bool isLocal)
    {
        if (!isLocal)
            return;

        Camera sceneCamera = PlayerSceneCamera.ResolveOutputCamera(gameObject.scene);
        if (sceneCamera == null)
            return;

        // Starter Assets caches a tagged MainCamera in Awake. In multiplayer/editor
        // clones that can point at another local runner, so we override it per owner.
        SetObjectMember(_starterController, "_mainCamera", sceneCamera.gameObject);
        SetObjectMember(_playerInput, "camera", sceneCamera);
        SetObjectMember(_playerInput, "m_Camera", sceneCamera);
    }

    private void EnsureLocalCameraFollow()
    {
        Transform target = _cameraTarget != null ? _cameraTarget : transform;
        var followCamera = PlayerSceneCamera.ResolveFollowCamera(gameObject.scene, transform);
        if (followCamera == null)
            return;

        if (followCamera.Follow != target || followCamera.LookAt != target)
            PlayerSceneCamera.AssignFollow(followCamera, target);
    }

    private void SetLocalBehaviourState(bool ownsPlayer, bool ownsLocalView)
    {
        SetEnabled(_starterController, ownsPlayer);
        SetEnabled(_starterInputs, ownsLocalView);
        SetEnabled(_playerInput, ownsLocalView);
        SetPlayerVirtualCameras(ownsLocalView);

        if (_characterController != null)
            _characterController.enabled = ownsPlayer;

        SetRemoteCollisionBlocker(!ownsPlayer);

        if (_interaction != null)
            _interaction.enabled = ownsLocalView;

        if (_interactionController != null)
            _interactionController.enabled = ownsLocalView;

        _lockService?.SetLocalPlayer(ownsLocalView);
    }

    private CapsuleCollider EnsureRemoteCollisionBlocker()
    {
        const string blockerName = "Remote Player Collision Blocker";

        Transform blockerTransform = transform.Find(blockerName);
        if (blockerTransform == null)
        {
            GameObject blockerObject = new GameObject(blockerName);
            blockerTransform = blockerObject.transform;
            blockerTransform.SetParent(transform, false);
        }

        if (!blockerTransform.TryGetComponent(out CapsuleCollider blocker))
            blocker = blockerTransform.gameObject.AddComponent<CapsuleCollider>();

        blocker.isTrigger = false;
        blocker.height = _remoteCollisionHeight;
        blocker.radius = _remoteCollisionRadius;
        blocker.center = _remoteCollisionCenter;
        blocker.direction = 1;
        blocker.enabled = false;
        blockerTransform.gameObject.layer = gameObject.layer;
        return blocker;
    }

    private void SetRemoteCollisionBlocker(bool enabled)
    {
        if (_remoteCollisionBlocker == null)
            _remoteCollisionBlocker = EnsureRemoteCollisionBlocker();

        _remoteCollisionBlocker.enabled = enabled;
    }

    private void SetPlayerVirtualCameras(bool isLocal)
    {
        if (_playerVirtualCameras == null)
            return;

        for (int i = 0; i < _playerVirtualCameras.Length; i++)
        {
            CinemachineCamera virtualCamera = _playerVirtualCameras[i];
            if (virtualCamera == null)
                continue;

            virtualCamera.enabled = isLocal;
            virtualCamera.Priority = isLocal
                ? PlayerSceneCamera.ActivePriority
                : PlayerSceneCamera.InactivePriority;
        }
    }

    private void CaptureState()
    {
        Vector3 currentPosition = transform.position;
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;
        NetworkVelocity = CalculateNetworkVelocity(currentPosition);
        _lastCapturedPosition = currentPosition;
        _hasCapturedState = true;

        if (_animator == null)
            return;

        NetworkAnimationSpeed = _animator.GetFloat(SpeedId);
        NetworkMotionSpeed = _animator.GetFloat(MotionSpeedId);
        NetworkGrounded = _animator.GetBool(GroundedId);
        NetworkJumping = _animator.GetBool(JumpId);
        NetworkFreeFalling = _animator.GetBool(FreeFallId);
    }

    private Vector3 CalculateNetworkVelocity(Vector3 currentPosition)
    {
        if (!_hasCapturedState || Runner == null)
            return Vector3.zero;

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= 0f)
            return Vector3.zero;

        return (currentPosition - _lastCapturedPosition) / deltaTime;
    }

    private void ApplyRemoteTransform()
    {
        Vector3 predictedPosition = NetworkPosition + NetworkVelocity * _remotePredictionTime;

        if (!_hasRemoteVisualState
            || Vector3.Distance(transform.position, predictedPosition) > _remoteSnapDistance)
        {
            transform.SetPositionAndRotation(predictedPosition, NetworkRotation);
            _hasRemoteVisualState = true;
            return;
        }

        float positionBlend = 1f - Mathf.Exp(-_remotePositionSharpness * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-_remoteRotationSharpness * Time.deltaTime);

        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, predictedPosition, positionBlend),
            Quaternion.Slerp(transform.rotation, NetworkRotation, rotationBlend));
    }

    private void ApplyRemoteAnimation()
    {
        if (_animator == null)
            return;

        _animator.SetFloat(SpeedId, NetworkAnimationSpeed);
        _animator.SetFloat(MotionSpeedId, NetworkMotionSpeed);
        _animator.SetBool(GroundedId, NetworkGrounded);
        _animator.SetBool(JumpId, NetworkJumping);
        _animator.SetBool(FreeFallId, NetworkFreeFalling);
    }

    private UnityBehaviour FindBehaviour(string typeName)
    {
        UnityBehaviour[] behaviours = GetComponentsInChildren<UnityBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            UnityBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    private static void SetObjectMember(UnityBehaviour target, string memberName, object value)
    {
        if (target == null || string.IsNullOrEmpty(memberName))
            return;

        Type targetType = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        PropertyInfo property = FindAssignableProperty(targetType, memberName, value, flags);
        if (property != null && property.CanWrite && IsAssignable(property.PropertyType, value))
        {
            property.SetValue(target, value);
            return;
        }

        FieldInfo field = targetType.GetField(memberName, flags);
        if (field != null && IsAssignable(field.FieldType, value))
            field.SetValue(target, value);
    }

    private static PropertyInfo FindAssignableProperty(
        Type targetType,
        string memberName,
        object value,
        BindingFlags flags)
    {
        PropertyInfo[] properties = targetType.GetProperties(flags);
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            if (property.Name == memberName
                && property.CanWrite
                && property.GetIndexParameters().Length == 0
                && IsAssignable(property.PropertyType, value))
            {
                return property;
            }
        }

        return null;
    }

    private static bool IsAssignable(Type targetType, object value)
    {
        return value == null || targetType.IsInstanceOfType(value);
    }

    private static void SetEnabled(UnityBehaviour behaviour, bool enabled)
    {
        if (behaviour != null)
            behaviour.enabled = enabled;
    }
}

/// <summary>
/// Shared Mode uses state authority as the practical owner for player objects.
/// Input authority still matters for other Fusion modes, so local checks accept both.
/// </summary>
public static class NetworkPlayerOwnership
{
    public static bool OwnsPlayer(NetworkBehaviour behaviour)
    {
        return behaviour != null && OwnsPlayer(behaviour.Object);
    }

    public static bool OwnsPlayer(NetworkObject networkObject)
    {
        return networkObject != null
            && networkObject.IsValid
            && (networkObject.HasInputAuthority
            || networkObject.HasStateAuthority
            || IsRunnerLocalPlayer(networkObject));
    }

    public static bool IsLocal(NetworkBehaviour behaviour) => ShouldDriveLocalView(behaviour);

    public static bool IsLocal(NetworkObject networkObject)
    {
        return ShouldDriveLocalView(networkObject);
    }

    public static bool IsRunnerLocalPlayer(NetworkObject networkObject)
    {
        return networkObject != null
            && networkObject.IsValid
            && networkObject.Runner != null
            && networkObject.Runner.IsRunning
            && (networkObject.Runner.GetPlayerObject(networkObject.Runner.LocalPlayer) == networkObject
            || networkObject.StateAuthority == networkObject.Runner.LocalPlayer
            || networkObject.InputAuthority == networkObject.Runner.LocalPlayer);
    }

    public static bool IsRemote(NetworkBehaviour behaviour)
    {
        return behaviour != null && !OwnsPlayer(behaviour.Object);
    }

    public static bool CanProvideFocusedInput(NetworkBehaviour behaviour)
    {
        return ShouldDriveLocalView(behaviour)
            && behaviour.Runner != null
            && behaviour.Runner.IsRunning
            && behaviour.Runner.ProvideInput;
    }

    public static bool ShouldDriveLocalView(NetworkBehaviour behaviour)
    {
        return behaviour != null && ShouldDriveLocalView(behaviour.Object);
    }

    public static bool ShouldDriveLocalView(NetworkObject networkObject)
    {
        return IsRunnerLocalPlayer(networkObject);
    }
}
