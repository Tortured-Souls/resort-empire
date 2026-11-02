using TMPro;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>Shows the build version (Player Settings → Version) on its text.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class VersionLabel : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<TMP_Text>().text = $"v{Application.version}";
        }
    }
}
