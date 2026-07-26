using Fusion;
using UnityEngine;

/// <summary>
/// Replicates <see cref="HealthComponent"/> state over Photon Fusion Shared Mode.
/// This is the ONLY file in the health system that imports Fusion.
///
/// Authority flow:
///   HealthComponent changes → HandleHealthChanged → write [Networked] NetworkedHP → proxies receive it.
///
/// Proxy flow:
///   [Networked] NetworkedHP changes → OnNetworkedHPChanged → HealthComponent.SetHP → OnHealthChanged event → UI/bar update.
///
/// One-shot events (hit, death):
///   Authority → RPC to proxies only. OnChangedRender alone could miss rapid successive hits.
///   Authority fires its own HealthComponent events locally; proxies receive RPCs.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class NetworkedHealthSync : NetworkBehaviour
{
    private HealthComponent _health;
    private DamageAttributionComponent _attribution;

    [Networked, OnChangedRender(nameof(OnNetworkedHPChanged))]
    public float NetworkedHP { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Spawned()
    {
        _health = GetComponent<HealthComponent>();
        _attribution = GetComponent<DamageAttributionComponent>();

        if (HasStateAuthority)
        {
            // Seed the networked value from the current local HP (set in HealthComponent.Awake).
            NetworkedHP = _health.CurrentHP;
            _health.OnHealthChanged += HandleHealthChanged;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_health != null && HasStateAuthority)
            _health.OnHealthChanged -= HandleHealthChanged;
    }

    // ── Authority side ────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors local HP changes to the networked state so all proxies receive the update.
    /// Subscribed only on the state authority.
    /// </summary>
    private void HandleHealthChanged(float current, float max)
    {
        NetworkedHP = current;
    }

    /// <summary>
    /// Entry point for external sources (e.g. monsters) to damage this networked entity.
    /// Routes to the state authority in Shared Mode via RPC when called from a non-authority client.
    /// </summary>
    public void RequestDamage(float amount, DamageInfo info, GameObject instigator = null)
    {
        NetworkId instigatorId = ResolveInstigatorId(instigator);

        if (HasStateAuthority)
            ApplyDamage(amount, info, instigatorId);
        else
            RPC_RequestDamage(
                amount,
                (int)info.Type,
                info.HasHitDirection,
                info.HitDirection.x,
                info.HitDirection.y,
                info.HitDirection.z,
                instigatorId);
    }

    /// <summary>
    /// Primary entry point for applying damage on networked entities.
    /// Guards against non-authority callers. Sends RPCs for one-shot events (hit / death).
    ///
    /// For local-only entities (no NetworkObject), call HealthComponent.TakeDamage directly instead.
    /// </summary>
    public void ApplyDamage(float amount, DamageInfo info, NetworkId instigatorId = default)
    {
        if (!HasStateAuthority) return;

        RecordInstigator(instigatorId);
        _health.TakeDamage(amount, info);

        // Authority already received its own OnDamageTaken event; notify proxies via RPC.
        RPC_NotifyDamageTaken(
            amount,
            (int)info.Type,
            info.HasHitDirection,
            info.HitDirection.x,
            info.HitDirection.y,
            info.HitDirection.z);

        if (!_health.IsAlive)
            NotifyDeathAuthority();
    }

    /// <summary>
    /// Heals HP from the state authority.
    /// </summary>
    public void ApplyHeal(float amount)
    {
        if (!HasStateAuthority) return;
        _health.Heal(amount);
    }

    // ── Proxy side ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires on ALL clients when [Networked] NetworkedHP changes.
    /// Proxies push the value to their local HealthComponent so UI/feedbacks stay reactive.
    /// Authority is skipped — it drives HealthComponent directly and is the source of truth.
    /// </summary>
    private void OnNetworkedHPChanged()
    {
        if (HasStateAuthority) return;

        if (_health == null)
            _health = GetComponent<HealthComponent>();

        _health.SetHP(NetworkedHP);
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(
        float amount,
        int damageTypeInt,
        bool hasHitDirection,
        float hitDirX,
        float hitDirY,
        float hitDirZ,
        NetworkId instigatorId)
    {
        ApplyDamage(
            amount,
            BuildDamageInfo(amount, damageTypeInt, hasHitDirection, hitDirX, hitDirY, hitDirZ),
            instigatorId);
    }

    /// <summary>
    /// Replicates a hit event to proxy clients only.
    /// DamageType is passed as int because Fusion RPCs require INetworkStruct for custom types;
    /// we reconstruct DamageInfo on the receiving end to keep DamageInfo.cs Fusion-free.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_NotifyDamageTaken(
        float amount,
        int damageTypeInt,
        bool hasHitDirection,
        float hitDirX,
        float hitDirY,
        float hitDirZ)
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        var info = BuildDamageInfo(amount, damageTypeInt, hasHitDirection, hitDirX, hitDirY, hitDirZ);
        _health.NetworkInvokeDamageTaken(amount, info);
    }

    private static DamageInfo BuildDamageInfo(
        float amount,
        int damageTypeInt,
        bool hasHitDirection,
        float hitDirX,
        float hitDirY,
        float hitDirZ)
    {
        var info = new DamageInfo(amount, (DamageType)damageTypeInt);
        if (!hasHitDirection)
            return info;

        info.HasHitDirection = true;
        info.HitDirection = new Vector3(hitDirX, hitDirY, hitDirZ);
        return info;
    }

    /// <summary>Replicates the death event to proxy clients only.</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_NotifyDeath(NetworkId killerId)
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        _health.NetworkInvokeDied(BuildDeathContext(killerId));
    }

    // ── Death / attribution ───────────────────────────────────────────────────

    private void NotifyDeathAuthority()
    {
        if (_attribution == null)
            _attribution = GetComponent<DamageAttributionComponent>();

        DeathContext context = _attribution != null
            ? _attribution.BuildDeathContext()
            : DeathContext.None;

        NetworkId killerId = ResolveInstigatorId(context.Killer);
        _health.NotifyDied(context);
        RPC_NotifyDeath(killerId);
    }

    private void RecordInstigator(NetworkId instigatorId)
    {
        if (_attribution == null)
            _attribution = GetComponent<DamageAttributionComponent>();

        if (_attribution == null)
            return;

        GameObject instigator = ResolveInstigator(instigatorId);
        _attribution.RecordInstigator(instigator);
    }

    private DeathContext BuildDeathContext(NetworkId killerId)
    {
        GameObject killer = ResolveInstigator(killerId);
        return killer != null ? new DeathContext(killer) : DeathContext.None;
    }

    private GameObject ResolveInstigator(NetworkId instigatorId)
    {
        if (Runner == null || !instigatorId.IsValid)
            return null;

        return Runner.TryFindObject(instigatorId, out NetworkObject networkObject) && networkObject != null
            ? networkObject.gameObject
            : null;
    }

    private NetworkId ResolveInstigatorId(GameObject instigator)
    {
        if (instigator == null)
            return default;

        var networkObject = instigator.GetComponentInParent<NetworkObject>();
        return networkObject != null && networkObject.IsValid
            ? networkObject.Id
            : default;
    }
}
