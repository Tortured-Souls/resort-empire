using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResortEmpire.Audio
{
    /// <summary>
    /// Plays one looping background track through a shared <see cref="AudioSource"/> that
    /// survives scene loads, fading out and in when the track changes. Scenes ask for music
    /// with a <see cref="SceneMusic"/>; loading a scene without one fades the music out.
    /// Follows <see cref="VolumeSettings.MusicVolume"/> live.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private const float FadeSeconds = 0.8f;

        private static MusicPlayer _instance;

        private AudioSource _source;
        private AudioClip _target; // Track we are heading to; null means silence.
        private float _fade;       // 0 = silent, 1 = full music volume.

        /// <summary>Starts <paramref name="clip"/>; keeps playing without a restart if it is already the current track.</summary>
        public static void Play(AudioClip clip)
        {
            if (_instance == null)
            {
                var go = new GameObject("Music");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MusicPlayer>();
            }

            _instance.SetTarget(clip);
        }

        /// <summary>Fades the current track out.</summary>
        public static void Stop()
        {
            if (_instance != null) _instance.SetTarget(null);
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            VolumeSettings.MusicVolumeChanged += ApplyVolume;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            VolumeSettings.MusicVolumeChanged -= ApplyVolume;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_instance == this) _instance = null;
        }

        private void SetTarget(AudioClip clip)
        {
            _target = clip;
            enabled = true;
        }

        // Runs only while fading; disables itself once the music has settled.
        private void Update()
        {
            float step = Time.unscaledDeltaTime / FadeSeconds;
            if (_source.clip != _target)
            {
                _fade = Mathf.MoveTowards(_fade, 0f, step);
                if (_fade == 0f) SwapClip();
            }
            else
            {
                // A clip whose audio data is not loaded yet can ignore Play; keep asking until it runs.
                if (_target != null && !_source.isPlaying) _source.Play();
                float goal = _target != null ? 1f : 0f;
                _fade = Mathf.MoveTowards(_fade, goal, step);
                if (_fade == goal && (_target == null || _source.isPlaying)) enabled = false;
            }

            ApplyVolume();
        }

        private void SwapClip()
        {
            _source.Stop();
            _source.clip = _target;
            if (_target != null) _source.Play();
        }

        private void ApplyVolume() => _source.volume = VolumeSettings.MusicVolume * _fade;

        // Scene objects are already enabled here, so a SceneMusic in the new scene has made its request.
        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<SceneMusic>(true) != null) return;
            }

            Stop();
        }
    }
}
