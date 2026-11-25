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
    /// A floating badge marks an unstaffed building with at least three guests waiting.
    /// </summary>
    public class Building : MonoBehaviour, IPointerClickHandler
    {
        public const int MaxLevel = 3;

        /// <summary>worker_slots: workers one building takes, the same for every building in the prototype.</summary>
        public const int WorkerSlots = 1;

        internal const int QueueWarningThreshold = 3;

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
        [Tooltip("service_time: seconds to serve one guest with a worker.")]
        [SerializeField, Min(0.01f)] private float serviceTime = 12f;
        [Tooltip("utilization_empty: share of full speed without a worker. Never 0, or a wave could not end (WR2).")]
        [SerializeField, Range(0.01f, 1f)] private float emptyUtilization = 0.33f;
        [Tooltip("Worker standing at the building; hidden while nobody works here.")]
        [SerializeField] private SpriteRenderer workerFigure;
        [SerializeField] private Vector2 queueStep = new(-0.3f, -0.2f);
        [Tooltip("Guests shown standing in the queue. Guests behind them keep waiting, out of sight, and a +N label counts them.")]
        [SerializeField, Min(1)] private int visibleQueue = 4;

        [Header("Visitor pivots")]
        [Tooltip("Where visitors show while using the building, one per slot; each pivot marks the visitor's feet. Visitors beyond the pivot count stay inside, hidden.")]
        [SerializeField] private Transform[] pivots = Array.Empty<Transform>();

        [Tooltip("Visitors stand on the pivots instead of sitting.")]
        [SerializeField] private bool standingOnly;

        [Header("Boost")]
        [SerializeField] private Color boostTint = new(1f, 0.92f, 0.55f, 1f);

        private readonly List<GameObject> _visitors = new();
        private SpriteRenderer _renderer;
        private Color _baseColor = Color.white;
        private float _boostTimeLeft;
        private float _boostServiceMultiplier = 1f;
        private readonly List<GameObject> _queue = new();
        private int _visitorCount;
        private bool _hasWorker;
        private SlowServiceIndicator _indicator;
        private QueueOverflowLabel _overflowLabel;

        internal event Action<Building> QueueWarningStarted;
        internal bool NeedsQueueWarning { get; private set; }
        // Distinguishes a new backlog from a previous warning still on screen or waiting to be shown.
        internal int QueueEpisode { get; private set; }

        public event Action<Building> VisitorsChanged;

        public event Action<Building> LevelChanged;

        /// <summary>Raised when the player taps the building.</summary>
        public event Action<Building> Clicked;

        /// <summary>Raised when a boost starts or runs out.</summary>
        public event Action<Building> BoostChanged;

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

        /// <summary>utilization: 1 with a worker, <c>utilization_empty</c> without.</summary>
        public float Utilization => _hasWorker ? 1f : emptyUtilization;

        /// <summary>True while an Energy boost speeds up the service.</summary>
        public bool IsBoosted => _boostTimeLeft > 0f;

        /// <summary>Seconds left of the running boost, 0 without one.</summary>
        public float BoostTimeLeft => _boostTimeLeft;

        /// <summary>
        /// Seconds to serve one guest at the current <see cref="Utilization"/> and boost.
        /// Read every frame so changes also affect guests already seated.
        /// </summary>
        public float ServiceDuration => Mathf.Max(0.01f, serviceTime * _boostServiceMultiplier / Utilization);

        /// <summary>True while a worker works here; see <see cref="WorkerSlots"/>.</summary>
        public bool HasWorker => _hasWorker;

        public int QueueCount => _queue.Count;

        /// <summary>Guests shown standing in the queue; the rest wait out of sight (SCRUM-178).</summary>
        public int VisibleQueue => visibleQueue;

        /// <summary>Waiting guests beyond <see cref="VisibleQueue"/>, counted by the +N label.</summary>
        public int HiddenQueueCount => HiddenInQueue(_queue.Count, visibleQueue);

        /// <summary>Guests of a queue of <paramref name="queued"/> that do not fit into <paramref name="visible"/> slots.</summary>
        public static int HiddenInQueue(int queued, int visible) => Mathf.Max(0, queued - Mathf.Max(1, visible));

        /// <summary>
        /// True when <paramref name="visitor"/> waits in one of the visible queue slots. The limit is
        /// only visual: hidden guests keep their place and move up as the queue advances.
        /// </summary>
        public bool IsShownInQueue(GameObject visitor)
        {
            int index = _queue.IndexOf(visitor);
            return index >= 0 && index < visibleQueue;
        }

        public int VisitorCount => _visitorCount;

        public bool IsFull => _visitorCount >= Capacity;

        public bool HasVisitor(GameObject visitor) => visitor != null && _visitors.Contains(visitor);

        private void Awake() => ApplyWorkerFigure();

        public void JoinQueue(GameObject visitor)
        {
            if (visitor == null || _queue.Contains(visitor) || _visitors.Contains(visitor)) return;
            _queue.Add(visitor);
            RefreshQueueWarning();
        }

        // Hidden guests wait on the last visible slot, so a long queue never crosses the island.
        public Vector3 QueuePosition(GameObject visitor)
        {
            int index = Mathf.Clamp(_queue.IndexOf(visitor), 0, visibleQueue - 1);
            return QueueSlot(index);
        }

        private Vector3 QueueSlot(int index) => EntrancePosition + (Vector3)(queueStep * index);

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
            if (_queue.Count > 0)
            {
                if (_queue[0] != visitor) return false;
                RemoveQueuedAt(0);
            }

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
            int queued = _queue.IndexOf(visitor);
            if (queued >= 0)
            {
                RemoveQueuedAt(queued);
                return;
            }

            int slot = _visitors.IndexOf(visitor);
            if (slot < 0) return;
            // Keep seat indices stable when a different guest finishes first.
            _visitors[slot] = null;
            _visitorCount--;

            OnVisitorLeft(visitor);
            VisitorsChanged?.Invoke(this);
        }

        private void RemoveQueuedAt(int index)
        {
            _queue.RemoveAt(index);
            RefreshQueueWarning();
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
            RefreshQueueWarning();
        }

        private void ApplyWorkerFigure()
        {
            if (workerFigure != null) workerFigure.enabled = _hasWorker;
        }

        // "+N" right behind the last visible guest while more guests wait than are shown.
        private void RefreshOverflowLabel()
        {
            int hidden = HiddenQueueCount;
            if (hidden <= 0)
            {
                if (_overflowLabel != null) _overflowLabel.gameObject.SetActive(false);
                return;
            }

            if (_overflowLabel == null) _overflowLabel = QueueOverflowLabel.Create(this);
            _overflowLabel.gameObject.SetActive(true);
            _overflowLabel.Show(hidden, QueueSlot(visibleQueue));
        }

        private void RefreshQueueWarning()
        {
            RefreshOverflowLabel();
            bool needed = !HasWorker && QueueCount >= QueueWarningThreshold;
            if (needed == NeedsQueueWarning) return;
            NeedsQueueWarning = needed;

            if (_indicator != null) _indicator.gameObject.SetActive(needed);
            else if (needed) _indicator = SlowServiceIndicator.Create(this);

            if (needed)
            {
                QueueEpisode++;
                QueueWarningStarted?.Invoke(this);
            }
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
