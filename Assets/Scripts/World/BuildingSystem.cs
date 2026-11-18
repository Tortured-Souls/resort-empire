using System;
using System.Collections.Generic;
using ResortEmpire.UI;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Owns the buildings of the resort and their workers: places the starting buildings,
    /// unlocks plots after waves and sells them for coins. There is always one worker fewer than
    /// buildings, each worker stands on a building, and moving one is free and instant in any
    /// state (build document §4.5). The wave manager reports progress through
    /// <see cref="CompleteWave"/> and <see cref="SetAwaitingWave"/>; plots can only be bought in
    /// AWAITING_WAVE, while a cruise ship waits at the dock.
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
        [SerializeField] private WorkerAllocationPanel workerPanel;
        [Tooltip("Placed at the start of the game. All but the last one start with a worker.")]
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

        /// <summary>True in AWAITING_WAVE, the only time plots can be bought.</summary>
        public bool IsAwaitingWave => _awaitingWave;

        private void Awake()
        {
            plots.RemoveAll(plot => plot == null);
        }

        private void OnEnable()
        {
            if (wallet != null) wallet.BalanceChanged += OnBalanceChanged;
            if (purchasePanel != null) purchasePanel.BuyClicked += OnBuyClicked;
            if (workerPanel != null) workerPanel.MoveClicked += OnMoveClicked;
            foreach (var plot in plots) plot.Clicked += OnPlotClicked;
            foreach (var building in _buildings) building.Clicked += OnBuildingClicked;
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (purchasePanel != null) purchasePanel.BuyClicked -= OnBuyClicked;
            if (workerPanel != null) workerPanel.MoveClicked -= OnMoveClicked;
            foreach (var plot in plots) plot.Clicked -= OnPlotClicked;
            foreach (var building in _buildings) building.Clicked -= OnBuildingClicked;
        }

        private void Start()
        {
            foreach (var start in startingBuildings)
            {
                if (start.spot == null || start.prefab == null) continue;
                if (start.spot.TryPlace(start.prefab.gameObject, out var placed)) AddBuilding(placed.GetComponent<Building>());
            }

            for (int i = 0; i < Workers; i++) _buildings[i].SetWorker(true);

            if (purchasePanel != null) purchasePanel.Hide();
            if (workerPanel != null) workerPanel.Hide();
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

        /// <summary>Buys <paramref name="plot"/>: pays, places the building and staffs it with a new worker.</summary>
        public bool TryPurchase(BuildingPlot plot)
        {
            if (plot == null || !_awaitingWave || plot.State != PlotState.Available) return false;
            if (wallet == null || !wallet.CanAfford(plot.Price)) return false;
            if (!plot.TryBuild(out var building)) return false;

            wallet.TrySpendCoins(plot.Price);
            AddBuilding(building);
            // The new worker comes with the new building, which would otherwise start empty.
            building.SetWorker(true);
            BuildingPurchased?.Invoke(building);
            WorkersChanged?.Invoke(Workers);
            if (workerPanel != null) workerPanel.Refresh();
            Notifications.Show(plot.DisplayName, "Built! You got 1 new worker.");
            return true;
        }

        /// <summary>
        /// Moves the worker of <paramref name="from"/> to <paramref name="to"/>: free, instant and
        /// allowed during a wave too (WR5). Each building has one worker slot, so
        /// <paramref name="to"/> must be without a worker. Guests already seated in both buildings
        /// change speed right away.
        /// </summary>
        public bool TryMoveWorker(Building from, Building to)
        {
            if (from == to || !_buildings.Contains(from) || !_buildings.Contains(to)) return false;
            if (!from.HasWorker || to.HasWorker) return false;

            from.SetWorker(false);
            to.SetWorker(true);
            // Show the cost of the move as well as the gain (AC2b).
            Notifications.Show(to.DisplayName, $"Full speed now. {from.DisplayName} is empty and slow.");
            return true;
        }

        private void AddBuilding(Building building)
        {
            _buildings.Add(building);
            building.Clicked += OnBuildingClicked;
        }

        private void OnPlotClicked(BuildingPlot plot)
        {
            if (!_awaitingWave || !plot.IsForSale || purchasePanel == null) return;
            if (workerPanel != null) workerPanel.Hide();
            purchasePanel.Show(plot);
        }

        private void OnBuildingClicked(Building building)
        {
            if (workerPanel == null) return;
            if (purchasePanel != null) purchasePanel.Hide();
            workerPanel.Show(building, _buildings);
        }

        private void OnBuyClicked(BuildingPlot plot)
        {
            if (TryPurchase(plot)) purchasePanel.Hide();
        }

        private void OnMoveClicked(Building from, Building to)
        {
            if (TryMoveWorker(from, to)) workerPanel.Hide();
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
