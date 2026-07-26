using Fusion;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepSystem : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SurfaceDefinition _surfaceDefinition;
    [SerializeField] private FootstepCollection _footstepCollection;
    [SerializeField] private SurfaceType _defaultSurface = SurfaceType.Grass;
    
    [Tooltip("Layer mask for ground detection. If GroundDetector is present, this can be overridden.")]
    [SerializeField] private LayerMask _groundLayer;
    
    [Header("Trigger Settings")]
    [Tooltip("If true, footsteps will play automatically based on velocity. If false, call PlayFootstep() via Animation Events.")]
    [SerializeField] private bool _playBasedOnVelocity = false;
    [Tooltip("Seconds between footstep sounds while walking.")]
    [SerializeField] private float _walkStepInterval = 0.5f;
    [Tooltip("Seconds between footstep sounds while running.")]
    [SerializeField] private float _runStepInterval = 0.3f;
    [Tooltip("Horizontal speed (m/s) at which the run interval kicks in.")]
    [SerializeField] private float _runSpeedThreshold = 4f;
    [Tooltip("Minimum horizontal speed required to play footsteps.")]
    [SerializeField] private float _minMoveSpeed = 0.1f;
    [Tooltip("Seconds the player can be slightly off the ground on uneven surfaces before footsteps stop.")]
    [SerializeField] private float _groundedGracePeriod = 0.15f;
    [Header("References")]
    [SerializeField] private GroundDetector _groundDetector;
    private AudioSource _audioSource;
    private NetworkCharacterController _characterController;
    private float _stepTimer;
    private float _lastGroundedTime;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _characterController = PlayerRoot.Resolve<NetworkCharacterController>(this);

        // Try to get ground layer from detector if not set
        if (_groundLayer == 0 && _groundDetector != null)
        {
            _groundLayer = _groundDetector.GroundLayer;
        }
    }

    public override void Spawned()
    {
        _stepTimer = 0f;
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (_playBasedOnVelocity)
            UpdateTimedFootsteps();
    }

    private void UpdateTimedFootsteps()
    {
        if (IsGrounded())
            _lastGroundedTime = Time.time;

        bool effectivelyGrounded = (Time.time - _lastGroundedTime) <= _groundedGracePeriod;

        if (!effectivelyGrounded)
        {
            _stepTimer = 0f;
            return;
        }

        float speed = GetHorizontalSpeed();
        if (speed < _minMoveSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        float interval = Mathf.Lerp(_walkStepInterval, _runStepInterval, speed / _runSpeedThreshold);
        _stepTimer += Time.deltaTime;

        if (_stepTimer >= interval)
        {
            _stepTimer = 0f;
            PlayFootstep();
        }
    }

    private float GetHorizontalSpeed()
    {
        if (_characterController != null)
        {
            Vector3 vel = _characterController.Velocity;
            vel.y = 0f;
            return vel.magnitude;
        }
        // Fallback: not ideal but acceptable when no character controller is present
        return 0f;
    }

    /// <summary>
    /// Plays a footstep sound based on the surface below the transform.
    /// Can be called via Animation Event.
    /// </summary>
    public void PlayFootstep()
    {
        // Don't play footsteps if we are not grounded
        // Note: For Animation Events, sometimes we want to force it (like landing), but standard walk cycle assumes grounded.
        // If checking IsGrounded here causes missed steps, we can remove it for Anim Events, 
        // but it's safer to keep it to avoid air-walking sounds if animation continues during fall.
        if (!IsGrounded()) 
        {
            // If called manually (Anim Event) while in air, we might want to skip.
            // But if distance-based logic calls it, it already checked IsGrounded.
            return;
        }

        if (_audioSource == null) return;

        SurfaceType surfaceType = DetectSurface();
        AudioClip clip = _footstepCollection.GetRandomClip(surfaceType);

        if (clip != null)
        {
            // Randomize pitch slightly for variety
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            
            // PlayOneShot allows overlapping footsteps if running fast
            _audioSource.PlayOneShot(clip);
        }
    }

    private bool IsGrounded()
    {
        if (_groundDetector != null) return _groundDetector.IsGrounded;
        
        // Fallback: simple raycast check
        return PhysicsSceneQueries.CheckSphere(this, GetCheckPosition(), 0.2f, _groundLayer, QueryTriggerInteraction.Ignore);
    }

    private SurfaceType DetectSurface()
    {
        Vector3 origin = GetCheckPosition() + Vector3.up * 0.2f;
        float distance = 0.5f;

        // Raycast down to find the surface
        if (PhysicsSceneQueries.Raycast(this, origin, Vector3.down, out RaycastHit hit, distance, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Check for Tag via SurfaceDefinition
            if (_surfaceDefinition != null)
            {
                return _surfaceDefinition.GetSurfaceType(hit.collider.tag);
            }
        }

        return _defaultSurface;
    }

    private Vector3 GetCheckPosition()
    {
         if (_groundDetector != null && _groundDetector.GroundCheck != null)
         {
             return _groundDetector.GroundCheck.position;
         }
         return transform.position;
    }
}
