using System;
using ResortEmpire.UI;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Energy boost for buildings (FPP-15): during a wave the player spends Energy and one
    /// building serves faster for a short time. Boosts are optional, five stars stay reachable
    /// without them (FPP-18).
    /// </summary>
    public sealed class BoostSystem : MonoBehaviour
    {
        [SerializeField] private EnergyWallet energyWallet;
        [SerializeField] private WaveManager waveManager;
        [SerializeField, Min(1)] private int energyCost = 1;
        [SerializeField, Min(1f)] private float duration = 15f;
        [Tooltip("Service time multiplier while boosted, 0.5 means twice as fast.")]
        [SerializeField, Range(0.1f, 1f)] private float serviceMultiplier = 0.5f;

        /// <summary>Raised after a building got boosted and the Energy was paid.</summary>
        public event Action<Building> BuildingBoosted;

        public int EnergyCost => energyCost;
        public float Duration => duration;

        /// <summary>True when <paramref name="building"/> can be boosted right now.</summary>
        public bool CanBoost(Building building) =>
            building != null && !building.IsBoosted && energyWallet != null && energyWallet.CanAfford(energyCost)
            && waveManager != null && waveManager.State == WaveState.WaveActive;

        /// <summary>Spends Energy and boosts <paramref name="building"/>. Only while a wave runs.</summary>
        public bool TryBoost(Building building)
        {
            if (!CanBoost(building) || !energyWallet.TrySpendEnergy(energyCost)) return false;
            if (!building.StartBoost(duration, serviceMultiplier))
            {
                energyWallet.AddEnergy(energyCost);
                return false;
            }

            BuildingBoosted?.Invoke(building);
            Notifications.Show(building.DisplayName, $"Boosted for {duration:0} s!");
            return true;
        }
    }
}
