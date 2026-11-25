using System;
using System.Collections.Generic;
using ResortEmpire.UI;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Energy boost Speed Up (FPP-15, SCRUM-146): during a wave the player spends Energy and one
    /// building serves twice as fast for a short time. Each building can be boosted once per wave,
    /// in the arrival as well as after it. A boost does not add seats and is strongest on a
    /// staffed building. Boosts are optional, five stars stay reachable without them (FPP-18).
    /// </summary>
    public sealed class BoostSystem : MonoBehaviour
    {
        /// <summary>What a building offers in the boost UI right now.</summary>
        public enum Offer
        {
            /// <summary>No wave is running, nothing to speed up.</summary>
            Hidden,

            /// <summary>The player can pay and boost.</summary>
            Ready,

            /// <summary>A boost is running on the building.</summary>
            Boosted,

            /// <summary>The building already had its boost this wave.</summary>
            Used,

            /// <summary>Not enough Energy; the button offers an ad instead.</summary>
            NeedEnergy
        }

        [SerializeField] private EnergyWallet energyWallet;
        [SerializeField] private WaveManager waveManager;
        [Tooltip("Energy for one Speed Up (TUNING, e.g. 3).")]
        [SerializeField, Min(1)] private int energyCost = 3;
        [Tooltip("energy_speed_duration_s: seconds the building stays boosted.")]
        [SerializeField, Min(1f)] private float duration = 15f;
        [Tooltip("Service time multiplier while boosted, 0.5 means twice as fast (energy_speed_reduction_pct 0.50).")]
        [SerializeField, Range(0.1f, 1f)] private float serviceMultiplier = 0.5f;
        [Tooltip("Energy granted for watching an ad. The prototype has no ads yet, the reward is given at once.")]
        [SerializeField, Min(0)] private int adEnergyReward = 3;

        private readonly HashSet<Building> _usedThisWave = new();

        /// <summary>Raised after a building got boosted and the Energy was paid.</summary>
        public event Action<Building> BuildingBoosted;

        public int EnergyCost => energyCost;
        public float Duration => duration;

        private void OnEnable()
        {
            if (waveManager != null) waveManager.StateChanged += OnWaveStateChanged;
        }

        private void OnDisable()
        {
            if (waveManager != null) waveManager.StateChanged -= OnWaveStateChanged;
        }

        private bool WaveRunning => waveManager != null && waveManager.State == WaveState.WaveActive;

        /// <summary>True when <paramref name="building"/> was already boosted in the running wave.</summary>
        public bool WasUsedThisWave(Building building) => building != null && _usedThisWave.Contains(building);

        /// <summary>True when <paramref name="building"/> can be boosted right now.</summary>
        public bool CanBoost(Building building) =>
            building != null && !building.IsBoosted && !WasUsedThisWave(building) && WaveRunning
            && energyWallet != null && energyWallet.CanAfford(energyCost);

        /// <summary>What the boost button of <paramref name="building"/> offers right now.</summary>
        public Offer OfferFor(Building building)
        {
            if (building == null || !WaveRunning) return Offer.Hidden;
            if (building.IsBoosted) return Offer.Boosted;
            if (WasUsedThisWave(building)) return Offer.Used;
            return energyWallet != null && energyWallet.CanAfford(energyCost) ? Offer.Ready : Offer.NeedEnergy;
        }

        /// <summary>Spends Energy and boosts <paramref name="building"/>. Only while a wave runs, once per building.</summary>
        public bool TryBoost(Building building)
        {
            if (!CanBoost(building) || !energyWallet.TrySpendEnergy(energyCost)) return false;
            if (!building.StartBoost(duration, serviceMultiplier))
            {
                energyWallet.AddEnergy(energyCost);
                return false;
            }

            _usedThisWave.Add(building);
            BuildingBoosted?.Invoke(building);
            Notifications.Show(building.DisplayName, $"Twice as fast for {duration:0} s!");
            return true;
        }

        /// <summary>
        /// "Watch ad for ⚡" when the player is out of Energy. Prototype stub: there is no ad SDK
        /// yet, so the reward is granted right away.
        /// </summary>
        public void WatchAdForEnergy()
        {
            if (energyWallet == null || adEnergyReward <= 0) return;
            energyWallet.AddEnergy(adEnergyReward);
            Notifications.Show("Thanks for watching!", $"+{adEnergyReward} ⚡ Energy");
        }

        // A new wave gives every building its boost back.
        private void OnWaveStateChanged(WaveState previous, WaveState next)
        {
            if (next == WaveState.WaveIncoming) _usedThisWave.Clear();
        }
    }
}
