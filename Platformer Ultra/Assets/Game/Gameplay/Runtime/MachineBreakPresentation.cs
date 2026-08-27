using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MachineBreakPresentation : MonoBehaviour
    {
        [SerializeField] private FactoryMachineHealth _machineHealth;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _rubbleCrashClip;
        [SerializeField] private GameObject _breakEffectPrefab;
        [SerializeField] private CameraShakeController _cameraShake;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.78f;
        [SerializeField, Min(0f)] private float _shakeMaximumDistance = 24f;

        private bool _subscribed;

        public int BreakPresentationCount { get; private set; }
        public AudioClip RubbleCrashClip => _rubbleCrashClip;
        public GameObject BreakEffectPrefab => _breakEffectPrefab;

        private void Awake()
        {
            _machineHealth ??= GetComponent<FactoryMachineHealth>();
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
            FactoryMachineHealth machineHealth,
            AudioSource audioSource,
            AudioClip rubbleCrashClip,
            GameObject breakEffectPrefab,
            CameraShakeController cameraShake)
        {
            Unsubscribe();
            _machineHealth = machineHealth;
            _audioSource = audioSource;
            _rubbleCrashClip = rubbleCrashClip;
            _breakEffectPrefab = breakEffectPrefab;
            _cameraShake = cameraShake;
            ConfigureSource();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void HandleMachineBroken(DamageInfo damageInfo)
        {
            BreakPresentationCount++;
            Vector3 position = _machineHealth != null && _machineHealth.Targetable != null
                ? _machineHealth.Targetable.TargetPoint.position
                : transform.position;

            if (_audioSource != null && _rubbleCrashClip != null && Application.isPlaying)
            {
                _audioSource.pitch = Random.Range(0.94f, 1.03f);
                _audioSource.PlayOneShot(_rubbleCrashClip, _volume);
            }

            if (_breakEffectPrefab != null && Application.isPlaying)
            {
                GameObject instance = Instantiate(_breakEffectPrefab, position, Quaternion.identity);
                instance.GetComponent<GameplayEffect>()?.Play();
            }

            _cameraShake?.PlayAt(position, 0.3f, 0.38f, 20f, _shakeMaximumDistance);
        }

        private void ConfigureSource()
        {
            if (_audioSource == null)
            {
                return;
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.minDistance = 3f;
            _audioSource.maxDistance = 24f;
        }

        private void Subscribe()
        {
            if (_subscribed || _machineHealth == null)
            {
                return;
            }

            _machineHealth.Died += HandleMachineBroken;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _machineHealth == null)
            {
                return;
            }

            _machineHealth.Died -= HandleMachineBroken;
            _subscribed = false;
        }
    }
}
