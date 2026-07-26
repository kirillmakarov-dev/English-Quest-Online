using System;
using Cysharp.Threading.Tasks;
using EnglishKingdom.CurrencySystem;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Manages the player's spendable coin balance.
/// Self-registers as <see cref="ICurrencyService"/> via ServiceLocator on Awake
/// and persists the balance through <see cref="SaveManager"/> after every mutation.
/// </summary>
public class CurrencyManager : StaticInstance<CurrencyManager>, ICurrencyService
{
    // ── ICurrencyService ──────────────────────────────────────────────────────

    public event Action<int> OnBalanceChanged;

    public int Balance { get; private set; }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.For(this).Register<ICurrencyService>(this);
        InitializeAsync().Forget();
    }

    protected override void OnDestroy()
    {
        ServiceLocator.DeregisterFor<ICurrencyService>(this);
        base.OnDestroy();
    }

    // ── ICurrencyService Implementation ──────────────────────────────────────

    public async UniTask InitializeAsync()
    {
        // ServiceLocatorGlobal and SaveSystemBootstrapper each run their own Awake.
        // Unity does not guarantee Awake order across GameObjects, so either may not
        // have run yet when this fires.  Poll until both are ready before loading.
        await UniTask.WaitUntil(
            () => ServiceLocator.Global != null &&
                  ServiceLocator.Global.TryGet<ISaveService>(out _),
            cancellationToken: destroyCancellationToken);

        CurrencySaveData data = await SaveManager.LoadCurrencyAsync();
        Balance = Mathf.Max(0, data.Balance);
        OnBalanceChanged?.Invoke(Balance);
        AppLog.Info($"[CurrencyManager] Initialized with balance: {Balance}");
    }

    public void Add(int amount)
    {
        if (amount <= 0)
        {
            AppLog.Warning($"[CurrencyManager] Add called with non-positive amount: {amount}. Ignored.");
            return;
        }

        Balance += amount;
        OnBalanceChanged?.Invoke(Balance);
        PersistAsync().Forget();
        AppLog.Info($"[CurrencyManager] Added {amount}. New balance: {Balance}");
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            AppLog.Warning($"[CurrencyManager] TrySpend called with non-positive amount: {amount}. Ignored.");
            return false;
        }

        if (Balance < amount)
        {
            AppLog.Info($"[CurrencyManager] Insufficient funds. Required: {amount}, Available: {Balance}");
            return false;
        }

        Balance -= amount;
        OnBalanceChanged?.Invoke(Balance);
        PersistAsync().Forget();
        AppLog.Info($"[CurrencyManager] Spent {amount}. Remaining balance: {Balance}");
        return true;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async UniTaskVoid PersistAsync()
    {
        await SaveManager.SaveCurrencyAsync(new CurrencySaveData { Balance = Balance });
    }
}
