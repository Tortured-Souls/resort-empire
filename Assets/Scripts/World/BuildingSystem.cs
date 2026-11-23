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
    /// <see cref="CompleteWave"/> and <see cref="SetUpgradesAllowed"/>; plots can only be bought
    /// and buildings upgraded between waves, in AWAITING_WAVE and UPGRADE.
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
        private readonly Dictionary<Building, int> _suggestedLevel = new();
        private int _completedWaves;
        private bool _upgradesAllowed = true;

        /// <summary>Raised after a plot was bought and its building placed.</summary>
        public event Action<Building> BuildingPurchased;

        /// <summary>Raised after a building was upgraded and paid for.</summary>
        public event Action<Building> BuildingUpgraded;

        /// <summary>
        /// Raised in AWAITING_WAVE or UPGRADE when the player can newly afford the next level of a
        /// building, once per building and level. The upgrade suggestion panel listens to it.
        /// </summary>
        public event Action<Building> UpgradeAvailable;

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

        /// <summary>
        /// True in AWAITING_WAVE and UPGRADE, the only states in which plots can be bought and
        /// buildings upgraded.
        /// </summary>
        public bool UpgradesAllowed => _upgradesAllowed;

        /// <summary>True when <paramref name="building"/> can be upgraded right now.</summary>
        public bool CanUpgrade(Building building) =>
            building != null && _upgradesAllowed && building.CanUpgrade && wallet != null && wallet.CanAfford(building.UpgradePrice);

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
            foreach (var building in _buildings)
            {
                Subscribe(building);
                if (building.NeedsQueueWarning) OnQueueWarningStarted(building);
            }
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (purchasePanel != null) purchasePanel.BuyClicked -= OnBuyClicked;
            if (workerPanel != null) workerPanel.MoveClicked -= OnMoveClicked;
            foreach (var plot in plots) plot.Clicked -= OnPlotClicked;
            foreach (var building in _buildings)
            {
                building.Clicked -= OnBuildingClicked;
                building.QueueWarningStarted -= OnQueueWarningStarted;
            }
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

        /// <summary>Rebuilds a saved resort: marks waves as completed and places bought plots for free.</summary>
        /// <param name="completedWaves">Number of the last completed wave.</param>
        /// <param name="boughtPlots">Indices into <see cref="Plots"/> of the plots the player owns.</param>
        public void Restore(int completedWaves, IEnumerable<int> boughtPlots)
        {
            _completedWaves = Mathf.Max(_completedWaves, completedWaves);
            foreach (int index in boughtPlots)
            {
                if (index < 0 || index >= plots.Count) continue;
                if (plots[index].TryBuild(out var building)) _buildings.Add(building);
            }

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

        /// <summary>Called by the wave manager when entering or leaving AWAITING_WAVE or UPGRADE.</summary>
        public void SetUpgradesAllowed(bool allowed)
        {
            _upgradesAllowed = allowed;
            if (!allowed && purchasePanel != null) purchasePanel.Hide();
            if (allowed) SuggestUpgrades();
        }

        /// <summary>Buys <paramref name="plot"/>: pays, places the building and staffs it with a new worker.</summary>
        public bool TryPurchase(BuildingPlot plot)
        {
            if (plot == null || !_upgradesAllowed || plot.State != PlotState.Available) return false;
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

            to.SetWorker(true);
            from.SetWorker(false);
            // Show the cost of the move as well as the gain (AC2b).
            Notifications.Show(to.DisplayName, $"Full speed now. {from.DisplayName} is empty and slow.");
            return true;
        }

        private void AddBuilding(Building building)
        {
            _buildings.Add(building);
            Subscribe(building);
        }

        private void Subscribe(Building building)
        {
            building.Clicked += OnBuildingClicked;
            building.QueueWarningStarted += OnQueueWarningStarted;
        }

        private void OnQueueWarningStarted(Building building)
        {
            int episode = building.QueueEpisode;
            Notifications.ShowQueueWarning($"{building.DisplayName}: slow service",
                $"At least {Building.QueueWarningThreshold} guests are waiting.",
                () => this != null && isActiveAndEnabled && building != null && building.isActiveAndEnabled
                    && building.NeedsQueueWarning && building.QueueEpisode == episode,
                HasWorkerToMove,
                () => MoveRandomWorkerTo(building));
        }

        private bool HasWorkerToMove()
        {
            foreach (var building in _buildings)
                if (CanDonateWorker(building)) return true;
            return false;
        }

        private static bool CanDonateWorker(Building building) =>
            building != null && building.isActiveAndEnabled && building.HasWorker;

        private void MoveRandomWorkerTo(Building target)
        {
            if (!target.NeedsQueueWarning) return;

            // Reservoir sampling gives each currently staffed building the same chance, without a list.
            Building donor = null;
            int count = 0;
            foreach (var building in _buildings)
            {
                if (CanDonateWorker(building) && UnityEngine.Random.Range(0, ++count) == 0) donor = building;
            }

            if (donor != null && TryMoveWorker(donor, target) && workerPanel != null) workerPanel.Refresh();
        }

        /// <summary>Upgrades <paramref name="building"/> by one level and pays for it. Only in AWAITING_WAVE or UPGRADE.</summary>
        public bool TryUpgrade(Building building)
        {
            if (!CanUpgrade(building) || !_buildings.Contains(building)) return false;

            int price = building.UpgradePrice;
            if (!wallet.TrySpendCoins(price)) return false;
            if (!building.TryUpgrade())
            {
                wallet.AddCoins(price);
                return false;
            }

            BuildingUpgraded?.Invoke(building);
            Notifications.Show(building.DisplayName, $"Upgraded to level {building.Level}!");
            return true;
        }

        // Suggests every upgrade the player can afford, each level only once.
        private void SuggestUpgrades()
        {
            if (!_upgradesAllowed) return;

            foreach (var building in _buildings)
            {
                if (!CanUpgrade(building)) continue;
                if (_suggestedLevel.TryGetValue(building, out int level) && level >= building.Level) continue;

                _suggestedLevel[building] = building.Level;
                // Until the upgrade panel (SCRUM-109) listens, a toast tells the player.
                if (UpgradeAvailable != null) UpgradeAvailable.Invoke(building);
                else Notifications.Show("Upgrade available!", $"{building.DisplayName}: level {building.Level + 1} for {building.UpgradePrice:N0} coins.");
            }
        }

        private void OnPlotClicked(BuildingPlot plot)
        {
            if (!_upgradesAllowed || !plot.IsForSale || purchasePanel == null) return;
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

        private void OnBalanceChanged(int balance)
        {
            RefreshPlots();
            SuggestUpgrades();
        }

        private void RefreshPlots()
        {
            int coins = wallet != null ? wallet.Balance : 0;
            foreach (var plot in plots) plot.Refresh(_completedWaves, coins);

            if (purchasePanel != null && purchasePanel.IsOpen) purchasePanel.Refresh();
        }
    }
}
