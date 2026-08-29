using System;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementAudio : MonoBehaviour
    {
        [SerializeField] private ThirdPersonPlayerController _controller;
        [SerializeField] private ProceduralPlayerAnimator _proceduralAnimator;
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _footstepClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _jumpClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _doubleJumpClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _dashClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _lightLandingClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _heavyLandingClips = Array.Empty<AudioClip>();
        [SerializeField, Min(0f)] private float _heavyLandingSpeed = 7f;
        [SerializeField, Range(0f, 1f)] private float _dashVolumeScale = 0.8f;

        private int _lastFootstepIndex = -1;
        private int _lastJumpIndex = -1;
        private int _lastDoubleJumpIndex = -1;
        private int _lastDashIndex = -1;
        private int _lastLightLandingIndex = -1;
        private int _lastHeavyLandingIndex = -1;
        private bool _subscribed;

        public AudioClip[] FootstepClips => _footstepClips;
        public AudioClip[] JumpClips => _jumpClips;
        public AudioClip[] DoubleJumpClips => _doubleJumpClips;
        public AudioClip[] DashClips => _dashClips;
        public AudioClip[] LightLandingClips => _lightLandingClips;
        public AudioClip[] HeavyLandingClips => _heavyLandingClips;
        public AudioSource Source => _source;
        public int PlaybackCount { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            ConfigureSource();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            ThirdPersonPlayerController controller,
            ProceduralPlayerAnimator proceduralAnimator,
            AudioSource source,
            AudioClip[] footstepClips,
            AudioClip[] jumpClips,
            AudioClip[] doubleJumpClips,
            AudioClip[] dashClips,
            AudioClip[] lightLandingClips,
            AudioClip[] heavyLandingClips,
            float heavyLandingSpeed = 7f)
        {
            Unsubscribe();
            _controller = controller;
            _proceduralAnimator = proceduralAnimator;
            _source = source;
            _footstepClips = footstepClips ?? Array.Empty<AudioClip>();
            _jumpClips = jumpClips ?? Array.Empty<AudioClip>();
            _doubleJumpClips = doubleJumpClips ?? Array.Empty<AudioClip>();
            _dashClips = dashClips ?? Array.Empty<AudioClip>();
            _lightLandingClips = lightLandingClips ?? Array.Empty<AudioClip>();
            _heavyLandingClips = heavyLandingClips ?? Array.Empty<AudioClip>();
            _heavyLandingSpeed = Mathf.Max(0f, heavyLandingSpeed);
            ConfigureSource();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void HandleFootstepped()
        {
            PlayRandom(_footstepClips, ref _lastFootstepIndex, 0.42f, 0.96f, 1.04f);
        }

        private void HandleJumped(bool airJump)
        {
            if (airJump)
            {
                PlayRandom(_doubleJumpClips, ref _lastDoubleJumpIndex, 0.68f, 0.98f, 1.04f);
                return;
            }

            PlayRandom(_jumpClips, ref _lastJumpIndex, 0.62f, 0.97f, 1.03f);
        }

        private void HandleDashed(Vector3 direction, bool airborne)
        {
            float baseVolume = airborne ? 0.64f : 0.72f;
            PlayRandom(_dashClips, ref _lastDashIndex, baseVolume * _dashVolumeScale, 0.97f, 1.03f);
        }

        private void HandleLanded(float impactSpeed)
        {
            float volume = Mathf.InverseLerp(2.25f, 12f, impactSpeed);
            if (impactSpeed >= _heavyLandingSpeed)
            {
                PlayRandom(_heavyLandingClips, ref _lastHeavyLandingIndex, Mathf.Lerp(0.62f, 0.9f, volume), 0.96f, 1.02f);
                return;
            }

            PlayRandom(_lightLandingClips, ref _lastLightLandingIndex, Mathf.Lerp(0.42f, 0.62f, volume), 0.98f, 1.04f);
        }

        private void PlayRandom(
            AudioClip[] clips,
            ref int previousIndex,
            float volume,
            float minimumPitch,
            float maximumPitch)
        {
            if (_source == null || clips == null || clips.Length == 0 || !Application.isPlaying || Time.timeScale <= 0f)
            {
                return;
            }

            int index = ChooseIndex(clips, previousIndex);
            if (index < 0)
            {
                return;
            }

            previousIndex = index;
            _source.pitch = UnityEngine.Random.Range(minimumPitch, maximumPitch);
            _source.PlayOneShot(clips[index], volume * UnityEngine.Random.Range(0.92f, 1.05f));
            PlaybackCount++;
        }

        private static int ChooseIndex(AudioClip[] clips, int previousIndex)
        {
            if (clips.Length == 1)
            {
                return clips[0] != null ? 0 : -1;
            }

            int start = UnityEngine.Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                int index = (start + offset) % clips.Length;
                if (index != previousIndex && clips[index] != null)
                {
                    return index;
                }
            }

            return previousIndex >= 0 && previousIndex < clips.Length && clips[previousIndex] != null
                ? previousIndex
                : -1;
        }

        private void ResolveReferences()
        {
            _controller ??= GetComponent<ThirdPersonPlayerController>();
            _proceduralAnimator ??= GetComponentInChildren<ProceduralPlayerAnimator>(true);
            _source ??= GetComponent<AudioSource>();
        }

        private void ConfigureSource()
        {
            if (_source == null)
            {
                return;
            }

            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0.15f;
            _source.dopplerLevel = 0f;
            _source.priority = 72;
            _source.minDistance = 1f;
            _source.maxDistance = 15f;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            ResolveReferences();
            if (_controller != null)
            {
                _controller.Jumped += HandleJumped;
                _controller.Dashed += HandleDashed;
                _controller.Landed += HandleLanded;
            }

            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.Footstepped += HandleFootstepped;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_controller != null)
            {
                _controller.Jumped -= HandleJumped;
                _controller.Dashed -= HandleDashed;
                _controller.Landed -= HandleLanded;
            }

            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.Footstepped -= HandleFootstepped;
            }

            _subscribed = false;
        }
    }
}
