using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ResortEmpire.World
{
    /// <summary>
    /// A service building. It seats <see cref="Capacity"/> guests at once and serves each in
    /// <see cref="ServiceDuration"/>: full speed with a worker, slower without one (build
    /// document §4.4–4.5). Workers are assigned by <see cref="BuildingSystem"/>; a tap on the
    /// building raises <see cref="Clicked"/>, so it needs a <see cref="Collider2D"/>.
    /// </summary>
    public class Building : MonoBehaviour, IPointerClickHandler
    {
        public const int MaxLevel = 3;

        /// <summary>worker_slots: workers one building takes, the same for every building in the prototype.</summary>
        public const int WorkerSlots = 1;

        [SerializeField] private string displayName = "Building";
        [SerializeField] private Transform entrance;
        [SerializeField, Min(1)] private int capacity = 1;
        [SerializeField, Range(1, MaxLevel)] private int level = 1;

        // Capacity for levels 1..MaxLevel; when empty, capacity is used for every level.
        [SerializeField] private int[] capacityPerLevel = Array.Empty<int>();

        [SerializeField] private NeedType servedNeed = NeedType.Rooms;
        [Tooltip("service_time: seconds to serve one guest with a worker.")]
        [SerializeField, Min(0.01f)] private float serviceTime = 12f;
        [Tooltip("utilization_empty: share of full speed without a worker. Never 0, or a wave could not end (WR2).")]
        [SerializeField, Range(0.01f, 1f)] private float emptyUtilization = 0.33f;
        [Tooltip("Worker standing at the building; hidden while nobody works here.")]
        [SerializeField] private SpriteRenderer workerFigure;
        [SerializeField] private Transform[] seats = Array.Empty<Transform>();
        [SerializeField] private Vector2 queueStep = new(-0.3f, -0.2f);
        [SerializeField, Min(0.01f)] private float seatSpacing = 0.35f;

        private readonly List<GameObject> _visitors = new();
        private readonly List<GameObject> _queue = new();
        private int _visitorCount;
        private bool _hasWorker;

        public event Action<Building> VisitorsChanged;

        public event Action<Building> LevelChanged;

        /// <summary>Raised when the player taps the building.</summary>
        public event Action<Building> Clicked;

        public string DisplayName => displayName;

        public Vector3 EntrancePosition => entrance != null ? entrance.position : transform.position;

        public int Level => level;

        public bool CanUpgrade => level < MaxLevel;

        public int Capacity => level <= capacityPerLevel.Length ? Mathf.Max(1, capacityPerLevel[level - 1]) : capacity;

        public NeedType ServedNeed => servedNeed;

        /// <summary>utilization: 1 with a worker, <c>utilization_empty</c> without.</summary>
        public float Utilization => _hasWorker ? 1f : emptyUtilization;

        /// <summary>
        /// Seconds to serve one guest at the current <see cref="Utilization"/>. Read it every frame:
        /// a worker arriving or leaving changes the speed of guests already seated, too.
        /// </summary>
        public float ServiceDuration => serviceTime / Utilization;

        /// <summary>True while a worker works here; see <see cref="WorkerSlots"/>.</summary>
        public bool HasWorker => _hasWorker;

        public int QueueCount => _queue.Count;

        public int VisitorCount => _visitorCount;

        public bool IsFull => _visitorCount >= Capacity;

        public bool HasVisitor(GameObject visitor) => visitor != null && _visitors.Contains(visitor);

        private void Awake() => ApplyWorkerFigure();

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }

        // Only BuildingSystem staffs buildings; it keeps one worker fewer than buildings.
        internal void SetWorker(bool hasWorker)
        {
            _hasWorker = hasWorker;
            ApplyWorkerFigure();
        }

        private void ApplyWorkerFigure()
        {
            if (workerFigure != null) workerFigure.enabled = _hasWorker;
        }

        protected virtual void OnVisitorEntered(GameObject visitor) { }

        protected virtual void OnVisitorLeft(GameObject visitor) { }
    }
}
