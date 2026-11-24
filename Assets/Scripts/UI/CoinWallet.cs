using System;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>Tracks the current coin balance and notifies listeners when it changes.</summary>
    public sealed class CoinWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int balance;

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        /// <summary>Current number of coins.</summary>
        public int Balance => balance;

        private void Awake()
        {
            balance = Mathf.Max(0, balance);
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
