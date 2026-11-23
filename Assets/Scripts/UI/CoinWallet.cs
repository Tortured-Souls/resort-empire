using System;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>Tracks the current coin balance and notifies listeners when it changes.</summary>
    public sealed class CoinWallet : MonoBehaviour
    {
        // coins_per_unit and coins_rating_mult for 1★..5★ and Perfect (build document §4.8, TUNING).
        private const int CoinsPerUnit = 16;
        private static readonly float[] CoinsForRating = { 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.3f };

        [SerializeField, Min(0)] private int balance;

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        /// <summary>Current number of coins.</summary>
        public int Balance => balance;

        private void Awake()
        {
            balance = Mathf.Max(0, balance);
        }

        /// <summary>
        /// Coins for a finished wave: its need <paramref name="units"/> × coins_per_unit × the
        /// multiplier of a <paramref name="stars"/> (1–5) or Perfect rating.
        /// </summary>
        public static int RewardForWave(int units, int stars, bool perfect)
        {
            int tier = perfect ? CoinsForRating.Length - 1 : Mathf.Clamp(stars, 1, CoinsForRating.Length - 1) - 1;
            return Mathf.RoundToInt(Mathf.Max(0, units) * CoinsPerUnit * CoinsForRating[tier]);
        }

        /// <summary>Adds coins, e.g. the reward at the end of a wave.</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            SetBalance((int)Math.Min(int.MaxValue, (long)balance + amount));
        }

        /// <summary>True when the balance covers <paramref name="amount"/>.</summary>
        public bool CanAfford(int amount) => amount <= balance;

        /// <summary>Spends coins if the balance is high enough. Returns false otherwise.</summary>
        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || amount > balance) return false;
            SetBalance(balance - amount);
            return true;
        }

        /// <summary>Sets the balance directly, e.g. when loading a save.</summary>
        public void SetBalance(int amount)
        {
            int next = Mathf.Max(0, amount);
            if (balance == next) return;

            balance = next;
            BalanceChanged?.Invoke(balance);
        }
    }
}
