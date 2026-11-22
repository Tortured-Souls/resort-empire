using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>Small colored label from the shared UI kit (Prefabs/UI/Tag), e.g. "Available" or "No worker".</summary>
    public sealed class UITag : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        /// <summary>Shows <paramref name="text"/> on a <paramref name="color"/> background; see <see cref="UIPalette"/>.</summary>
        public void Set(string text, Color color)
        {
            label.text = text;
            background.color = color;
        }
    }
}
