using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Applies a short knockback to NavMesh-driven entities when they take damage.
/// Runs only on state authority when a <see cref="NetworkObject"/> is present.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentKnockbackReaction : MonoBehaviour
{
    [SerializeField] private HealthComponent _health;
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private NetworkObject _networkObject;
    [SerializeField] private HitReactionConfigSO _config;

    private Coroutine _knockbackRoutine;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        if (_navAgent == null)
            _navAgent = GetComponent<NavMeshAgent>();

        if (_networkObject == null)
            _networkObject = GetComponent<NetworkObject>();
    }

    private bool IsKnockbackEnabled()
        => _config == null || _config.knockbackEnabled;

    private float GetKnockbackDistance()
        => _config != null ? _config.knockbackDistance : 0.6f;

    private float GetKnockbackDuration()
        => _config != null ? _config.knockbackDuration : 0.15f;

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTaken;

        if (_knockbackRoutine != null)
        {
            StopCoroutine(_knockbackRoutine);
            _knockbackRoutine = null;
        }
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (!IsKnockbackEnabled())
            return;

        if (_health != null && !_health.IsAlive)
            return;

        if (!info.HasHitDirection)
            return;

        if (!CanApplyKnockback())
            return;

        if (_knockbackRoutine != null)
            StopCoroutine(_knockbackRoutine);

        _knockbackRoutine = StartCoroutine(KnockbackRoutine(info.HitDirection));
    }

    private bool CanApplyKnockback()
    {
        if (_networkObject == null || !_networkObject.IsValid)
            return true;

        return _networkObject.HasStateAuthority;
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        if (_navAgent == null || !_navAgent.isActiveAndEnabled)
        {
            _knockbackRoutine = null;
            yield break;
        }

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.001f)
        {
            _knockbackRoutine = null;
            yield break;
        }

        flatDirection.Normalize();

        _navAgent.isStopped = true;
        _navAgent.velocity = Vector3.zero;
        bool restoreUpdatePosition = _navAgent.updatePosition;
        _navAgent.updatePosition = false;

        Vector3 start = transform.position;
        Vector3 target = start + flatDirection * GetKnockbackDistance();
        float duration = GetKnockbackDuration();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            Vector3 next = Vector3.Lerp(start, target, eased);

            if (NavMesh.SamplePosition(next, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                next = hit.position;

            transform.position = next;
            yield return null;
        }

        if (_navAgent.isActiveAndEnabled)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit finalHit, 0.5f, NavMesh.AllAreas))
                _navAgent.Warp(finalHit.position);
            else
                _navAgent.Warp(transform.position);

            _navAgent.updatePosition = restoreUpdatePosition;
            _navAgent.isStopped = false;
        }

        _knockbackRoutine = null;
    }
}
