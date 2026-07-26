using UnityEngine;

/// <summary>
/// Killer context passed with <see cref="HealthComponent.OnDied"/>.
/// Fusion-free — uses <see cref="GameObject"/> instead of <see cref="Fusion.PlayerRef"/>.
/// </summary>
public readonly struct DeathContext
{
  public readonly GameObject Killer;

  public DeathContext(GameObject killer)
  {
    Killer = killer;
  }

  public bool HasKiller => Killer != null;

  public static DeathContext None => default;
}
