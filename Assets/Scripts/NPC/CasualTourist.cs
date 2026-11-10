using System;
using System.Collections.Generic;
using ResortEmpire.World;
using UnityEngine;

namespace ResortEmpire.NPC
{
    /// <summary>
    /// Visual carrier of service requests. A wave owner starts the visit and supplies pause state;
    /// this component does not spawn waves, score guests, or remove impatient guests.
    /// </summary>
    [RequireComponent(typeof(NpcNeeds), typeof(NpcStateMachine), typeof(SpriteRenderer))]
    public sealed class CasualTourist : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.2f;
        [SerializeField] private Sprite standingSprite;
        [SerializeField] private Sprite[] walkingSprites = Array.Empty<Sprite>();
        [SerializeField] private Sprite seatedSprite;
        [SerializeField, Min(0.01f)] private float walkFrameSeconds = 0.18f;

        private NpcNeeds _needs;
        private NpcStateMachine _state;
        private SpriteRenderer _renderer;
        private IReadOnlyList<Building> _buildings;
        private Building _target;
        private float _serviceProgress;
        private float _walkTime;
        private bool _wavePaused;
        private bool _applicationPaused;
        private bool _visiting;

        public NpcNeeds Needs => _needs;
        public NpcStateMachine StateMachine => _state;
        public Building Target => _target;
        public float ServiceProgress => _serviceProgress;
        public bool IsPaused => _wavePaused || _applicationPaused;

        private void Awake()
        {
            _needs = GetComponent<NpcNeeds>();
            _state = GetComponent<NpcStateMachine>();
            _renderer = GetComponent<SpriteRenderer>();
            ShowStanding();
        }

        /// <summary>Start at the ship's landing point using the needs configured on this object.</summary>
        public void BeginVisit(CruiseShip ship, IReadOnlyList<Building> buildings)
        {
            if (ship == null) throw new ArgumentNullException(nameof(ship));
            if (buildings == null) throw new ArgumentNullException(nameof(buildings));
            ReleaseBuilding();
            _buildings = buildings;
            transform.position = ship.SpawnPoint.position;
            _visiting = true;
            _state.SetState(NpcState.AwaitingService);
            ShowStanding();
        }

        /// <summary>Call with true in WAVE_PAUSED and false on resuming. Does not reset any state.</summary>
        public void SetWavePaused(bool paused) => _wavePaused = paused;

        private void OnApplicationPause(bool paused) => _applicationPaused = paused;

        private void Update()
        {
            if (!_visiting || IsPaused || Time.deltaTime <= 0f) return;

            float deltaTime = Time.deltaTime;
            _state.Tick(deltaTime);
            var need = _needs.Current;
            if (need == null)
            {
                _state.SetState(NpcState.Completed);
                ShowStanding();
                return;
            }

            if (_target == null || !_target.isActiveAndEnabled)
            {
                ReleaseBuilding();
                _state.SetState(NpcState.AwaitingService);
                ShowStanding();
                FindBuilding(need.Type);
                return;
            }

            switch (_state.Current)
            {
                case NpcState.WalkingToBuilding:
                    if (MoveTo(_target.EntrancePosition, deltaTime))
                    {
                        _target.JoinQueue(gameObject);
                        _state.SetState(NpcState.Waiting);
                        ShowStanding();
                    }
                    break;

                case NpcState.Waiting:
                    need.Wait(deltaTime);
                    if (MoveTo(_target.QueuePosition(gameObject), deltaTime) && _target.TryEnter(gameObject))
                    {
                        _state.SetState(NpcState.WalkingToSeat);
                    }
                    break;

                case NpcState.WalkingToSeat:
                    need.Wait(deltaTime);
                    if (MoveTo(_target.SeatPosition(gameObject), deltaTime))
                    {
                        _state.SetState(NpcState.UsingBuilding);
                        _renderer.sprite = seatedSprite;
                    }
                    break;

                case NpcState.UsingBuilding:
                    // Read the current rate every frame so staffing also affects seated guests.
                    _serviceProgress = Mathf.Min(1f, _serviceProgress + deltaTime / _target.ServiceDuration);
                    if (_serviceProgress >= 1f)
                    {
                        ReleaseBuilding();
                        _state.SetState(NpcState.AwaitingService);
                        ShowStanding();
                        _needs.CompleteCurrent();
                        if (_needs.Current == null) _state.SetState(NpcState.Completed);
                    }
                    break;
            }
        }

        private void FindBuilding(NeedType type)
        {
            if (_buildings == null) return;
            for (int i = 0; i < _buildings.Count; i++)
            {
                var building = _buildings[i];
                if (building == null || !building.isActiveAndEnabled || building.ServedNeed != type) continue;
                _target = building;
                _state.SetState(NpcState.WalkingToBuilding);
                return;
            }
            // Missing services leave the need pending; they are never silently skipped.
        }

        private bool MoveTo(Vector3 destination, float deltaTime)
        {
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(previous, destination, moveSpeed * deltaTime);
            Vector3 movement = transform.position - previous;
            if (movement.sqrMagnitude > 0f)
            {
                if (Mathf.Abs(movement.x) > 0.0001f) _renderer.flipX = movement.x < 0f;
                _walkTime += deltaTime;
                if (walkingSprites.Length > 0)
                    _renderer.sprite = walkingSprites[(int)(_walkTime / walkFrameSeconds) % walkingSprites.Length];
            }
            else
            {
                ShowStanding();
            }

            return (transform.position - destination).sqrMagnitude < 0.000001f;
        }

        private void ShowStanding()
        {
            _walkTime = 0f;
            _renderer.sprite = standingSprite;
        }

        private void ReleaseBuilding()
        {
            if (_target != null) _target.Leave(gameObject);
            _target = null;
            _serviceProgress = 0f;
        }

        private void OnDisable()
        {
            ReleaseBuilding();
            if (_state != null) _state.SetState(NpcState.AwaitingService);
        }
    }
}
