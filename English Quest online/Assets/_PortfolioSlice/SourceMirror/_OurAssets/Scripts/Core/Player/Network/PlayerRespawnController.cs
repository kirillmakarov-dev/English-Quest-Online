using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Teleports and revives the player at the nearest <see cref="PlayerSpawnPoint"/> after death.
/// Runs only on state authority — proxy clients still receive death VFX via <see cref="HealthComponent.OnDied"/>.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerRespawnController : NetworkBehaviour
{
    [SerializeField] private float _respawnDelay = 1.5f;
    [SerializeField] private bool _resetVelocity = true;
    [SerializeField] private float _postRespawnInvincibility;

    private HealthComponent _health;
    private NetworkCharacterController _characterController;
    private PlayerLockSystem _lockSystem;
    private bool _isRespawning;
    private CancellationTokenSource _respawnCts;

    public override void Spawned()
    {
        _health = GetComponent<HealthComponent>();
        _characterController = PlayerRoot.Resolve<NetworkCharacterController>(this);
        _lockSystem = PlayerRoot.Resolve<PlayerLockSystem>(this);

        if (!HasStateAuthority) return;

        _health.OnDied += HandleDied;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        CancelPendingRespawn();

        if (_health != null && HasStateAuthority)
            _health.OnDied -= HandleDied;
    }

    private void HandleDied(DeathContext _)
    {
        if (!HasStateAuthority || _isRespawning) return;

        _isRespawning = true;
        Vector3 deathPosition = transform.position;
        _lockSystem?.Lock(PlayerLockSystem.LockType.Movement, this);
        RespawnAfterDelayAsync(deathPosition, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RespawnAfterDelayAsync(Vector3 deathPosition, CancellationToken destroyToken)
    {
        _respawnCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
        CancellationToken token = _respawnCts.Token;

        try
        {
            if (_respawnDelay > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(_respawnDelay), cancellationToken: token);

            if (!HasStateAuthority || _health == null || _characterController == null)
                return;

            PlayerSpawnPoint.TryGetNearestSpawnPoint(deathPosition, out Vector3 position, out Quaternion rotation);
            _characterController.Teleport(position, rotation);

            if (_resetVelocity)
                _characterController.Velocity = Vector3.zero;

            _health.Revive();

            if (_postRespawnInvincibility > 0f)
                _health.GrantInvincibility(_postRespawnInvincibility);

            if (Object.HasInputAuthority)
                AlignLocalCameraOrbit(rotation);
        }
        catch (System.OperationCanceledException)
        {
            // Scene unload or despawn cancelled the pending respawn.
        }
        finally
        {
            _lockSystem?.Unlock(PlayerLockSystem.LockType.Movement, this);
            _isRespawning = false;
            _respawnCts?.Dispose();
            _respawnCts = null;
        }
    }

    private void CancelPendingRespawn()
    {
        if (_isRespawning)
            _lockSystem?.Unlock(PlayerLockSystem.LockType.Movement, this);

        _respawnCts?.Cancel();
        _respawnCts?.Dispose();
        _respawnCts = null;
        _isRespawning = false;
    }

    private void AlignLocalCameraOrbit(Quaternion facing)
    {
        CinemachineCamera followCamera = PlayerSceneContext.ResolveFollowCamera(gameObject.scene);
        if (followCamera != null)
            PlayerSceneCamera.AlignOrbit(followCamera, facing);
    }
}
