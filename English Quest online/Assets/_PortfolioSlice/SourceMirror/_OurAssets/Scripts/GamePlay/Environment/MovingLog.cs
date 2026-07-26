using Fusion;
using UnityEngine;

public class MovingLog : NetworkBehaviour
{
    private enum LogState
    {
        Extending,
        WaitingExtended,
        Retracting,
        WaitingRetracted
    }

    [Header("Movement Settings")]
    [Tooltip("The local direction the log should move in.")]
    [SerializeField] private Vector3 _moveDirection = Vector3.forward;
    [Tooltip("How far the log should move from its starting position.")]
    [SerializeField] private float _moveDistance = 5f;
    [Tooltip("The speed at which the log moves.")]
    [SerializeField] private float _moveSpeed = 2f;

    [Header("Timing Settings")]
    [Tooltip("How long to wait when fully extended (seconds).")]
    [SerializeField] private float _extendedWaitTime = 2f;
    [Tooltip("How long to wait when fully retracted (seconds).")]
    [SerializeField] private float _retractedWaitTime = 2f;

    [Header("References")]
    [Tooltip("The Kinematic Rigidbody attached to this log.")]
    [SerializeField] private Rigidbody _rigidbody;

    [Networked] private LogState CurrentState { get; set; }
    [Networked] private TickTimer WaitTimer { get; set; }
    [Networked] private Vector3 StartPosition { get; set; }

    private Vector3 TargetExtendedPosition => StartPosition + (transform.TransformDirection(_moveDirection.normalized) * _moveDistance);

    public override void Spawned()
    {
        base.Spawned();
        
        StartPosition = transform.position;
        CurrentState = LogState.WaitingRetracted;
        WaitTimer = TickTimer.CreateFromSeconds(Runner, _retractedWaitTime);

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_rigidbody == null) return;

        switch (CurrentState)
        {
            case LogState.Extending:
                MoveTowards(TargetExtendedPosition, LogState.WaitingExtended, _extendedWaitTime);
                break;

            case LogState.WaitingExtended:
                if (WaitTimer.Expired(Runner))
                {
                    CurrentState = LogState.Retracting;
                }
                break;

            case LogState.Retracting:
                MoveTowards(StartPosition, LogState.WaitingRetracted, _retractedWaitTime);
                break;

            case LogState.WaitingRetracted:
                if (WaitTimer.Expired(Runner))
                {
                    CurrentState = LogState.Extending;
                }
                break;
        }
    }

    private void MoveTowards(Vector3 targetPosition, LogState nextState, float waitTime)
    {
        Vector3 newPos = Vector3.MoveTowards(_rigidbody.position, targetPosition, _moveSpeed * Runner.DeltaTime);
        _rigidbody.MovePosition(newPos);

        if (Vector3.Distance(_rigidbody.position, targetPosition) < 0.001f)
        {
            CurrentState = nextState;
            WaitTimer = TickTimer.CreateFromSeconds(Runner, waitTime);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 startPos = Application.isPlaying ? StartPosition : transform.position;
        Vector3 endPos = startPos + (transform.TransformDirection(_moveDirection.normalized) * _moveDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPos, 0.3f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPos, 0.3f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPos, endPos);
    }
#endif
}
