using System;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryAIVoiceEmitter : MonoBehaviour
    {
        [Serializable]
        public struct VoiceLine
        {
            [TextArea(2, 4)] public string Caption;
            public AudioClip Clip;
        }

        [SerializeField] private AudioSource _primarySource;
        [SerializeField] private AudioSource _metallicSource;
        [SerializeField] private FactoryIntroPresenter _presenter;
        [SerializeField] private VoiceLine[] _lines = Array.Empty<VoiceLine>();
        [SerializeField] private AudioSource[] _duckedSources = Array.Empty<AudioSource>();
        [SerializeField, Range(0.5f, 1.5f)] private float _pitch = 0.93f;
        [SerializeField, Min(0f)] private float _metallicDelay = 0.012f;
        [SerializeField, Range(0f, 1f)] private float _duckMultiplier = 0.56f;
        [SerializeField, Min(0f)] private float _fallbackSubtitleDuration = 3f;

        private float[] _baseDuckedVolumes = Array.Empty<float>();
        private double _voiceEndsAt;
        private bool _voiceActive;

        public int LineCount => _lines != null ? _lines.Length : 0;
        public bool VoiceActive => _voiceActive;

        private void Awake()
        {
            CacheDuckedVolumes();
            ConfigureSources();
        }

        private void Update()
        {
            if (!_voiceActive)
            {
                return;
            }

            bool active = AudioSettings.dspTime < _voiceEndsAt;
            SetDucked(active);
            if (!active)
            {
                _voiceActive = false;
                _presenter?.HideSubtitle();
            }
        }

        private void OnDisable()
        {
            StopAll();
        }

        public void Configure(
            AudioSource primarySource,
            AudioSource metallicSource,
            FactoryIntroPresenter presenter,
            VoiceLine[] lines,
            AudioSource[] duckedSources,
            float pitch = 0.93f,
            float metallicDelay = 0.012f)
        {
            _primarySource = primarySource;
            _metallicSource = metallicSource;
            _presenter = presenter;
            _lines = lines ?? Array.Empty<VoiceLine>();
            _duckedSources = duckedSources ?? Array.Empty<AudioSource>();
            _pitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
            _metallicDelay = Mathf.Max(0f, metallicDelay);
            CacheDuckedVolumes();
            ConfigureSources();
        }

        public bool PlayLine(int index)
        {
            if (_lines == null || index < 0 || index >= _lines.Length)
            {
                return false;
            }

            VoiceLine line = _lines[index];
            _primarySource?.Stop();
            _metallicSource?.Stop();
            _presenter?.ShowSubtitle("FACTORY EMERGENCY SYSTEM", line.Caption);

            double duration = line.Clip != null
                ? GetProcessedDuration(line.Clip, _pitch)
                : Mathf.Max(1f, _fallbackSubtitleDuration);
            double startTime = AudioSettings.dspTime + 0.045d;
            _voiceEndsAt = startTime + duration + _metallicDelay + 0.08d;
            _voiceActive = true;
            SetDucked(true);

            if (line.Clip == null)
            {
                Debug.LogWarning($"Factory AI voice line {index} has no clip; showing subtitles only.", this);
                return true;
            }

            Schedule(_primarySource, line.Clip, startTime);
            Schedule(_metallicSource, line.Clip, startTime + _metallicDelay);
            return true;
        }

        public void StopAll()
        {
            _primarySource?.Stop();
            _metallicSource?.Stop();
            _voiceActive = false;
            _voiceEndsAt = 0d;
            SetDucked(false);
            _presenter?.HideSubtitle();
        }

        public static double GetProcessedDuration(AudioClip clip, float pitch)
        {
            return clip == null ? 0d : clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
        }

        private void ConfigureSources()
        {
            ConfigureSource(_primarySource, 1f);
            ConfigureSource(_metallicSource, 0.14f);
        }

        private void ConfigureSource(AudioSource source, float volume)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.pitch = _pitch;
            source.volume = volume;
        }

        private static void Schedule(AudioSource source, AudioClip clip, double dspTime)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.clip = clip;
            source.PlayScheduled(dspTime);
        }

        private void CacheDuckedVolumes()
        {
            _baseDuckedVolumes = new float[_duckedSources != null ? _duckedSources.Length : 0];
            for (int index = 0; index < _baseDuckedVolumes.Length; index++)
            {
                _baseDuckedVolumes[index] = _duckedSources[index] != null ? _duckedSources[index].volume : 0f;
            }
        }

        private void SetDucked(bool ducked)
        {
            if (_duckedSources == null)
            {
                return;
            }

            for (int index = 0; index < _duckedSources.Length; index++)
            {
                AudioSource source = _duckedSources[index];
                if (source == null || index >= _baseDuckedVolumes.Length)
                {
                    continue;
                }

                source.volume = _baseDuckedVolumes[index] * (ducked ? _duckMultiplier : 1f);
            }
        }
    }
}
