using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResortEmpire.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameWorldScene = "GameWorld";
        [SerializeField] private string settingsScene = "Settings";

        /// <summary>Loads the game world scene, continuing the saved game when there is one.</summary>
        public void PlayGame()
        {
            SceneManager.LoadScene(gameWorldScene);
        }

        /// <summary>Loads the settings scene.</summary>
        public void OpenSettings()
        {
            SceneManager.LoadScene(settingsScene);
        }

        /// <summary>Quits the player, or stops Play Mode in the Unity Editor.</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
