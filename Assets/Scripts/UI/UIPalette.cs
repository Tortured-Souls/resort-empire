using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Colors of the shared UI kit in <c>Prefabs/UI</c>. The prefabs carry their own colors;
    /// use these for states that change at runtime, e.g. the color of a <see cref="UITag"/>.
    /// </summary>
    public static class UIPalette
    {
        /// <summary>Text on light surfaces.</summary>
        public static readonly Color Text = new Color32(0x4A, 0x25, 0x11, 0xFF);

        /// <summary>Good state: staffed, available, done.</summary>
        public static readonly Color Positive = new Color32(0x14, 0xA6, 0x72, 0xFF);

        /// <summary>Needs attention without being an error: empty, slow.</summary>
        public static readonly Color Warning = new Color32(0xF2, 0x71, 0x1F, 0xFF);

        /// <summary>Not available right now: busy, locked.</summary>
        public static readonly Color Neutral = new Color32(0xA3, 0x9A, 0x92, 0xFF);
    }
}
