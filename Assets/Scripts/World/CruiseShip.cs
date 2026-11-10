using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// The cruise ship that brings each wave of guests to the island. Between visits it waits
    /// off-screen; <see cref="SailIn"/> brings it bow first to its dock (the position it has in
    /// the scene), where it waits for the player to start the wave or reject it, and
    /// <see cref="SailAway"/> backs it out the same way. NPCs are spawned at
    /// <see cref="SpawnPoint"/>, which moves with the ship.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CruiseShip : MonoBehaviour
    {
        public enum Phase
        {
            Away,
            SailingIn,
            Docked,
            SailingAway
        }

        [SerializeField] private Transform spawnPoint;
        [Tooltip("Where the ship waits between visits, relative to the dock. Point it straight back from the bow and keep it off-screen.")]
        [SerializeField] private Vector2 offshoreOffset = new(-10f, -5f);
        [SerializeField, Min(0.1f)] private float sailAwaySeconds = 3f;
        [SerializeField, Min(0f)] private float bobHeight = 0.03f;
        [SerializeField, Min(0.1f)] private float bobSeconds = 2.5f;

        private SpriteRenderer _renderer;
        private Vector3 _dockPosition;
        private Vector3 _position;
        private Vector3 _from;
        private Vector3 _to;
        private float _elapsed;
        private float _duration;
        private Phase _phase;

        /// <summary>Where new NPCs appear. Falls back to the ship itself when not set.</summary>
        public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;

        public Phase CurrentPhase => _phase;

        /// <summary>True while the ship lies at its dock and guests can disembark.</summary>
        public bool IsDocked => _phase == Phase.Docked;

        private Vector3 OffshorePosition => _dockPosition + (Vector3)offshoreOffset;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _dockPosition = transform.position;
            _position = OffshorePosition;
            _phase = Phase.Away;
            _renderer.enabled = false;
            transform.position = _position;
        }

        /// <summary>Sails from the current position to the dock in <paramref name="seconds"/>.</summary>
        public void SailIn(float seconds)
        {
            Sail(Phase.SailingIn, _dockPosition, seconds);
        }

        /// <summary>Sails back off-screen and hides the ship there.</summary>
        public void SailAway()
        {
            if (_phase is Phase.Away or Phase.SailingAway) return;
            Sail(Phase.SailingAway, OffshorePosition, sailAwaySeconds);
        }

        private void Sail(Phase phase, Vector3 destination, float seconds)
        {
            _phase = phase;
            _from = _position;
            _to = destination;
            _duration = Mathf.Max(0.01f, seconds);
            _elapsed = 0f;
            _renderer.enabled = true;
        }

        private void Update()
        {
            if (_phase == Phase.Away) return;

            if (_phase != Phase.Docked)
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / _duration);
                // Arrive by slowing down into the dock, leave by slowly picking up speed.
                float eased = _phase == Phase.SailingIn ? 1f - (1f - t) * (1f - t) * (1f - t) : t * t;
                _position = Vector3.LerpUnclamped(_from, _to, eased);

                if (t >= 1f)
                {
                    _phase = _phase == Phase.SailingIn ? Phase.Docked : Phase.Away;
                    _renderer.enabled = _phase != Phase.Away;
                }
            }

            float bob = Mathf.Sin(Time.time * (2f * Mathf.PI / bobSeconds)) * bobHeight;
            transform.position = _position + Vector3.up * bob;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SpawnPoint.position, 0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 dock = Application.isPlaying ? _dockPosition : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(dock, dock + (Vector3)offshoreOffset);
        }
    }
}
