using System;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAudioPresentation : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private MonoBehaviour _motorBehaviour;
        [SerializeField] private ProceduralEnemyAnimator _proceduralAnimator;
        [SerializeField] private EnemyAttackController _attackController;
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField] private AudioSource _loopSource;
        [SerializeField] private AudioClip[] _footstepClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _normalAttackStartClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _normalAttackImpactClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _specialAttackStartClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _specialAttackImpactClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip _movementLoopClip;
        [SerializeField, Min(0.1f)] private float _groundMinimumDistance = 5f;
        [SerializeField, Min(0.1f)] private float _saboteurMaximumDistance = 36f;
        [SerializeField, Min(0.1f)] private float _armoredMaximumDistance = 44f;

        private IEnemyMotor _motor;
        private int _lastFootstepIndex = -1;
        private int _lastNormalStartIndex = -1;
        private int _lastNormalImpactIndex = -1;
        private int _lastSpecialStartIndex = -1;
        private int _lastSpecialImpactIndex = -1;
        private bool _subscribed;

        public AudioClip[] FootstepClips => _footstepClips;
        public AudioClip MovementLoopClip => _movementLoopClip;
        public AudioSource OneShotSource => _oneShotSource;
        public AudioSource LoopSource => _loopSource;
        public int PlaybackCount { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            ConfigureSources();
        }

        private void OnEnable()
        {
            Subscribe();
            StartMovementLoop();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_loopSource != null)
            {
                _loopSource.Stop();
            }
        }

        private void Update()
        {
            if (_loopSource == null || _movementLoopClip == null || _definition == null ||
                _definition.Archetype != EnemyArchetype.Drone)
            {
                return;
            }

            if (!_loopSource.isPlaying && Application.isPlaying)
            {
                StartMovementLoop();
            }

            float speed = _motor != null ? Vector3.ProjectOnPlane(_motor.Velocity, Vector3.up).magnitude : 0f;
            float referenceSpeed = Mathf.Max(0.1f, _definition.PlayerChaseSpeed);
            float normalizedSpeed = Mathf.Clamp01(speed / referenceSpeed);
            _loopSource.pitch = Mathf.Lerp(0.92f, 1.08f, normalizedSpeed);
            _loopSource.volume = Mathf.Lerp(0.12f, 0.2f, normalizedSpeed);
        }

        public void Configure(
            EnemyDefinition definition,
            MonoBehaviour motorBehaviour,
            ProceduralEnemyAnimator proceduralAnimator,
            EnemyAttackController attackController,
            AudioSource oneShotSource,
            AudioSource loopSource,
            AudioClip[] footstepClips,
            AudioClip[] normalAttackStartClips,
            AudioClip[] normalAttackImpactClips,
            AudioClip[] specialAttackStartClips,
            AudioClip[] specialAttackImpactClips,
            AudioClip movementLoopClip)
        {
            Unsubscribe();
            _definition = definition;
            _motorBehaviour = motorBehaviour;
            _proceduralAnimator = proceduralAnimator;
            _attackController = attackController;
            _oneShotSource = oneShotSource;
            _loopSource = loopSource;
            _footstepClips = footstepClips ?? Array.Empty<AudioClip>();
            _normalAttackStartClips = normalAttackStartClips ?? Array.Empty<AudioClip>();
            _normalAttackImpactClips = normalAttackImpactClips ?? Array.Empty<AudioClip>();
            _specialAttackStartClips = specialAttackStartClips ?? Array.Empty<AudioClip>();
            _specialAttackImpactClips = specialAttackImpactClips ?? Array.Empty<AudioClip>();
            _movementLoopClip = movementLoopClip;
            ResolveReferences();
            ConfigureSources();
            if (isActiveAndEnabled)
            {
                Subscribe();
                StartMovementLoop();
            }
        }

        private void HandleFootstepped()
        {
            float volume = _definition != null && _definition.Archetype == EnemyArchetype.Armored ? 0.88f : 0.64f;
            PlayRandom(_footstepClips, ref _lastFootstepIndex, volume, 0.96f, 1.04f);
        }

        private void HandleAttackStarted(bool special)
        {
            if (special)
            {
                PlayRandom(_specialAttackStartClips, ref _lastSpecialStartIndex, 0.96f, 0.98f, 1.02f);
                return;
            }

            float volume = IsGroundEnemy ? 0.84f : 0.68f;
            PlayRandom(_normalAttackStartClips, ref _lastNormalStartIndex, volume, 0.97f, 1.03f);
        }

        private void HandleAttackImpacted(bool special, Vector3 position)
        {
            if (special)
            {
                PlayRandom(_specialAttackImpactClips, ref _lastSpecialImpactIndex, 1f, 0.97f, 1.02f);
                return;
            }

            float volume = IsGroundEnemy ? 0.95f : 0.78f;
            PlayRandom(_normalAttackImpactClips, ref _lastNormalImpactIndex, volume, 0.97f, 1.03f);
        }

        private void PlayRandom(AudioClip[] clips, ref int previousIndex, float volume, float minPitch, float maxPitch)
        {
            if (_oneShotSource == null || clips == null || clips.Length == 0 || !Application.isPlaying || Time.timeScale <= 0f)
            {
                return;
            }

            int index = ChooseIndex(clips, previousIndex);
            if (index < 0)
            {
                return;
            }

            previousIndex = index;
            _oneShotSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            _oneShotSource.PlayOneShot(clips[index], volume * UnityEngine.Random.Range(0.92f, 1.05f));
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
            _motor = _motorBehaviour as IEnemyMotor;
            _proceduralAnimator ??= GetComponentInChildren<ProceduralEnemyAnimator>(true);
            _attackController ??= GetComponent<EnemyAttackController>();
        }

        private void ConfigureSources()
        {
            bool drone = _definition != null && _definition.Archetype == EnemyArchetype.Drone;
            float maximumDistance = drone
                ? 18f
                : (_definition != null && _definition.Archetype == EnemyArchetype.Armored
                    ? _armoredMaximumDistance
                    : _saboteurMaximumDistance);
            ConfigureSource(
                _oneShotSource,
                drone ? 84 : 64,
                drone ? 1.5f : _groundMinimumDistance,
                maximumDistance,
                drone ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear);
            ConfigureSource(_loopSource, 150, 1.5f, 20f, AudioRolloffMode.Logarithmic);
            if (_loopSource != null)
            {
                _loopSource.loop = true;
                _loopSource.clip = _movementLoopClip;
            }
        }

        private static void ConfigureSource(
            AudioSource source,
            int priority,
            float minimumDistance,
            float maximumDistance,
            AudioRolloffMode rolloffMode)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = rolloffMode;
            source.minDistance = minimumDistance;
            source.maxDistance = maximumDistance;
            source.priority = priority;
            source.reverbZoneMix = 0.28f;
        }

        private bool IsGroundEnemy =>
            _definition != null && _definition.Archetype != EnemyArchetype.Drone;

        private void StartMovementLoop()
        {
            if (_loopSource == null || _movementLoopClip == null || !Application.isPlaying || !_loopSource.isActiveAndEnabled)
            {
                return;
            }

            _loopSource.clip = _movementLoopClip;
            _loopSource.loop = true;
            _loopSource.volume = 0.12f;
            if (!_loopSource.isPlaying)
            {
                _loopSource.Play();
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            ResolveReferences();
            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.Footstepped += HandleFootstepped;
            }

            if (_attackController != null)
            {
                _attackController.AttackStarted += HandleAttackStarted;
                _attackController.AttackImpacted += HandleAttackImpacted;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.Footstepped -= HandleFootstepped;
            }

            if (_attackController != null)
            {
                _attackController.AttackStarted -= HandleAttackStarted;
                _attackController.AttackImpacted -= HandleAttackImpacted;
            }

            _subscribed = false;
        }
    }
}
