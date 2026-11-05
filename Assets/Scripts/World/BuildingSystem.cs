using System;
using System.Collections.Generic;
using ResortEmpire.UI;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Owns the buildings of the resort: places the starting buildings, unlocks plots after
    /// waves, sells them for coins and keeps the worker count (always one less than buildings).
    /// The wave manager reports progress through <see cref="CompleteWave"/> and
    /// <see cref="SetAwaitingWave"/>; plots can only be bought while awaiting the next wave.
    /// </summary>
    public sealed class BuildingSystem : MonoBehaviour
    {
        [Serializable]
        private struct StartingBuilding
        {
            public BuildingSpot spot;
            public Building prefab;
        }

        [SerializeField] private CoinWallet wallet;
        [SerializeField] private BuildingPurchasePanel purchasePanel;
        [SerializeField] private List<StartingBuilding> startingBuildings = new();
        [SerializeField] private List<BuildingPlot> plots = new();

        private readonly List<Building> _buildings = new();
        private int _completedWaves;
        private bool _awaitingWave = true;

        /// <summary>Raised after a plot was bought and its building placed.</summary>
        public event Action<Building> BuildingPurchased;

        /// <summary>Raised with the new worker count.</summary>
        public event Action<int> WorkersChanged;

        /// <summary>All buildings the player owns.</summary>
        public IReadOnlyList<Building> Buildings => _buildings;

        /// <summary>All purchasable plots.</summary>
        public IReadOnlyList<BuildingPlot> Plots => plots;

        /// <summary>Number of workers: one per building, minus one so the player has to move them.</summary>
        public int Workers => Mathf.Max(0, _buildings.Count - 1);

        /// <summary>Number of the last completed wave, 0 before the first one.</summary>
        public int CompletedWaves => _completedWaves;

        /// <summary>True between waves (AWAITING_WAVE), the only time plots can be bought.</summary>
        public bool IsAwaitingWave => _awaitingWave;

        private void Awake()
        {
            plots.RemoveAll(plot => plot == null);
        }

        private void OnEnable()
        {
            if (wallet != null) wallet.BalanceChanged += OnBalanceChanged;
            if (purchasePanel != null) purchasePanel.BuyClicked += OnBuyClicked;
            foreach (var plot in plots) plot.Clicked += OnPlotClicked;
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (purchasePanel != null) purchasePanel.BuyClicked -= OnBuyClicked;
            foreach (var plot in plots) plot.Clicked -= OnPlotClicked;
        }

        private void Start()
        {
            foreach (var start in startingBuildings)
            {
                if (start.spot == null || start.prefab == null) continue;
                if (start.spot.TryPlace(start.prefab.gameObject, out var placed)) _buildings.Add(placed.GetComponent<Building>());
            }

            if (purchasePanel != null) purchasePanel.Hide();
            RefreshPlots();
            WorkersChanged?.Invoke(Workers);
        }

        /// <summary>Called by the wave manager when <paramref name="wave"/> (1-based) is finished.</summary>
        public void CompleteWave(int wave)
        {
            if (wave <= _completedWaves) return;

            _completedWaves = wave;
            RefreshPlots();
        }

        /// <summary>Called by the wave manager when entering or leaving AWAITING_WAVE.</summary>
        public void SetAwaitingWave(bool awaiting)
        {
            _awaitingWave = awaiting;
            if (!awaiting && purchasePanel != null) purchasePanel.Hide();
        }

        /// <summary>Buys <paramref name="plot"/>: pays, places the building and adds a worker.</summary>
        public bool TryPurchase(BuildingPlot plot)
        {
            if (plot == null || !_awaitingWave || plot.State != PlotState.Available) return false;
            if (wallet == null || !wallet.CanAfford(plot.Price)) return false;
            if (!plot.TryBuild(out var building)) return false;

            wallet.TrySpendCoins(plot.Price);
            _buildings.Add(building);
            BuildingPurchased?.Invoke(building);
            WorkersChanged?.Invoke(Workers);
            Notifications.Show(plot.DisplayName, "Built! You got 1 new worker.");
            return true;
        }

        private void OnPlotClicked(BuildingPlot plot)
        {
            if (!_awaitingWave || !plot.IsForSale || purchasePanel == null) return;
            purchasePanel.Show(plot);
        }

        private void OnBuyClicked(BuildingPlot plot)
        {
            if (TryPurchase(plot)) purchasePanel.Hide();
        }

        private void OnBalanceChanged(int balance) => RefreshPlots();

        private void RefreshPlots()
        {
            int coins = wallet != null ? wallet.Balance : 0;
            foreach (var plot in plots) plot.Refresh(_completedWaves, coins);

            if (purchasePanel != null && purchasePanel.IsOpen) purchasePanel.Refresh();
        }
    }
}
