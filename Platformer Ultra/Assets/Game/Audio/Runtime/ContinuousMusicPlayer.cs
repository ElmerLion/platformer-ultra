using System;
using UnityEngine;
using UnityEngine.Audio;

namespace PlatformerUltra.Audio
{
    [DisallowMultipleComponent]
    public sealed class ContinuousMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _tracks = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _volume = 0.22f;
        [SerializeField, Min(0f)] private float _crossfadeDuration = 4f;
        [SerializeField] private AudioSource _sourceA;
        [SerializeField] private AudioSource _sourceB;
        [SerializeField] private AudioMixerGroup _outputGroup;

        private AudioSource _activeSource;
        private AudioSource _incomingSource;
        private int _trackIndex;
        private float _crossfadeElapsed;
        private bool _isCrossfading;
        private bool _waitingForFirstTrack;

        public void Configure(
            AudioClip[] tracks,
            float volume = 0.22f,
            float crossfadeDuration = 4f,
            AudioMixerGroup outputGroup = null)
        {
            _tracks = tracks ?? Array.Empty<AudioClip>();
            _volume = Mathf.Clamp01(volume);
            _crossfadeDuration = Mathf.Max(0f, crossfadeDuration);
            _outputGroup = outputGroup;
            EnsureSources();
            ConfigureSources();
        }

        private void Awake()
        {
            EnsureSources();
            ConfigureSources();
        }

        private void Start()
        {
            PlayFirstTrack();
        }

        private void OnDisable()
        {
            _sourceA?.Stop();
            _sourceB?.Stop();
            _isCrossfading = false;
        }

        private void OnValidate()
        {
            if (_sourceA != null && _sourceB != null)
            {
                ConfigureSources();
            }
        }

        private void Update()
        {
            if (_activeSource == null || _tracks.Length == 0)
            {
                return;
            }

            if (_waitingForFirstTrack)
            {
                TryStartFirstTrack();
                return;
            }

            if (_isCrossfading)
            {
                UpdateCrossfade();
                return;
            }

            if (_tracks.Length == 1)
            {
                return;
            }

            float remaining = _activeSource.clip != null
                ? _activeSource.clip.length - _activeSource.time
                : 0f;
            float fadeDuration = GetUsableCrossfadeDuration(_activeSource.clip);
            if (!_activeSource.isPlaying || remaining <= fadeDuration)
            {
                BeginCrossfade(fadeDuration);
            }
        }

        private void PlayFirstTrack()
        {
            _trackIndex = FindNextValidTrackIndex(-1);
            if (_trackIndex < 0)
            {
                return;
            }

            _activeSource = _sourceA;
            _incomingSource = _sourceB;
            _activeSource.clip = _tracks[_trackIndex];
            _activeSource.loop = _tracks.Length == 1;
            _activeSource.volume = _volume;
            _waitingForFirstTrack = true;
            TryStartFirstTrack();
        }

        private void BeginCrossfade(float fadeDuration)
        {
            int nextIndex = FindNextValidTrackIndex(_trackIndex);
            if (nextIndex < 0 || nextIndex == _trackIndex)
            {
                _activeSource.loop = true;
                return;
            }

            AudioClip nextTrack = _tracks[nextIndex];
            if (nextTrack.loadState == AudioDataLoadState.Unloaded)
            {
                nextTrack.LoadAudioData();
            }

            if (nextTrack.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            _trackIndex = nextIndex;
            _incomingSource.clip = nextTrack;
            _incomingSource.loop = false;
            _incomingSource.volume = fadeDuration <= 0f ? _volume : 0f;
            _incomingSource.Play();
            _crossfadeElapsed = 0f;
            _isCrossfading = fadeDuration > 0f;

            if (!_isCrossfading)
            {
                CompleteCrossfade();
            }
        }

        private void UpdateCrossfade()
        {
            float fadeDuration = GetUsableCrossfadeDuration(_activeSource.clip);
            _crossfadeElapsed += Time.unscaledDeltaTime;
            float normalized = fadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_crossfadeElapsed / fadeDuration);
            float angle = normalized * Mathf.PI * 0.5f;
            _activeSource.volume = _volume * Mathf.Cos(angle);
            _incomingSource.volume = _volume * Mathf.Sin(angle);

            if (normalized >= 1f)
            {
                CompleteCrossfade();
            }
        }

        private void CompleteCrossfade()
        {
            _activeSource.Stop();
            _activeSource.volume = 0f;
            (_activeSource, _incomingSource) = (_incomingSource, _activeSource);
            _activeSource.volume = _volume;
            _isCrossfading = false;
        }

        private void TryStartFirstTrack()
        {
            AudioClip track = _activeSource.clip;
            if (track == null)
            {
                _waitingForFirstTrack = false;
                return;
            }

            if (track.loadState == AudioDataLoadState.Unloaded)
            {
                track.LoadAudioData();
            }

            if (track.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            _activeSource.Play();
            _waitingForFirstTrack = false;
        }

        private int FindNextValidTrackIndex(int currentIndex)
        {
            for (int offset = 1; offset <= _tracks.Length; offset++)
            {
                int index = (currentIndex + offset) % _tracks.Length;
                if (_tracks[index] != null)
                {
                    return index;
                }
            }

            return -1;
        }

        private float GetUsableCrossfadeDuration(AudioClip clip)
        {
            return clip == null
                ? 0f
                : Mathf.Min(_crossfadeDuration, clip.length * 0.25f);
        }

        private void EnsureSources()
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (_sourceA == null)
            {
                _sourceA = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            }

            if (_sourceB == null)
            {
                _sourceB = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureSources()
        {
            ConfigureSource(_sourceA);
            ConfigureSource(_sourceB);
        }

        private void ConfigureSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 0;
            source.outputAudioMixerGroup = _outputGroup;
        }
    }
}
