using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Runs one wave from <c>Start Wave</c> until every guest is served
    /// </summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private CasualTourist guestPrefab;
        [SerializeField] private Transform guestContainer;
        [SerializeField, Min(1)] private int guestsPerWave = 12;
        [Tooltip("wave_timer: how long guests keep arriving. It closes the arrival, never the wave (WR3).")]
        [SerializeField, Range(45f, 180f)] private float waveTimer = 90f;
        [Tooltip("Length of WAVE_INCOMING: the cruise ship's arrival animation.")]
        [SerializeField, Range(2f, 4f)] private float incomingSeconds = 3f;

        private readonly List<CasualTourist> _guests = new();
        private readonly List<Building> _buildings = new();
        private readonly HashSet<NeedType> _servedNeeds = new();
        private readonly List<NeedType> _guestNeeds = new();
        private WaveState _state = WaveState.AwaitingWave;
        private CruiseShip _ship;
        private int _wave;
        private float _arrivalElapsed;
        private bool _arrivalOpen;

        /// <summary>Raised with the previous and the new state.</summary>
        public event Action<WaveState, WaveState> StateChanged;

        public WaveState State => _state;

        /// <summary>Number of the running or the last finished wave, 1-based; 0 before the first.</summary>
        public int Wave => _wave;

        /// <summary>True during the first phase of WAVE_ACTIVE, while guests still disembark.</summary>
        public bool IsArrivalOpen => _arrivalOpen;

        /// <summary>Guests of the current wave that have already disembarked.</summary>
        public IReadOnlyList<CasualTourist> Guests => _guests;

        /// <summary>Player action from AWAITING_WAVE. Returns false in any other state.</summary>
        public bool StartWave()
        {
            if (_state != WaveState.AwaitingWave) return false;

            var world = GameWorldController.Instance;
            if (world == null || world.CruiseShip == null || guestPrefab == null)
            {
                Debug.LogError($"{nameof(WaveManager)} needs a {nameof(GameWorldController)} with a cruise ship and a guest prefab.", this);
                return false;
            }

            _ship = world.CruiseShip;
            CollectServices(world);
            _wave++;
            _arrivalElapsed = 0f;
            _ship.SailIn(incomingSeconds);
            SetState(WaveState.WaveIncoming);
            return true;
        }

        private void Update()
        {
            switch (_state)
            {
                case WaveState.WaveIncoming:
                    if (!_ship.IsDocked) break;
                    _arrivalOpen = true;
                    SetState(WaveState.WaveActive);
                    // The first guest steps off as soon as the ship docks.
                    TickArrival(0f);
                    break;

                case WaveState.WaveActive:
                    if (_arrivalOpen) TickArrival(Time.deltaTime);
                    else if (AllGuestsServed()) FinishWave();
                    break;
            }
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

        private void CollectServices(GameWorldController world)
        {
            _buildings.Clear();
            _servedNeeds.Clear();
            foreach (var spot in world.BuildingSpots)
            {
                if (!spot.IsOccupied || !spot.Building.TryGetComponent(out Building building)) continue;
                _buildings.Add(building);
                _servedNeeds.Add(building.ServedNeed);
            }
        }

        // WR9: a need whose building is not on the island is never spawned. The guest would wait
        // for that service forever and the wave could never end.
        private void KeepServedNeeds(NpcNeeds needs)
        {
            _guestNeeds.Clear();
            foreach (var need in needs.Needs)
            {
                if (_servedNeeds.Contains(need.Type)) _guestNeeds.Add(need.Type);
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

        private void FinishWave()
        {
            // Guests leave only once the wave is over.
            foreach (var guest in _guests)
            {
                if (guest != null) Destroy(guest.gameObject);
            }

            _guests.Clear();
            SetState(WaveState.AwaitingWave);
        }

        private void SetState(WaveState next)
        {
            var previous = _state;
            _state = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
