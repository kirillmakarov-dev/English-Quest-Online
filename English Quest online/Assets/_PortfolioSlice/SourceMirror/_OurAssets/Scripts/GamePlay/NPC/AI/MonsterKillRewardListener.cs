using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Runs configured <see cref="GameAction"/> rewards when this monster dies,
/// but only on the client whose local player dealt the killing blow.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public class MonsterKillRewardListener : MonoBehaviour
{
    [SerializeField] private HealthComponent _health;
    [Tooltip("Actions executed on the killer's client only (stat recording, future loot drops, etc.).")]
    [SerializeField] private List<GameAction> _rewardsOnKill = new();

    private NetworkRunner _runner;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        CollectSiblingRewardActionsIfNeeded();

        var networkObject = GetComponent<NetworkObject>();
        _runner = networkObject != null && networkObject.IsValid
            ? networkObject.Runner
            : null;
    }

    private void CollectSiblingRewardActionsIfNeeded()
    {
        if (_rewardsOnKill != null && _rewardsOnKill.Count > 0)
            return;

        _rewardsOnKill = new List<GameAction>();
        foreach (GameAction action in GetComponents<GameAction>())
            _rewardsOnKill.Add(action);
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDied -= HandleDied;
    }

    private void HandleDied(DeathContext context)
    {
        if (!CombatKillCredit.IsLocalPlayerKill(context, _runner))
            return;

        foreach (GameAction action in _rewardsOnKill)
        {
            if (action != null)
                StartCoroutine(action.Execute());
        }
    }
}
