using System;
using UnityEngine;

namespace ResortEmpire.Audio
{
    /// <summary>
    /// Player volume preferences in the 0–1 range, persisted in <see cref="PlayerPrefs"/>.
    /// Both default to full volume until the player changes them.
    /// </summary>
    public static class VolumeSettings
    {
        private const string SoundVolumeKey = "SoundVolume";
        private const string MusicVolumeKey = "MusicVolume";

        /// <summary>Sound effects volume.</summary>
        public static float SoundVolume
        {
            get => PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(SoundVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>Raised after <see cref="MusicVolume"/> changes, so playing music can follow it live.</summary>
        public static event Action MusicVolumeChanged;

        /// <summary>Background music volume.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                MusicVolumeChanged?.Invoke();
            }
        }
    }
}
