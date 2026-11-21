using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using UnityEngine;

namespace ResortEmpire.World
{
    public class Building : MonoBehaviour
    {
        public const int MaxLevel = 3;

        [SerializeField] private string displayName = "Building";
        [SerializeField] private Transform entrance;
        [SerializeField, Min(1)] private int capacity = 1;
        [SerializeField, Range(1, MaxLevel)] private int level = 1;

        // Capacity for levels 1..MaxLevel; when empty, capacity is used for every level.
        [SerializeField] private int[] capacityPerLevel = Array.Empty<int>();

        [SerializeField] private NeedType servedNeed = NeedType.Rooms;
        [SerializeField, Min(0.01f)] private float serviceTime = 36f;
        [SerializeField, Range(0.01f, 1f)] private float workerUtilization = 0.33f;
        [SerializeField] private bool hasWorker;
        [SerializeField] private Transform[] seats = Array.Empty<Transform>();
        [SerializeField] private Vector2 queueStep = new(-0.3f, -0.2f);
        [SerializeField, Min(0.01f)] private float seatSpacing = 0.35f;

        [Header("Boost")]
        [SerializeField] private Color boostTint = new(1f, 0.92f, 0.55f, 1f);

        private readonly List<GameObject> _visitors = new();
        private SpriteRenderer _renderer;
        private Color _baseColor = Color.white;
        private float _boostTimeLeft;
        private float _boostServiceMultiplier = 1f;
        private readonly List<GameObject> _queue = new();
        private int _visitorCount;

        public event Action<Building> VisitorsChanged;

        public event Action<Building> LevelChanged;

        /// <summary>Raised when a boost starts or runs out.</summary>
        public event Action<Building> BoostChanged;

        public string DisplayName => displayName;

        public Vector3 EntrancePosition => entrance != null ? entrance.position : transform.position;

        public int Level => level;

        public bool CanUpgrade => level < MaxLevel;

        public int Capacity => level <= capacityPerLevel.Length ? Mathf.Max(1, capacityPerLevel[level - 1]) : capacity;

        public NeedType ServedNeed => servedNeed;

        /// <summary>Unstaffed seconds per request. Staffing changes the rate, not progress.</summary>
        public float ServiceDuration => Mathf.Max(0.01f, serviceTime * _boostServiceMultiplier * (hasWorker ? workerUtilization : 1f));

        /// <summary>True while an Energy boost speeds up the service.</summary>
        public bool IsBoosted => _boostTimeLeft > 0f;

        /// <summary>Seconds left of the running boost, 0 without one.</summary>
        public float BoostTimeLeft => _boostTimeLeft;

        public bool HasWorker
        {
            get => hasWorker;
            set => hasWorker = value;
        }

        public int QueueCount => _queue.Count;

        public int VisitorCount => _visitorCount;

        public bool IsFull => _visitorCount >= Capacity;

        public bool HasVisitor(GameObject visitor) => visitor != null && _visitors.Contains(visitor);

        public void JoinQueue(GameObject visitor)
        {
            if (visitor == null || _queue.Contains(visitor) || _visitors.Contains(visitor)) return;
            _queue.Add(visitor);
        }

        public Vector3 QueuePosition(GameObject visitor)
        {
            int index = Mathf.Max(0, _queue.IndexOf(visitor));
            return EntrancePosition + (Vector3)(queueStep * index);
        }

        public Vector3 SeatPosition(GameObject visitor)
        {
            int index = _visitors.IndexOf(visitor);
            if (index < 0) return EntrancePosition;
            if (index < seats.Length && seats[index] != null) return seats[index].position;
            return EntrancePosition + new Vector3((index - (Capacity - 1) * 0.5f) * seatSpacing, 0.35f, 0f);
        }

        public bool TryEnter(GameObject visitor)
        {
            if (visitor == null || IsFull || _visitors.Contains(visitor)) return false;
            if (_queue.Count > 0 && _queue[0] != visitor) return false;

            _queue.Remove(visitor);
            int slot = _visitors.IndexOf(null);
            if (slot >= 0) _visitors[slot] = visitor;
            else _visitors.Add(visitor);
            _visitorCount++;
            OnVisitorEntered(visitor);
            VisitorsChanged?.Invoke(this);
            return true;
        }

        public void Leave(GameObject visitor)
        {
            if (visitor == null) return;
            _queue.Remove(visitor);
            int slot = _visitors.IndexOf(visitor);
            if (slot < 0) return;
            // Keep seat indices stable when a different guest finishes first.
            _visitors[slot] = null;
            _visitorCount--;

            OnVisitorLeft(visitor);
            VisitorsChanged?.Invoke(this);
        }

        // Raises the level by one; paying for the upgrade is up to the caller.
        public bool TryUpgrade()
        {
            if (!CanUpgrade) return false;

            level++;
            LevelChanged?.Invoke(this);
            VisitorsChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Speeds up the service for <paramref name="seconds"/>: service time is multiplied by
        /// <paramref name="serviceMultiplier"/> (0.5 = twice as fast). Paying is up to the caller.
        /// </summary>
        public bool StartBoost(float seconds, float serviceMultiplier)
        {
            if (IsBoosted || seconds <= 0f) return false;

            _boostTimeLeft = seconds;
            _boostServiceMultiplier = Mathf.Clamp(serviceMultiplier, 0.1f, 1f);
            if (Renderer != null)
            {
                _baseColor = Renderer.color;
                Renderer.color = _baseColor * boostTint;
            }

            BoostChanged?.Invoke(this);
            return true;
        }

        protected virtual void Update()
        {
            if (!IsBoosted) return;

            _boostTimeLeft -= Time.deltaTime;
            if (_boostTimeLeft > 0f) return;

            _boostTimeLeft = 0f;
            _boostServiceMultiplier = 1f;
            if (Renderer != null) Renderer.color = _baseColor;
            BoostChanged?.Invoke(this);
        }

        private SpriteRenderer Renderer => _renderer != null ? _renderer : _renderer = GetComponent<SpriteRenderer>();

        protected virtual void OnVisitorEntered(GameObject visitor) { }

        protected virtual void OnVisitorLeft(GameObject visitor) { }
    }
}
