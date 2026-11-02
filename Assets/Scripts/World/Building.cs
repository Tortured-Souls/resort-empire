using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using UnityEngine;

namespace ResortEmpire.World
{
    public class Building : MonoBehaviour
    {
        [SerializeField] private string displayName = "Building";
        [SerializeField] private Transform entrance;
        [SerializeField, Min(1)] private int capacity = 1;
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

        public string DisplayName => displayName;

        public Vector3 EntrancePosition => entrance != null ? entrance.position : transform.position;

        public int Capacity => capacity;

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

        public bool IsFull => _visitorCount >= capacity;

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
            return EntrancePosition + new Vector3((index - (capacity - 1) * 0.5f) * seatSpacing, 0.35f, 0f);
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

        protected virtual void OnVisitorEntered(GameObject visitor) { }

        protected virtual void OnVisitorLeft(GameObject visitor) { }
    }
}
