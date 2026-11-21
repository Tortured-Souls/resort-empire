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
        [SerializeField] private int[] upgradePrices = { 500, 1200 };

        [SerializeField] private NeedType servedNeed = NeedType.Rooms;
        [SerializeField, Min(0.01f)] private float serviceTime = 36f;
        [SerializeField, Range(0.01f, 1f)] private float workerUtilization = 0.33f;
        [SerializeField] private bool hasWorker;
        [SerializeField] private Vector2 queueStep = new(-0.3f, -0.2f);

        [Header("Visitor pivots")]
        [Tooltip("Where visitors show while using the building, one per slot; each pivot marks the visitor's feet. Visitors beyond the pivot count stay inside, hidden.")]
        [SerializeField] private Transform[] pivots = Array.Empty<Transform>();

        [Tooltip("Visitors stand on the pivots instead of sitting.")]
        [SerializeField] private bool standingOnly;

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
        /// <summary>Capacity of this building at <paramref name="buildingLevel"/> (1..<see cref="MaxLevel"/>).</summary>
        public int CapacityAt(int buildingLevel) =>
            buildingLevel <= capacityPerLevel.Length ? Mathf.Max(1, capacityPerLevel[buildingLevel - 1]) : capacity;

        /// <summary>True when visitors stand on the pivots instead of sitting.</summary>
        public bool StandingOnly => standingOnly;

        /// <summary>Visitor pivots by slot; null entries are slots without a pivot.</summary>
        public IReadOnlyList<Transform> Pivots => pivots;

        public NeedType ServedNeed => servedNeed;

        /// <summary>Unstaffed seconds per request. Staffing changes the rate, not progress.</summary>
        public float ServiceDuration => Mathf.Max(0.01f, serviceTime * (hasWorker ? workerUtilization : 1f));

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

        /// <summary>
        /// Pivot of the slot <paramref name="visitor"/> occupies. False when the slot has no pivot: the
        /// visitor stays inside, out of sight, and <paramref name="position"/> is the entrance.
        /// </summary>
        public bool TryGetPivot(GameObject visitor, out Vector3 position)
        {
            int slot = _visitors.IndexOf(visitor);
            if (slot >= 0 && slot < pivots.Length && pivots[slot] != null)
            {
                position = pivots[slot].position;
                return true;
            }

            position = EntrancePosition;
            return false;
        }

        public bool TryEnter(GameObject visitor)
        {
            if (visitor == null || IsFull || _visitors.Contains(visitor)) return false;
            if (_queue.Count > 0 && _queue[0] != visitor) return false;

            _queue.Remove(visitor);
            // Lowest free slot first, so visible pivots fill up before guests go inside unseen.
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

        protected virtual void OnVisitorEntered(GameObject visitor) { }

        protected virtual void OnVisitorLeft(GameObject visitor) { }
    }
}
