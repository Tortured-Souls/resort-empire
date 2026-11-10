using ResortEmpire.World;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// One of the player's choices for the docked cruise ship: <c>Start Wave</c> lets its guests
    /// disembark, <c>Reject</c> sends it away so the next one sails in. The button exists only in
    /// AWAITING_WAVE: otherwise it is hidden instead of being shown disabled.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ShipDecisionButton : MonoBehaviour
    {
        public enum Decision
        {
            StartWave,
            Reject
        }

        [SerializeField] private WaveManager waveManager;
        [SerializeField] private Decision decision;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (waveManager == null)
            {
                Debug.LogError($"{nameof(ShipDecisionButton)} has no {nameof(WaveManager)} assigned.", this);
                return;
            }

            _button.onClick.AddListener(Decide);
            waveManager.StateChanged += OnWaveStateChanged;
            gameObject.SetActive(waveManager.State == WaveState.AwaitingWave);
        }

        private void OnDestroy()
        {
            if (waveManager == null) return;
            _button.onClick.RemoveListener(Decide);
            waveManager.StateChanged -= OnWaveStateChanged;
        }

        private void Decide()
        {
            if (decision == Decision.StartWave) waveManager.StartWave();
            else waveManager.RejectShip();
        }

        private void OnWaveStateChanged(WaveState previous, WaveState next)
        {
            gameObject.SetActive(next == WaveState.AwaitingWave);
        }
    }
}
