using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>A non-interactive warning sprite centered above a building's roof.</summary>
    public sealed class SlowServiceIndicator : MonoBehaviour
    {
        private const float RoofGap = 0.12f;
        private const float BobAmplitude = 0.05f;
        private static Sprite _badgeSprite;
        private Vector3 _restPosition;

        public static SlowServiceIndicator Create(Building building)
        {
            if (_badgeSprite == null) _badgeSprite = Resources.Load<Sprite>("SlowServiceBadge");

            var badge = new GameObject("Slow Service Indicator");
            badge.transform.SetParent(building.transform, false);
            var renderer = badge.AddComponent<SpriteRenderer>();
            renderer.sprite = _badgeSprite;
            renderer.sortingOrder = short.MaxValue;

            Bounds bodyBounds = default;
            if (building.TryGetComponent(out SpriteRenderer body) && body.sprite != null)
            {
                bodyBounds = body.localBounds;
                renderer.sortingLayerID = body.sortingLayerID;
            }
            else if (building.TryGetComponent(out BoxCollider2D footprint))
            {
                bodyBounds = new Bounds(footprint.offset, footprint.size);
            }

            var indicator = badge.AddComponent<SlowServiceIndicator>();
            indicator._restPosition = new Vector3(bodyBounds.center.x,
                bodyBounds.max.y + RoofGap + renderer.localBounds.extents.y, 0f);
            badge.transform.localPosition = indicator._restPosition;
            return indicator;
        }

        private void Update()
        {
            float bob = BobAmplitude * Mathf.Sin(Time.time * 3f);
            transform.localPosition = _restPosition + Vector3.up * bob;
        }
    }
}
