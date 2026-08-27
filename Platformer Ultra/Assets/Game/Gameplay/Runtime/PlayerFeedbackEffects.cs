using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerFeedbackEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThirdPersonPlayerController _playerController;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private CameraShakeController _cameraShake;
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField] private AudioSource _repairLoopSource;

        [Header("Audio")]
        [SerializeField] private AudioClip _jumpClip;
        [SerializeField] private AudioClip _playerHitClip;
        [SerializeField] private AudioClip _repairLoopClip;
        [SerializeField, Range(0f, 1f)] private float _jumpVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _hitVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _repairVolume = 0.32f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject _jumpEffectPrefab;
        [SerializeField] private GameObject _doubleJumpEffectPrefab;
        [SerializeField] private GameObject _hitEffectPrefab;
        [SerializeField] private GameObject _repairEffectPrefab;
        [SerializeField] private Transform _effectOrigin;

        private GameplayEffect _activeRepairEffect;
        private bool _subscribed;

        public int JumpFeedbackCount { get; private set; }
        public int HitFeedbackCount { get; private set; }
        public bool IsRepairFeedbackActive =>
            (_repairLoopSource != null && _repairLoopSource.isPlaying) || _activeRepairEffect != null;

        private void Awake()
        {
            ResolveReferences();
            ConfigureAudioSources();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopRepairFeedback();
        }

        public void Configure(
            ThirdPersonPlayerController playerController,
            PlayerHealth playerHealth,
            PlayerInteractor playerInteractor,
            CameraShakeController cameraShake,
            AudioSource oneShotSource,
            AudioSource repairLoopSource,
            AudioClip jumpClip,
            AudioClip playerHitClip,
            AudioClip repairLoopClip,
            GameObject jumpEffectPrefab,
            GameObject doubleJumpEffectPrefab,
            GameObject hitEffectPrefab,
            GameObject repairEffectPrefab,
            Transform effectOrigin)
        {
            Unsubscribe();
            _playerController = playerController;
            _playerHealth = playerHealth;
            _playerInteractor = playerInteractor;
            _cameraShake = cameraShake;
            _oneShotSource = oneShotSource;
            _repairLoopSource = repairLoopSource;
            _jumpClip = jumpClip;
            _playerHitClip = playerHitClip;
            _repairLoopClip = repairLoopClip;
            _jumpEffectPrefab = jumpEffectPrefab;
            _doubleJumpEffectPrefab = doubleJumpEffectPrefab;
            _hitEffectPrefab = hitEffectPrefab;
            _repairEffectPrefab = repairEffectPrefab;
            _effectOrigin = effectOrigin;
            ConfigureAudioSources();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void HandleJumped(bool airJump)
        {
            JumpFeedbackCount++;
            PlayOneShot(_jumpClip, _jumpVolume, 0.96f, 1.04f);
            GameObject effectPrefab = airJump && _doubleJumpEffectPrefab != null
                ? _doubleJumpEffectPrefab
                : _jumpEffectPrefab;
            SpawnEffect(effectPrefab, GetFeetPosition(), airJump ? 1.15f : 1f);
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            HitFeedbackCount++;
            PlayOneShot(_playerHitClip, _hitVolume, 0.95f, 1.05f);
            SpawnEffect(_hitEffectPrefab, GetEffectOriginPosition(), 1f);
            bool lethal = _playerHealth != null && !_playerHealth.IsAlive;
            _cameraShake?.Play(lethal ? 0.28f : 0.14f, lethal ? 0.38f : 0.2f, lethal ? 19f : 27f);
        }

        private void HandleTimedInteractionStarted(ITimedInteractable target)
        {
            FactoryObjectiveTerminal terminal = target as FactoryObjectiveTerminal;
            if (terminal == null || terminal.MachineState != FactoryMachineState.Broken)
            {
                return;
            }

            StopRepairFeedback();
            if (_repairLoopSource != null && _repairLoopClip != null && Application.isPlaying)
            {
                _repairLoopSource.clip = _repairLoopClip;
                _repairLoopSource.volume = _repairVolume;
                _repairLoopSource.loop = true;
                _repairLoopSource.pitch = 1f;
                _repairLoopSource.Play();
            }

            Vector3 position = terminal.MachineHealth != null && terminal.MachineHealth.Targetable != null
                ? terminal.MachineHealth.Targetable.TargetPoint.position
                : terminal.transform.position;
            GameObject instance = SpawnEffect(_repairEffectPrefab, position, 1f);
            _activeRepairEffect = instance != null ? instance.GetComponent<GameplayEffect>() : null;
        }

        private void HandleTimedInteractionEnded(ITimedInteractable target, bool completed)
        {
            if (target is FactoryObjectiveTerminal)
            {
                StopRepairFeedback();
            }
        }

        private void StopRepairFeedback()
        {
            if (_repairLoopSource != null)
            {
                _repairLoopSource.Stop();
                _repairLoopSource.clip = null;
            }

            if (_activeRepairEffect != null)
            {
                _activeRepairEffect.Stop();
                _activeRepairEffect = null;
            }
        }

        private void PlayOneShot(AudioClip clip, float volume, float minimumPitch, float maximumPitch)
        {
            if (_oneShotSource == null || clip == null || !Application.isPlaying)
            {
                return;
            }

            _oneShotSource.pitch = Random.Range(minimumPitch, maximumPitch);
            _oneShotSource.PlayOneShot(clip, volume);
        }

        private GameObject SpawnEffect(GameObject prefab, Vector3 position, float scale)
        {
            if (prefab == null || !Application.isPlaying)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            GameplayEffect effect = instance.GetComponent<GameplayEffect>();
            effect?.Play();
            return instance;
        }

        private Vector3 GetFeetPosition()
        {
            return transform.position + Vector3.up * 0.08f;
        }

        private Vector3 GetEffectOriginPosition()
        {
            return _effectOrigin != null ? _effectOrigin.position : transform.position + Vector3.up * 1.1f;
        }

        private void ResolveReferences()
        {
            _playerController ??= GetComponent<ThirdPersonPlayerController>();
            _playerHealth ??= GetComponent<PlayerHealth>();
            _playerInteractor ??= GetComponent<PlayerInteractor>();
        }

        private void ConfigureAudioSources()
        {
            if (_oneShotSource != null)
            {
                _oneShotSource.playOnAwake = false;
                _oneShotSource.loop = false;
                _oneShotSource.spatialBlend = 0f;
            }

            if (_repairLoopSource != null)
            {
                _repairLoopSource.playOnAwake = false;
                _repairLoopSource.loop = true;
                _repairLoopSource.spatialBlend = 0.15f;
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            ResolveReferences();
            if (_playerController != null)
            {
                _playerController.Jumped += HandleJumped;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Damaged += HandleDamaged;
            }

            if (_playerInteractor != null)
            {
                _playerInteractor.TimedInteractionStarted += HandleTimedInteractionStarted;
                _playerInteractor.TimedInteractionEnded += HandleTimedInteractionEnded;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_playerController != null)
            {
                _playerController.Jumped -= HandleJumped;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Damaged -= HandleDamaged;
            }

            if (_playerInteractor != null)
            {
                _playerInteractor.TimedInteractionStarted -= HandleTimedInteractionStarted;
                _playerInteractor.TimedInteractionEnded -= HandleTimedInteractionEnded;
            }

            _subscribed = false;
        }
    }
}
