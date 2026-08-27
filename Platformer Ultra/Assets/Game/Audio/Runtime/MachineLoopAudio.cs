using UnityEngine;

namespace PlatformerUltra.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MachineLoopAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.12f;
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.45f;
        [SerializeField, Range(0f, 0.1f)] private float _pitchVariation = 0.02f;
        [SerializeField, Min(0.01f)] private float _minDistance = 2.5f;
        [SerializeField, Min(0.01f)] private float _maxDistance = 14f;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private AudioSource _source;

        private float _targetVolume;

        public void Configure(
            AudioClip clip,
            float volume = 0.12f,
            float minDistance = 2.5f,
            float maxDistance = 14f,
            float fadeInDuration = 0.45f)
        {
            _clip = clip;
            _volume = Mathf.Clamp01(volume);
            _minDistance = Mathf.Max(0.01f, minDistance);
            _maxDistance = Mathf.Max(_minDistance, maxDistance);
            _fadeInDuration = Mathf.Max(0f, fadeInDuration);
            CacheAndConfigureSource();
        }

        public void SetPlaying(bool shouldPlay)
        {
            CacheAndConfigureSource();
            if (shouldPlay)
            {
                StartLoop();
                return;
            }

            _targetVolume = 0f;
            _source.Stop();
        }

        private void Awake()
        {
            CacheAndConfigureSource();
        }

        private void OnEnable()
        {
            if (Application.isPlaying && _playOnEnable)
            {
                StartLoop();
            }
        }

        private void Start()
        {
            if (_playOnEnable && (_source == null || !_source.isPlaying))
            {
                StartLoop();
            }
        }

        private void OnDisable()
        {
            if (_source != null)
            {
                _source.Stop();
            }
        }

        private void OnValidate()
        {
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            if (_source != null || TryGetComponent(out _source))
            {
                ConfigureSource();
            }
        }

        private void Update()
        {
            if (_source == null || !_source.isPlaying)
            {
                return;
            }

            if (_fadeInDuration <= 0f)
            {
                _source.volume = _targetVolume;
                return;
            }

            _source.volume = Mathf.MoveTowards(
                _source.volume,
                _targetVolume,
                Time.unscaledDeltaTime * _volume / _fadeInDuration);
        }

        private void StartLoop()
        {
            if (_source == null || _clip == null)
            {
                return;
            }

            if (_source.isPlaying)
            {
                _targetVolume = _volume;
                return;
            }

            _source.clip = _clip;
            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _source.volume = _fadeInDuration > 0f ? 0f : _volume;
            _targetVolume = _volume;
            _source.Play();
        }

        private void CacheAndConfigureSource()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            ConfigureSource();
        }

        private void ConfigureSource()
        {
            if (_source == null)
            {
                return;
            }

            _source.clip = _clip;
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Logarithmic;
            _source.minDistance = _minDistance;
            _source.maxDistance = _maxDistance;
            _source.dopplerLevel = 0f;
            _source.spread = 35f;
            _source.priority = 160;

            if (!Application.isPlaying)
            {
                _source.volume = _volume;
            }
        }
    }
}
