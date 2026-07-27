# ServiceLocator — Usage Guide

A lightweight dependency container with three scopes: **Global**, **Scene**, and **Hierarchy**.

---

## 1. Setup in the Scene

### Global (survives scene loads)
Add a GameObject with **ServiceLocator Global** component. Tick *Don't Destroy On Load* if needed.

### Scene-level
Add a GameObject with **ServiceLocator Scene** component per scene.

> Or via the Unity menu: `GameObject → ServiceLocator → Add Global / Add Scene`

Fusion multi-peer merges level content into each runner's `SimulationUnityScene`. After merge, `EnglishQuestNetworkSceneManager` calls `ServiceLocator.RefreshForScene` so child scene locators re-register under the simulation scene.

---

## 2. Registering Services

Register during `Awake` or inside a `Bootstrapper` subclass.

```csharp
// Register on the global locator
ServiceLocator.Global.Register<IAudioService>(new AudioService());

// Scene managers (quest, dialogue) — unchanged
ServiceLocator.For(this).Register<IQuestService>(this);

// Local player components — register on the player's scene locator
ServiceLocator.ForSceneOf(this).Register<IPlayerLockSystem>(this);
```

Local-player interfaces must **never** register on `ServiceLocator.Global`:

- `IPlayerLockSystem`
- `ILocalPlayerHealth`
- `ILocalAbilityController`
- `IAbilityActionBarAdapter`
- `ILocalPlayerReadiness`
- `IPartyService`, `IPartyInviteService`, `ISessionPlayerRegistry`

---

## 3. Retrieving Services

### Get (throws if not found)
```csharp
var audio = ServiceLocator.Global.Get<IAudioService>();
```

### TryGet (safe, no exception)
```csharp
if (ServiceLocator.ForSceneOf(this).TryGet<IUIController>(out var ui))
{
    ui.ShowHUD();
}
```

---

## 4. Deregistering Services

```csharp
ServiceLocator.DeregisterGlobal<IAudioService>();

// Local-player providers — deregister from the same scene locator used for register
ServiceLocator.ForSceneOf(this).DeregisterIfRegistered<IPlayerLockSystem>();
```

Do **not** use `DeregisterFor(this)` for local-player providers: it may target a parent hierarchy locator instead of the scene locator.

---

## 5. Custom Bootstrapper (recommended pattern)

Create a bootstrapper to register your scene's services in one place:

```csharp
public class GameplayBootstrapper : Bootstrapper
{
    protected override void Bootstrap()
    {
        Container
            .Register<IAudioService>(audioService)
            .Register<IUIController>(uiController);
    }
}
```

---

## 6. Scope Resolution Order

When calling `ServiceLocator.For(this)`, it searches:
1. Parent hierarchy (closest `ServiceLocator` component up the tree)
2. Scene-level locator
3. Global locator (fallback)

Scene services override globals for the same type in that scene.

---

## 7. Scene-first local-player services (Multi-Peer)

Each Fusion simulation scene owns its local-player services through that scene's `ServiceLocator`.

### Register
```csharp
ServiceLocator.ForSceneOf(this).Register<IPlayerLockSystem>(this);
```

### Resolve
```csharp
// UI/minigame WITH PlayerInteraction (preferred)
ServiceLocator.For(interactor).TryGet<IPlayerLockSystem>(out var locks);

// HUD/UI in the SAME simulation scene as the player
ServiceLocator.For(this).TryGet<ILocalPlayerHealth>(out var health);

// After LocalPlayerReadiness.Ready
ServiceLocator.ForSceneOf(args.Player.GetComponent<PlayerInteraction>())
    .TryGet<ILocalAbilityController>(out var abilities);
```

### Wait for player (not service lookup)
```csharp
if (ServiceLocator.For(this).TryGet(out ILocalPlayerReadiness readiness))
{
    readiness.Ready += OnLocalPlayerReady;
    await readiness.WaitReadyAsync();
}
```

`ILocalPlayerReadiness` is registered automatically by `ServiceLocatorScene` on each gameplay scene. `LocalPlayerReadiness.TryGet(scene, out readiness)` resolves it when you only have a `Scene` or `NetworkRunner`.

---

## 8. DontDestroyOnLoad / cross-scene UI

DDOL objects live in the `DontDestroyOnLoad` pseudo-scene. Their `For(this)` resolves to Global or the wrong scene.

Rules:

- **Never** `ServiceLocator.For(ddolComponent).TryGet<IPlayerLockSystem>`
- **Always** `ServiceLocator.For(interactor).TryGet<...>` when opening shared UI (e.g. `WorldMapUI`)
- `For(this)` is allowed only when the component is guaranteed to live in the player's simulation scene

---

## Quick Reference

| Method | Scope | Throws? |
|---|---|---|
| `ServiceLocator.Global` | Global | — |
| `ServiceLocator.ForSceneOf(mb)` | Scene | — |
| `ServiceLocator.For(mb)` | Hierarchy → Scene → Global | — |
| `.Register<T>(service)` | Chosen locator | No (logs error on duplicate) |
| `.Get<T>()` | Chosen locator + fallback chain | **Yes** |
| `.TryGet<T>(out service)` | Chosen locator + fallback chain | No |
| `.DeregisterIfRegistered<T>()` | Chosen locator | No |

---

## 9. Migration note

Managers still using `StaticInstance<T>` alongside ServiceLocator should migrate fully over time. New code should prefer `ServiceLocator` + `LocalPlayerReadiness` and avoid `FindFirstObjectByType` for local-player resolution.
