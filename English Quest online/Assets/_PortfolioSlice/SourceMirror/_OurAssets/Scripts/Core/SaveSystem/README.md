# Save System - English Kingdom

## Overview

A versioned, async, hybrid save system built on top of Unity Gaming Services (UGS).
Data is written to both local disk and Unity Cloud Save so the project can survive
offline play and still sync when the backend is available.

This portfolio slice keeps the save layer focused on the systems we want to show:

- `CoreSaveData`
- `SettingsSaveData`
- `ProgressSaveData`
- `CurrencySaveData`
- `AbilitySaveData`
- `StatsSaveData`

Inventory and action bar storage were intentionally removed from the live slice.

## Architecture

```
SaveSystemBootstrapper (MonoBehaviour)
  -> HybridSaveService (ISaveService)
      -> LocalSaveService   (JSON on disk)
      -> CloudPlayerSaveService (Unity Cloud Save)

SaveManager (static facade)
SaveMigrationService (schema migration pipeline)
```

All services are registered on `ServiceLocator.Global` at boot. Game code uses
`SaveManager` rather than resolving backend services directly.

## Usage

```csharp
await SaveManager.SaveCoreAsync(coreData);
await SaveManager.SaveSettingsAsync(settingsData);
await SaveManager.SaveProgressAsync(progressData);
await SaveManager.SaveCurrencyAsync(currencyData);
await SaveManager.SaveAbilitiesAsync(abilityData);
await SaveManager.SaveStatsAsync(statsData);

CoreSaveData core = await SaveManager.LoadCoreAsync();
SettingsSaveData settings = await SaveManager.LoadSettingsAsync();
ProgressSaveData progress = await SaveManager.LoadProgressAsync();
CurrencySaveData currency = await SaveManager.LoadCurrencyAsync();
AbilitySaveData abilities = await SaveManager.LoadAbilitiesAsync();
StatsSaveData stats = await SaveManager.LoadStatsAsync();
```

## Hybrid Write / Read Strategy

**Save:** local first, then cloud.

**Load:** cloud first, fall back to local on failure.

**Delete:** both backends are cleared through the same facade.

## Versioning & Migration

Every domain object is stored inside a `VersionedWrapper<T>` envelope.
When `SaveMigrationService` sees an older version, it chains registered migrators
one step at a time until the payload reaches the latest schema.

The slice currently keeps the migration pipeline itself, but not the inventory-
specific migration path that used to live here.

## Bootstrap Setup

Attach `SaveSystemBootstrapper` to a persistent GameObject that loads before any
scene that reads or writes player data. It initializes the UGS Core SDK and
registers the save stack on `ServiceLocator.Global`.

## Save Debug Window

The editor save inspector is still available for the remaining live domains.
It is useful for verifying payloads during the portfolio slice cleanup and for
checking the current versioned JSON stored by the save backend.
