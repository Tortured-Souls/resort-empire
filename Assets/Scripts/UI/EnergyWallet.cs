using System;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Tracks the current Energy balance and notifies listeners when it changes (SCRUM-60).
    /// Energy is an optional boost, never a gate (build document §4.6): it has no cap in the
    /// prototype and cannot be bought for coins.
    /// </summary>
    public sealed class EnergyWallet : MonoBehaviour
    {
        // Energy drop for a finished wave by star rating 1★..5★ (§4.6, TUNING).
        private static readonly int[] EnergyForStars = { 0, 1, 2, 3, 5 };

        [SerializeField, Min(0)] private int balance;

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        /// <summary>Current amount of Energy.</summary>
        public int Balance => balance;

        private void Awake()
        {
            balance = Mathf.Max(0, balance);
        }

        /// <summary>Energy the player gets for a wave rated <paramref name="stars"/> (1–5).</summary>
        public static int RewardForStars(int stars)
        {
            int index = Mathf.Clamp(stars, 1, EnergyForStars.Length) - 1;
            return EnergyForStars[index];
        }

        /// <summary>Adds Energy, e.g. the reward at the end of a wave.</summary>
        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            SetBalance((int)Math.Min(int.MaxValue, (long)balance + amount));
        }

        /// <summary>True when the balance covers <paramref name="amount"/>.</summary>
        public bool CanAfford(int amount) => amount <= balance;

        /// <summary>Spends Energy if the balance is high enough. Returns false otherwise.</summary>
        public bool TrySpendEnergy(int amount)
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
