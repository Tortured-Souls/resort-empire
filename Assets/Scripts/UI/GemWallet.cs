using System;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>Tracks the current gem balance and notifies listeners when it changes.</summary>
    public sealed class GemWallet : MonoBehaviour
    {
        private void Awake()
        {
            balance = Mathf.Max(0, balance);
        }

        [SerializeField, Min(0)] private int balance;
        public int Balance => balance;
        public event Action<int> BalanceChanged;

        public void AddGems(int amount)
        {
            if (amount <= 0)
                return;

            SetBalance((int)Math.Min((long)int.MaxValue, (long)balance + amount));
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || amount > balance)
                return false;

            SetBalance(balance - amount);
            return true;
        }

        public void SetBalance(int amount)
        {
            var nextBalance = Mathf.Max(0, amount);
            if (balance == nextBalance)
                return;

            balance = nextBalance;
            BalanceChanged?.Invoke(balance);
        }
    }
}
