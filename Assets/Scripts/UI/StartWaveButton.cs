using ResortEmpire.World;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// The <c>Start Wave</c> call to action of the island view. It exists only in AWAITING_WAVE:
    /// during a wave the button is hidden instead of being shown disabled.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class StartWaveButton : MonoBehaviour
    {
        [SerializeField] private WaveManager waveManager;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (waveManager == null)
            {
                Debug.LogError($"{nameof(StartWaveButton)} has no {nameof(WaveManager)} assigned.", this);
                return;
            }

            _button.onClick.AddListener(StartWave);
            waveManager.StateChanged += OnWaveStateChanged;
            gameObject.SetActive(waveManager.State == WaveState.AwaitingWave);
        }

        private void OnDestroy()
        {
            if (waveManager == null) return;
            _button.onClick.RemoveListener(StartWave);
            waveManager.StateChanged -= OnWaveStateChanged;
        }

        private void StartWave() => waveManager.StartWave();

        private void OnWaveStateChanged(WaveState previous, WaveState next)
        {
            gameObject.SetActive(next == WaveState.AwaitingWave);
        }
    }
}
