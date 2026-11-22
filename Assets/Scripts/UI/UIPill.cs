using TMPro;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>Icon and a short value from the shared UI kit (Prefabs/UI/Pill), e.g. seats or workers of a building.</summary>
    public sealed class UIPill : MonoBehaviour
    {
        [SerializeField] private TMP_Text value;

        public void SetValue(string text) => value.text = text;
    }
}
