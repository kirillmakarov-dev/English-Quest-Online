using UnityEngine;

/// <summary>
/// Tracks the last damaging instigator on this entity (last hit wins).
/// Updated on the state authority before each authoritative hit.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public class DamageAttributionComponent : MonoBehaviour
{
  private GameObject _lastInstigator;

  public GameObject LastInstigator => _lastInstigator;

  public void RecordInstigator(GameObject source)
  {
    if (source == null)
      return;

    _lastInstigator = source;
  }

  public void Clear()
  {
    _lastInstigator = null;
  }

  public DeathContext BuildDeathContext()
  {
    return _lastInstigator != null
      ? new DeathContext(_lastInstigator)
      : DeathContext.None;
  }
}
