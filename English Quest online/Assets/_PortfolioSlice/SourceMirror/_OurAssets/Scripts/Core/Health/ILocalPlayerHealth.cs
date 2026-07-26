/// <summary>
/// Local-player health contract. Resolves via
/// <c>ServiceLocator.For(component).TryGet&lt;ILocalPlayerHealth&gt;</c>
/// after the player spawns with input authority.
/// </summary>
public interface ILocalPlayerHealth
{
    HealthComponent Health { get; }
}
