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

        [Header("Upgrade")]
        [Tooltip("Coins for the next level: [0] is level 1 to 2, [1] is level 2 to 3.")]
        [SerializeField] private int[] upgradePrices = { 300, 600 };

        [Tooltip("Service time multiplier for levels 1..MaxLevel, lower is faster. Empty means 1 for every level.")]
        [SerializeField] private float[] serviceTimePerLevel = { 1f, 0.85f, 0.7f };

        [SerializeField] private NeedType servedNeed = NeedType.Rooms;
        [SerializeField, Min(0.01f)] private float serviceTime = 36f;
        [SerializeField, Range(0.01f, 1f)] private float workerUtilization = 0.33f;
        [SerializeField] private bool hasWorker;
        [SerializeField] private Transform[] seats = Array.Empty<Transform>();
        [SerializeField] private Vector2 queueStep = new(-0.3f, -0.2f);
        [SerializeField, Min(0.01f)] private float seatSpacing = 0.35f;

        private readonly List<GameObject> _visitors = new();
        private readonly List<GameObject> _queue = new();
        private int _visitorCount;

        public event Action<Building> VisitorsChanged;

        public event Action<Building> LevelChanged;

        public string DisplayName => displayName;

        public Vector3 EntrancePosition => entrance != null ? entrance.position : transform.position;

        public int Level => level;

        public bool CanUpgrade => level < MaxLevel;

        public int Capacity => CapacityAt(level);

        /// <summary>Capacity after the next upgrade; the current one at max level.</summary>
        public int NextCapacity => CapacityAt(CanUpgrade ? level + 1 : level);

        /// <summary>Coins needed for the next level, 0 at max level.</summary>
        public int UpgradePrice => CanUpgrade && level - 1 < upgradePrices.Length ? Mathf.Max(0, upgradePrices[level - 1]) : 0;

        /// <summary>Service speed multiplier of the current level, lower is faster.</summary>
        public float ServiceTimeMultiplier => ServiceTimeAt(level);

        /// <summary>Service speed multiplier after the next upgrade.</summary>
        public float NextServiceTimeMultiplier => ServiceTimeAt(CanUpgrade ? level + 1 : level);

        public NeedType ServedNeed => servedNeed;

        /// <summary>Unstaffed seconds per request. Staffing changes the rate, not progress.</summary>
        public float ServiceDuration => Mathf.Max(0.01f, serviceTime * ServiceTimeMultiplier * (hasWorker ? workerUtilization : 1f));

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

        private int CapacityAt(int lvl) => lvl <= capacityPerLevel.Length ? Mathf.Max(1, capacityPerLevel[lvl - 1]) : capacity;

        private float ServiceTimeAt(int lvl) => lvl <= serviceTimePerLevel.Length ? Mathf.Clamp(serviceTimePerLevel[lvl - 1], 0.1f, 1f) : 1f;

        protected virtual void OnVisitorEntered(GameObject visitor) { }

        protected virtual void OnVisitorLeft(GameObject visitor) { }
    }
}
