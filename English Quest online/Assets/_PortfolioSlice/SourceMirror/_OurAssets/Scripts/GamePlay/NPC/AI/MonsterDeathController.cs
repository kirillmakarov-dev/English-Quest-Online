using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// Removes a monster after its death animation finishes.
/// Subscribes to <see cref="HealthComponent.OnDied"/> on every client; only the
/// state authority despawns networked monsters.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(MonsterAIComponent))]
public class MonsterDeathController : MonoBehaviour
{
    [SerializeField] private HealthComponent _health;
    [SerializeField] private MonsterAIComponent _ai;

    private Collider _collider;
    private DamageReceiver _damageReceiver;
    private NetworkObject _networkObject;
    private bool _isRemoving;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        if (_ai == null)
            _ai = GetComponent<MonsterAIComponent>();

        _collider = GetComponent<Collider>();
        _damageReceiver = GetComponent<DamageReceiver>();
        _networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        _health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDied -= HandleDied;
    }

    private void HandleDied(DeathContext _)
    {
        if (_isRemoving)
            return;

        _isRemoving = true;

        if (_collider != null)
            _collider.enabled = false;

        if (_damageReceiver != null)
            _damageReceiver.enabled = false;

        float delay = _ai != null && _ai.Config != null ? _ai.Config.deathAnimDuration : 1.67f;
        RemoveAfterDelayAsync(delay, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RemoveAfterDelayAsync(float delay, CancellationToken destroyToken)
    {
        try
        {
            if (delay > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: destroyToken);

            if (_networkObject != null && _networkObject.IsValid)
            {
                if (_networkObject.HasStateAuthority)
                    _networkObject.Runner.Despawn(_networkObject);

                return;
            }

            Destroy(gameObject);
        }
        catch (System.OperationCanceledException)
        {
            // Scene unload or despawn cancelled the pending removal.
        }
    }
}
