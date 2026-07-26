using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shared NavMesh and facing helpers for combat AI actions.
/// </summary>
public class NPCNavigationHelper
{
    private const float SetDestinationIntervalSeconds = 0.35f;
    private const float SetDestinationMoveThresholdSqr = 0.25f; // 0.5m

    private float _nextSetDestinationTime;
    private Vector3 _lastSetDestination;

    public float DistanceToTarget(Transform owner, Transform target)
    {
        if (owner == null || target == null)
            return float.MaxValue;

        Vector3 delta = target.position - owner.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    public float HorizontalSqrDistanceTo(Transform owner, Vector3 worldPosition)
    {
        if (owner == null)
            return float.MaxValue;

        Vector3 delta = worldPosition - owner.position;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    public bool IsWithinAttackRange(float horizontalDistance, float effectiveAttackRange)
    {
        return horizontalDistance <= effectiveAttackRange;
    }

    public float GetEffectiveAttackRange(ICombatAIConfig config, NavMeshAgent navAgent)
    {
        if (config == null)
            return 0f;

        float agentRadius = navAgent != null && navAgent.isActiveAndEnabled ? navAgent.radius : 0f;
        return config.attackRange + agentRadius;
    }

    public void ConfigureStoppingDistance(NavMeshAgent navAgent, ICombatAIConfig config)
    {
        if (navAgent == null || config == null)
            return;

        navAgent.stoppingDistance = Mathf.Clamp(config.attackRange * 0.25f, 0.05f, config.attackRange * 0.5f);
    }

    public void FaceTarget(Transform owner, Transform target, float deltaTime)
    {
        if (owner == null || target == null)
            return;

        Vector3 toTarget = target.position - owner.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f)
            return;

        owner.rotation = Quaternion.Slerp(
            owner.rotation,
            Quaternion.LookRotation(toTarget.normalized),
            10f * deltaTime);
    }

    public void Chase(NavMeshAgent navAgent, Transform target, float speed)
    {
        if (navAgent == null || target == null || !navAgent.isActiveAndEnabled || !navAgent.isOnNavMesh)
            return;

        navAgent.speed = speed;
        navAgent.isStopped = false;

        Vector3 destination = target.position;
        float now = Time.time;
        if (now < _nextSetDestinationTime)
        {
            Vector3 moved = destination - _lastSetDestination;
            moved.y = 0f;
            if (moved.sqrMagnitude < SetDestinationMoveThresholdSqr)
                return;
        }

        navAgent.SetDestination(destination);
        _lastSetDestination = destination;
        _nextSetDestinationTime = now + SetDestinationIntervalSeconds;
    }

    public void Stop(NavMeshAgent navAgent)
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
            navAgent.isStopped = true;
    }

    public void Resume(NavMeshAgent navAgent)
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
            navAgent.isStopped = false;
    }

    public void ResetPath(NavMeshAgent navAgent)
    {
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            navAgent.ResetPath();
    }
}
