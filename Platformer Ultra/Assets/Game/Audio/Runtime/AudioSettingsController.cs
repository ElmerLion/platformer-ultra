using UnityEngine;
using UnityEngine.Audio;

namespace PlatformerUltra.Audio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class AudioSettingsController : MonoBehaviour
    {
        public const string MasterParameter = "MasterVolume";
        public const string MusicParameter = "MusicVolume";
        public const string SfxParameter = "SfxVolume";
        public const string MasterPreferenceKey = "PlatformerUltra.Audio.Master";
        public const string MusicPreferenceKey = "PlatformerUltra.Audio.Music";
        public const string SfxPreferenceKey = "PlatformerUltra.Audio.Sfx";

        [SerializeField] private AudioMixer _mixer;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        private void Awake()
        {
            LoadAndApply();
        }

        private void OnEnable()
        {
            ApplyAll();
        }

        public void Configure(AudioMixer mixer)
        {
            _mixer = mixer;
            LoadAndApply();
        }

        public void LoadAndApply()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterPreferenceKey, 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPreferenceKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxPreferenceKey, 1f));
            ApplyAll();
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            MasterVolume = SetVolume(MasterPreferenceKey, MasterParameter, normalizedVolume);
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            MusicVolume = SetVolume(MusicPreferenceKey, MusicParameter, normalizedVolume);
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            SfxVolume = SetVolume(SfxPreferenceKey, SfxParameter, normalizedVolume);
        }

        public static float LinearToDecibels(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            return clamped <= 0.0001f ? -80f : Mathf.Clamp(20f * Mathf.Log10(clamped), -80f, 0f);
        }

        private float SetVolume(string preferenceKey, string parameterName, float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(preferenceKey, clamped);
            PlayerPrefs.Save();
            Apply(parameterName, clamped);
            return clamped;
        }

        private void ApplyAll()
        {
            Apply(MasterParameter, MasterVolume);
            Apply(MusicParameter, MusicVolume);
            Apply(SfxParameter, SfxVolume);
        }

        private void Apply(string parameterName, float normalizedVolume)
        {
            _mixer?.SetFloat(parameterName, LinearToDecibels(normalizedVolume));
        }
    }
}
