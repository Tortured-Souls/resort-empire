using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Slowly "breathes" a sprite: scales it around its pivot and fades its alpha on the same wave,
    /// e.g. the foam ring around the island that looks like waves washing up the beach.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnvironmentPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.1f)] private float scaleAmount = 0.015f;
        [SerializeField, Min(0.01f)] private float period = 4f;
        [Tooltip("Alpha multiplier at the low and high point of the wave.")]
        [SerializeField] private Vector2 alphaRange = new(0.6f, 1f);
        [Tooltip("Offset in seconds, so several pulsing objects do not move in sync.")]
        [SerializeField] private float phase;

        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private Color _baseColor;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _baseColor = _renderer.color;
        }

        private void Update()
        {
            float wave = Mathf.Sin((Time.time + phase) / period * Mathf.PI * 2f) * 0.5f + 0.5f;
            transform.localScale = _baseScale * (1f + wave * scaleAmount);

            Color color = _baseColor;
            color.a *= Mathf.Lerp(alphaRange.x, alphaRange.y, wave);
            _renderer.color = color;
        }
    }
}
