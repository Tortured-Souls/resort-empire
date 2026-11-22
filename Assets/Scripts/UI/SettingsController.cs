using ResortEmpire.Audio;
using ResortEmpire.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>Binds the settings screen's volume sliders to <see cref="VolumeSettings"/>.</summary>
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Slider soundVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private GameObject purgeConfirmDialog;

        private void Awake()
        {
            purgeConfirmDialog.SetActive(false);
            soundVolumeSlider.SetValueWithoutNotify(VolumeSettings.SoundVolume);
            musicVolumeSlider.SetValueWithoutNotify(VolumeSettings.MusicVolume);
            soundVolumeSlider.onValueChanged.AddListener(value => VolumeSettings.SoundVolume = value);
            musicVolumeSlider.onValueChanged.AddListener(value => VolumeSettings.MusicVolume = value);
        }

        /// <summary>Opens the dialog that asks the player to confirm deleting the saved game.</summary>
        public void RequestPurgeSavedProgress()
        {
            purgeConfirmDialog.SetActive(true);
        }

        /// <summary>Deletes the saved game, so the next Play starts a new resort.</summary>
        public void ConfirmPurgeSavedProgress()
        {
            SaveStorage.Delete(SaveStorage.DefaultPath);
            purgeConfirmDialog.SetActive(false);
        }

        /// <summary>Closes the confirmation dialog and keeps the saved game.</summary>
        public void CancelPurgeSavedProgress()
        {
            purgeConfirmDialog.SetActive(false);
        }

        /// <summary>Returns to the main menu scene.</summary>
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
