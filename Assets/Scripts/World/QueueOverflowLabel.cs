using TMPro;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// "+N" behind the last visible guest of a building's queue: N more guests wait out of sight
    /// (SCRUM-178). Non-interactive, created by the building when its queue first overflows.
    /// </summary>
    public sealed class QueueOverflowLabel : MonoBehaviour
    {
        private const float FontSize = 3f;
        private static readonly Color TextColor = Color.white;
        private static readonly Color OutlineColor = new(0.25f, 0.13f, 0.05f, 1f);

        private TextMeshPro _text;
        private int _shown = -1;

        public static QueueOverflowLabel Create(Building building)
        {
            var go = new GameObject("Queue Overflow Label");
            go.transform.SetParent(building.transform, false);

            var text = go.AddComponent<TextMeshPro>();
            text.fontSize = FontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = TextColor;
            text.outlineColor = OutlineColor;
            text.outlineWidth = 0.3f;
            text.rectTransform.sizeDelta = new Vector2(2f, 1f);

            // Same layer as the building and on top of the guests.
            var renderer = text.GetComponent<MeshRenderer>();
            if (building.TryGetComponent(out SpriteRenderer body)) renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = short.MaxValue;

            var label = go.AddComponent<QueueOverflowLabel>();
            label._text = text;
            return label;
        }

        /// <summary>Shows "+<paramref name="hidden"/>" at <paramref name="worldPosition"/>.</summary>
        public void Show(int hidden, Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (hidden == _shown) return;
            _shown = hidden;
            _text.text = Text(hidden);
        }

        /// <summary>Label text for <paramref name="hidden"/> guests out of sight.</summary>
        public static string Text(int hidden) => $"+{Mathf.Max(0, hidden)}";
    }
}
