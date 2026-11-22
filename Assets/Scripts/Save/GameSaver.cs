using ResortEmpire.UI;
using ResortEmpire.World;
using UnityEngine;

namespace ResortEmpire.Save
{
    /// <summary>
    /// Loads the local save when the GameWorld scene starts and writes it periodically, on every
    /// wave state change, every bought plot and when the player leaves the game.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GameSaver : MonoBehaviour
    {
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private WaveManager waveManager;
        [SerializeField, Min(1f)] private float autosaveIntervalSeconds = 10f;

        private CoinWallet _coinWallet;
        private GemWallet _gemWallet;
        private EnergyWallet _energyWallet;
        private string _path;
        private float _secondsSinceSave;

        private void Start()
        {
            _path = SaveStorage.DefaultPath;
            _coinWallet = FindAnyObjectByType<CoinWallet>();
            _gemWallet = FindAnyObjectByType<GemWallet>();
            _energyWallet = FindAnyObjectByType<EnergyWallet>();

            if (buildingSystem == null || waveManager == null)
            {
                Debug.LogError($"{nameof(GameSaver)} needs a {nameof(BuildingSystem)} and a {nameof(WaveManager)}.", this);
                enabled = false;
                return;
            }

            if (SaveStorage.TryRead(_path, out var data)) Apply(data);

            buildingSystem.BuildingPurchased += OnBuildingPurchased;
            waveManager.StateChanged += OnWaveStateChanged;
        }

        private void OnDestroy()
        {
            if (buildingSystem != null) buildingSystem.BuildingPurchased -= OnBuildingPurchased;
            if (waveManager != null) waveManager.StateChanged -= OnWaveStateChanged;
        }

        private void Update()
        {
            _secondsSinceSave += Time.unscaledDeltaTime;
            if (_secondsSinceSave >= autosaveIntervalSeconds) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnBuildingPurchased(Building building) => Save();

        private void OnWaveStateChanged(WaveState previous, WaveState next) => Save();

        private void Apply(SaveData data)
        {
            if (_coinWallet != null) _coinWallet.SetBalance(data.coins);
            if (_gemWallet != null) _gemWallet.SetBalance(data.gems);
            if (_energyWallet != null) _energyWallet.SetBalance(data.energy);

            buildingSystem.Restore(data.completedWaves, data.boughtPlots);
            waveManager.ResumeFrom(data.completedWaves);

            foreach (var saved in data.buildings)
            {
                var building = FindBuilding(saved.spotIndex);
                if (building == null) continue;

                while (building.Level < saved.level)
                {
                    if (!building.TryUpgrade()) break;
                }
            }
        }

        private SaveData Capture()
        {
            var data = new SaveData
            {
                coins = _coinWallet != null ? _coinWallet.Balance : 0,
                gems = _gemWallet != null ? _gemWallet.Balance : 0,
                energy = _energyWallet != null ? _energyWallet.Balance : 0,
                completedWaves = buildingSystem.CompletedWaves
            };

            var plots = buildingSystem.Plots;
            for (int i = 0; i < plots.Count; i++)
            {
                if (plots[i].State == PlotState.Unlocked) data.boughtPlots.Add(i);
            }

            var world = GameWorldController.Instance;
            if (world == null) return data;

            foreach (var spot in world.BuildingSpots)
            {
                if (spot.IsOccupied && spot.Building.TryGetComponent(out Building building))
                    data.buildings.Add(new SavedBuilding(spot.Index, building.Level));
            }

            return data;
        }

        private void Save()
        {
            if (!enabled || buildingSystem == null || waveManager == null) return;
            _secondsSinceSave = 0f;
            SaveStorage.Write(Capture(), _path);
        }

        private static Building FindBuilding(int spotIndex)
        {
            var world = GameWorldController.Instance;
            if (world == null) return null;

            foreach (var spot in world.BuildingSpots)
            {
                if (spot.Index == spotIndex && spot.IsOccupied && spot.Building.TryGetComponent(out Building building))
                    return building;
            }

            return null;
        }
    }
}
