using UnityEngine;

namespace ResortEmpire.Audio
{
    /// <summary>
    /// Declares the background music of its scene through <see cref="MusicPlayer"/>.
    /// Scenes that share a track (main menu and settings) keep it playing across loads.
    /// </summary>
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip music;

        private void OnEnable() => MusicPlayer.Play(music);
    }
}
