using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackPresentation : MonoBehaviour
    {
        [SerializeField] private EnemyAttackController _attackController;
        [SerializeField] private GameObject _normalImpactEffectPrefab;
        [SerializeField] private GameObject _specialImpactEffectPrefab;
        [SerializeField] private GameObject _specialLaunchEffectPrefab;
        [SerializeField] private CameraShakeController _cameraShake;
        [SerializeField, Min(0.1f)] private float _normalEffectScale = 1f;
        [SerializeField, Min(0.1f)] private float _specialEffectScale = 1f;
        [SerializeField, Min(0f)] private float _normalShakeAmplitude = 0.07f;
        [SerializeField, Min(0f)] private float _specialShakeAmplitude = 0.36f;
        [SerializeField, Min(0f)] private float _shakeMaximumDistance = 22f;

        private bool _subscribed;

        public int ImpactPresentationCount { get; private set; }
        public GameObject NormalImpactEffectPrefab => _normalImpactEffectPrefab;
        public GameObject SpecialImpactEffectPrefab => _specialImpactEffectPrefab;

        private void Awake()
        {
            _attackController ??= GetComponent<EnemyAttackController>();
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
            EnemyAttackController attackController,
            GameObject normalImpactEffectPrefab,
            GameObject specialImpactEffectPrefab,
            GameObject specialLaunchEffectPrefab,
            float normalEffectScale,
            float specialEffectScale,
            float normalShakeAmplitude,
            float specialShakeAmplitude,
            float shakeMaximumDistance)
        {
            Unsubscribe();
            _attackController = attackController;
            _normalImpactEffectPrefab = normalImpactEffectPrefab;
            _specialImpactEffectPrefab = specialImpactEffectPrefab;
            _specialLaunchEffectPrefab = specialLaunchEffectPrefab;
            _normalEffectScale = Mathf.Max(0.1f, normalEffectScale);
            _specialEffectScale = Mathf.Max(0.1f, specialEffectScale);
            _normalShakeAmplitude = Mathf.Max(0f, normalShakeAmplitude);
            _specialShakeAmplitude = Mathf.Max(0f, specialShakeAmplitude);
            _shakeMaximumDistance = Mathf.Max(0f, shakeMaximumDistance);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void InitializeRuntime(CameraShakeController cameraShake)
        {
            _cameraShake = cameraShake;
        }

        private void HandleAttackStarted(bool special)
        {
            if (special)
            {
                SpawnEffect(_specialLaunchEffectPrefab, transform.position + Vector3.up * 0.08f, _specialEffectScale * 0.75f);
            }
        }

        private void HandleAttackImpacted(bool special, Vector3 position)
        {
            ImpactPresentationCount++;
            SpawnEffect(
                special ? _specialImpactEffectPrefab : _normalImpactEffectPrefab,
                position,
                special ? _specialEffectScale : _normalEffectScale);

            if (_cameraShake == null)
            {
                return;
            }

            _cameraShake.PlayAt(
                position,
                special ? _specialShakeAmplitude : _normalShakeAmplitude,
                special ? 0.46f : 0.16f,
                special ? 18f : 29f,
                _shakeMaximumDistance);
        }

        private static void SpawnEffect(GameObject prefab, Vector3 position, float scale)
        {
            if (prefab == null || !Application.isPlaying)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            instance.GetComponent<GameplayEffect>()?.Play();
        }

        private void Subscribe()
        {
            if (_subscribed || _attackController == null)
            {
                return;
            }

            _attackController.AttackStarted += HandleAttackStarted;
            _attackController.AttackImpacted += HandleAttackImpacted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _attackController == null)
            {
                return;
            }

            _attackController.AttackStarted -= HandleAttackStarted;
            _attackController.AttackImpacted -= HandleAttackImpacted;
            _subscribed = false;
        }
    }
}
