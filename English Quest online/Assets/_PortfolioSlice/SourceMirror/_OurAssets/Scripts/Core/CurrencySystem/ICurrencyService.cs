using System;
using Cysharp.Threading.Tasks;

namespace EnglishKingdom.CurrencySystem
{
    /// <summary>
    /// Provides read/write access to the player's spendable coin balance.
    /// Resolved via ServiceLocator.For(monoBehaviour).Get&lt;ICurrencyService&gt;().
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>Fires after every balance change, passing the new balance.</summary>
        event Action<int> OnBalanceChanged;

        /// <summary>Current spendable coin balance. Always >= 0.</summary>
        int Balance { get; }

        /// <summary>
        /// Adds <paramref name="amount"/> coins to the balance.
        /// Negative values are ignored.
        /// </summary>
        void Add(int amount);

        /// <summary>
        /// Attempts to deduct <paramref name="amount"/> coins from the balance.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the balance was sufficient and the deduction succeeded;
        /// <c>false</c> if the player cannot afford the cost (balance unchanged).
        /// </returns>
        bool TrySpend(int amount);

        /// <summary>
        /// Loads persisted balance from the save backend.
        /// Must be awaited before Add / TrySpend are called.
        /// </summary>
        UniTask InitializeAsync();
    }
}
