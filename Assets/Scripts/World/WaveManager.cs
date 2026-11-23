using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using ResortEmpire.UI;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Runs the wave loop (build document §4.3): in AWAITING_WAVE the player presses
    /// <c>Start Wave</c>, the cruise ship sails in (WAVE_INCOMING), its guests disembark and the
    /// wave runs until every guest is served (WAVE_ACTIVE). WAVE_RESOLVING works out the
    /// <see cref="Report"/>, WAVE_COMPLETE waits for the player to claim its rewards, which ends
    /// the wave, and UPGRADE for them to upgrade or skip; then the resort awaits the next wave. An
    /// active wave pauses while the app is in the background (WAVE_PAUSED). Reports completed
    /// waves and the states that allow upgrades to the <see cref="BuildingSystem"/>.
    /// </summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private CasualTourist guestPrefab;
        [SerializeField] private Transform guestContainer;
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField, Min(1)] private int guestsPerWave = 12;
        [Tooltip("wave_timer: how long guests keep arriving. It closes the arrival, never the wave (WR3).")]
        [SerializeField, Range(45f, 180f)] private float waveTimer = 90f;
        [Tooltip("Length of WAVE_INCOMING: the cruise ship's arrival animation.")]
        [SerializeField, Range(2f, 4f)] private float incomingSeconds = 3f;
        [SerializeField] private SatisfactionSettings satisfaction = SatisfactionSettings.Default;

        private readonly List<CasualTourist> _guests = new();
        private readonly List<Building> _buildings = new();
        private readonly HashSet<NeedType> _servedNeeds = new();
        private readonly List<NeedType> _guestNeeds = new();
        private readonly List<Need> _waveNeeds = new();
        private readonly List<Need> _needsOfType = new();
        private WaveState _state = WaveState.AwaitingWave;
        private GameWorldController _world;
        private CruiseShip _ship;
        private CoinWallet _coinWallet;
        private EnergyWallet _energyWallet;
        private WaveReport _report;
        private int _wave;
        private float _arrivalElapsed;
        private bool _arrivalOpen;

        /// <summary>Raised with the previous and the new state.</summary>
        public event Action<WaveState, WaveState> StateChanged;

        /// <summary>
        /// Raised on entering WAVE_COMPLETE. The result screen (S4) shows <see cref="Report"/> and
        /// calls <see cref="ClaimRewards"/>.
        /// </summary>
        public event Action WaveCompleted;

        /// <summary>
        /// Raised on entering UPGRADE. The upgrade suggestion (S5) calls <see cref="FinishUpgrade"/>
        /// after <c>Upgrade Now</c> or <c>Later</c>.
        /// </summary>
        public event Action UpgradeOffered;

        public WaveState State => _state;

        /// <summary>Number of the running or the last finished wave, 1-based; 0 before the first.</summary>
        public int Wave => _wave;

        /// <summary>True during the first phase of WAVE_ACTIVE, while guests still disembark.</summary>
        public bool IsArrivalOpen => _arrivalOpen;

        /// <summary>Guests of the current wave that have already disembarked.</summary>
        public IReadOnlyList<CasualTourist> Guests => _guests;

        /// <summary>
        /// Result of the last resolved wave: Wave Satisfaction, rating, rewards and the breakdown
        /// per need. Null before the first wave is resolved.
        /// </summary>
        public WaveReport Report => _report;

        private void Start()
        {
            _world = GameWorldController.Instance;
            _ship = _world != null ? _world.CruiseShip : null;
            _coinWallet = FindAnyObjectByType<CoinWallet>();
            _energyWallet = FindAnyObjectByType<EnergyWallet>();
            if (_ship != null && guestPrefab != null && buildingSystem != null && _coinWallet != null && _energyWallet != null)
            {
                buildingSystem.SetUpgradesAllowed(AllowsUpgrades(_state));
                return;
            }

            Debug.LogError($"{nameof(WaveManager)} needs a {nameof(GameWorldController)} with a cruise ship, a guest prefab, a {nameof(BuildingSystem)}, a {nameof(CoinWallet)} and an {nameof(EnergyWallet)}.", this);
            enabled = false;
        }

        /// <summary>Continues a saved game: the next started wave follows <paramref name="completedWaves"/>.</summary>
        /// <param name="completedWaves">Number of the last completed wave.</param>
        public void ResumeFrom(int completedWaves)
        {
            if (_state != WaveState.AwaitingWave) return;
            _wave = Mathf.Max(0, completedWaves);
        }

        /// <summary>
        /// Player action from AWAITING_WAVE: the cruise ship sails in with the next wave of guests.
        /// Returns false in any other state.
        /// </summary>
        public bool StartWave()
        {
            if (_state != WaveState.AwaitingWave) return false;

            CollectServices();
            _wave++;
            _arrivalElapsed = 0f;
            SetState(WaveState.WaveIncoming);
            return true;
        }

        /// <summary>
        /// Player action from WAVE_COMPLETE (<c>Claim Rewards</c>): pays out the coins and Energy of
        /// <see cref="Report"/>, which ends the wave, and the upgrade suggestion follows. Returns
        /// false in any other state.
        /// </summary>
        public bool ClaimRewards()
        {
            if (_state != WaveState.WaveComplete) return false;

            _coinWallet.AddCoins(_report.CoinsEarned);
            _energyWallet.AddEnergy(_report.EnergyEarned);
            // The wave counts as completed together with its rewards, so a game closed before
            // claiming replays it. It also opens the area unlock (S9) for UPGRADE.
            buildingSystem.CompleteWave(_wave);
            SetState(WaveState.Upgrade);
            // Until the upgrade suggestion (SCRUM-155) listens, there is nothing to upgrade or skip.
            if (UpgradeOffered != null) UpgradeOffered.Invoke();
            else FinishUpgrade();
            return true;
        }

        /// <summary>
        /// Player action from UPGRADE, after <c>Upgrade Now</c> or <c>Later</c>: the resort awaits
        /// the next wave. Returns false in any other state.
        /// </summary>
        public bool FinishUpgrade()
        {
            if (_state != WaveState.Upgrade) return false;

            SetState(WaveState.AwaitingWave);
            return true;
        }

        private void Update()
        {
            switch (_state)
            {
                case WaveState.WaveIncoming:
                    // A ship still leaving after the last wave has to be gone before it sails in again.
                    if (_ship.CurrentPhase == CruiseShip.Phase.Away) _ship.SailIn(incomingSeconds);
                    if (!_ship.IsDocked) break;
                    _arrivalOpen = true;
                    SetState(WaveState.WaveActive);
                    // The first guest steps off as soon as the ship docks.
                    TickArrival(0f);
                    break;

                case WaveState.WaveActive:
                    if (_arrivalOpen) TickArrival(Time.deltaTime);
                    else if (AllGuestsServed()) SetState(WaveState.WaveResolving);
                    break;

                case WaveState.WaveResolving:
                    ResolveWave();
                    break;
            }
        }

        // Only an active wave pauses: the arrival timer stops in Update and every guest freezes.
        private void OnApplicationPause(bool paused)
        {
            if (paused && _state == WaveState.WaveActive) SetPaused(true);
            else if (!paused && _state == WaveState.WavePaused) SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            foreach (var guest in _guests)
            {
                if (guest != null) guest.SetWavePaused(paused);
            }

            SetState(paused ? WaveState.WavePaused : WaveState.WaveActive);
        }

        private void TickArrival(float deltaTime)
        {
            _arrivalElapsed = Mathf.Min(_arrivalElapsed + deltaTime, waveTimer);

            // Guest k disembarks at k * waveTimer / guestsPerWave, so the last one is off the
            // ship before the timer runs out.
            while (_guests.Count < guestsPerWave && _guests.Count * waveTimer <= _arrivalElapsed * guestsPerWave)
            {
                SpawnGuest();
            }

            if (_arrivalElapsed < waveTimer) return;
            _arrivalOpen = false;
            _ship.SailAway();
        }

        private void SpawnGuest()
        {
            var guest = Instantiate(guestPrefab, _ship.SpawnPoint.position, Quaternion.identity, guestContainer);
            KeepServedNeeds(guest.Needs);
            guest.BeginVisit(_ship, _buildings);
            _guests.Add(guest);
        }

        private void CollectServices()
        {
            _buildings.Clear();
            _servedNeeds.Clear();
            foreach (var spot in _world.BuildingSpots)
            {
                if (!spot.IsOccupied || !spot.Building.TryGetComponent(out Building building)) continue;
                _buildings.Add(building);
                _servedNeeds.Add(building.ServedNeed);
            }
        }

        // WR9: a need whose building is not on the island is never spawned. The guest would wait
        // for that service forever and the wave could never end. Needs of later waves are dropped too.
        private void KeepServedNeeds(NpcNeeds needs)
        {
            _guestNeeds.Clear();
            foreach (var need in needs.Needs)
            {
                if (_servedNeeds.Contains(need.Type) && need.IsActiveInWave(_wave)) _guestNeeds.Add(need.Type);
            }

            if (_guestNeeds.Count < needs.Needs.Count) needs.SetNeeds(_guestNeeds.ToArray());
        }

        private bool AllGuestsServed()
        {
            foreach (var guest in _guests)
            {
                if (guest != null && !guest.StateMachine.Is(NpcState.Completed)) return false;
            }

            return true;
        }

        // The result comes from the waiting history the guests carry, so they leave only after it.
        private void ResolveWave()
        {
            _report = CreateReport();
            RemoveGuests();
            SetState(WaveState.WaveComplete);
            // Until the result screen (SCRUM-154) listens, there is nothing to show before claiming.
            if (WaveCompleted != null) WaveCompleted.Invoke();
            else ClaimRewards();
        }

        // Wave Satisfaction, rating and rewards (build document §4.4, §4.6, §4.8). Every guest has
        // been served, so the units of the wave are all needs its guests carried.
        private WaveReport CreateReport()
        {
            _waveNeeds.Clear();
            foreach (var guest in _guests)
            {
                if (guest != null) _waveNeeds.AddRange(guest.Needs.Needs);
            }

            var report = new WaveReport
            {
                Wave = _wave,
                GuestsServed = _guests.Count,
                GuestsTotal = _guests.Count,
                Satisfaction = WaveSatisfaction.Calculate(_waveNeeds, satisfaction),
                NeedResults = ResultsByNeed()
            };
            report.CoinsEarned = CoinWallet.RewardForWave(_waveNeeds.Count, report.Stars, report.IsPerfect);
            report.EnergyEarned = EnergyWallet.RewardForStars(report.Stars);
            return report;
        }

        // Breakdown of the report: satisfaction of every need type the guests had, in NeedType order.
        private List<NeedResult> ResultsByNeed()
        {
            var results = new List<NeedResult>();
            foreach (NeedType type in Enum.GetValues(typeof(NeedType)))
            {
                _needsOfType.Clear();
                foreach (var need in _waveNeeds)
                {
                    if (need.Type == type) _needsOfType.Add(need);
                }

                if (_needsOfType.Count > 0)
                    results.Add(new NeedResult(Need.DisplayName(type), WaveSatisfaction.Calculate(_needsOfType, satisfaction)));
            }

            return results;
        }

        private void RemoveGuests()
        {
            foreach (var guest in _guests)
            {
                if (guest != null) Destroy(guest.gameObject);
            }

            _guests.Clear();
        }

        // WR6: no upgrades during a wave; plots and upgrades are for the time between waves.
        private static bool AllowsUpgrades(WaveState state) =>
            state is WaveState.AwaitingWave or WaveState.Upgrade;

        private void SetState(WaveState next)
        {
            var previous = _state;
            _state = next;
            buildingSystem.SetUpgradesAllowed(AllowsUpgrades(next));
            StateChanged?.Invoke(previous, next);
        }
    }
}
