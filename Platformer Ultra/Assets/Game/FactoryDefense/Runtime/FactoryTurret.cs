using System;
using System.Collections.Generic;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.FactoryDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(FactionMember), typeof(Targetable))]
    public sealed class FactoryTurret : MonoBehaviour, IDamageable, IFactoryTarget
    {
        [Header("References")]
        [SerializeField] private Health _health;
        [SerializeField] private FactionMember _factionMember;
        [SerializeField] private Targetable _targetable;
        [SerializeField] private TargetPoint _targetPoint;
        [SerializeField] private Transform _yawPivot;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private GameObject _muzzleFlash;
        [SerializeField] private TurretLaserTracer _laserTracerPrefab;
        [SerializeField] private AudioSource _shotAudioSource;
        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private EnemyRuntimeRegistry _enemyRegistry;
        [SerializeField] private MachineTargetRegistry _factoryRegistry;

        [Header("Balance")]
        [SerializeField, Min(1)] private int _maximumHealth = 80;
        [SerializeField, Min(1f)] private float _range = 15f;
        [SerializeField, Min(1)] private int _damage = 8;
        [SerializeField, Min(0.05f)] private float _shotInterval = 1f;
        [SerializeField, Min(1f)] private float _turnSpeed = 90f;
        [SerializeField, Range(0.1f, 45f)] private float _firingTolerance = 5f;
        [SerializeField, Min(0.02f)] private float _targetRefreshInterval = 0.2f;
        [SerializeField] private LayerMask _lineOfSightMask = ~0;

        private EnemyHealth _target;
        private TurretBuildSpot _owningSpot;
        private float _nextTargetRefreshTime;
        private float _nextFireTime;
        private float _muzzleFlashUntil;
        private bool _runtimeInitialized;
        private bool _deathHandled;
        private bool _registered;

        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
        public int MaximumHealth => _health != null ? _health.MaximumHealth : _maximumHealth;
        public bool IsAlive => _health != null && _health.IsAlive && !_deathHandled;
        public bool IsEligibleTarget => IsAlive && _targetable != null && _targetable.IsTargetable;
        public Targetable Targetable => _targetable;
        public EnemyHealth CurrentTarget => _target;
        public float Range => _range;
        public int Damage => _damage;
        public float ShotInterval => _shotInterval;
        public TurretLaserTracer LaserTracerPrefab => _laserTracerPrefab;
        public AudioClip ShotClip => _shotClip;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<FactoryTurret> Destroyed;
        public event Action<FactoryTurret, EnemyHealth> Fired;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (_runtimeInitialized && IsAlive)
            {
                RegisterTarget();
            }
        }

        private void OnDisable()
        {
            UnregisterTarget();
            _target = null;
        }

        private void OnValidate()
        {
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _range = Mathf.Max(1f, _range);
            _damage = Mathf.Max(1, _damage);
            _shotInterval = Mathf.Max(0.05f, _shotInterval);
            _turnSpeed = Mathf.Max(1f, _turnSpeed);
            _firingTolerance = Mathf.Clamp(_firingTolerance, 0.1f, 45f);
            _targetRefreshInterval = Mathf.Max(0.02f, _targetRefreshInterval);
            ResolveReferences();
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Configure(
            Health health,
            FactionMember factionMember,
            Targetable targetable,
            TargetPoint targetPoint,
            Transform yawPivot,
            Transform muzzle,
            GameObject muzzleFlash,
            LayerMask lineOfSightMask,
            int maximumHealth = 80,
            float range = 15f,
            int damage = 8,
            float shotInterval = 1f,
            float turnSpeed = 90f,
            float firingTolerance = 5f,
            float targetRefreshInterval = 0.2f,
            TurretLaserTracer laserTracerPrefab = null,
            AudioSource shotAudioSource = null,
            AudioClip shotClip = null)
        {
            _health = health;
            _factionMember = factionMember;
            _targetable = targetable;
            _targetPoint = targetPoint;
            _yawPivot = yawPivot;
            _muzzle = muzzle;
            _muzzleFlash = muzzleFlash;
            _laserTracerPrefab = laserTracerPrefab;
            _shotAudioSource = shotAudioSource;
            _shotClip = shotClip;
            _lineOfSightMask = lineOfSightMask;
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _range = Mathf.Max(1f, range);
            _damage = Mathf.Max(1, damage);
            _shotInterval = Mathf.Max(0.05f, shotInterval);
            _turnSpeed = Mathf.Max(1f, turnSpeed);
            _firingTolerance = Mathf.Clamp(firingTolerance, 0.1f, 45f);
            _targetRefreshInterval = Mathf.Max(0.02f, targetRefreshInterval);
            InitializeComponents();
        }

        public void InitializeRuntime(
            EnemyRuntimeRegistry enemyRegistry,
            MachineTargetRegistry factoryRegistry,
            TurretBuildSpot owningSpot)
        {
            UnregisterTarget();
            _enemyRegistry = enemyRegistry;
            _factoryRegistry = factoryRegistry;
            _owningSpot = owningSpot;
            _runtimeInitialized = true;
            _deathHandled = false;
            _target = null;
            _nextTargetRefreshTime = 0f;
            _nextFireTime = 0f;
            InitializeComponents();
            if (_health != null)
            {
                _health.RestoreFull();
            }

            if (_targetable != null)
            {
                _targetable.SetTargetable(true);
            }

            if (_muzzleFlash != null)
            {
                _muzzleFlash.SetActive(false);
            }
            RegisterTarget();
        }

        public void Tick(float deltaTime, float timestamp)
        {
            if (!_runtimeInitialized || !IsAlive || _yawPivot == null || _muzzle == null)
            {
                return;
            }

            if (_muzzleFlash != null && _muzzleFlash.activeSelf && timestamp >= _muzzleFlashUntil)
            {
                _muzzleFlash.SetActive(false);
            }

            if (timestamp >= _nextTargetRefreshTime || !CanTarget(_target))
            {
                AcquireTarget();
                _nextTargetRefreshTime = timestamp + _targetRefreshInterval;
            }

            if (!CanTarget(_target))
            {
                return;
            }

            Vector3 direction = _target.Targetable.TargetPoint.position - _yawPivot.position;
            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (planarDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            _yawPivot.rotation = Quaternion.RotateTowards(
                _yawPivot.rotation,
                desiredRotation,
                _turnSpeed * Mathf.Max(0f, deltaTime));
            if (Quaternion.Angle(_yawPivot.rotation, desiredRotation) > _firingTolerance ||
                timestamp < _nextFireTime || !HasLineOfSight(_target))
            {
                return;
            }

            Fire(_target, timestamp);
        }

        public bool CanTarget(EnemyHealth enemy)
        {
            if (_muzzle == null || enemy == null || !enemy.IsAlive || enemy.Targetable == null ||
                !enemy.Targetable.IsTargetable || enemy.Targetable.Faction != Faction.Enemy)
            {
                return false;
            }

            Vector3 offset = enemy.Targetable.TargetPoint.position - _muzzle.position;
            return offset.sqrMagnitude <= _range * _range && HasLineOfSight(enemy);
        }

        public bool HasLineOfSight(EnemyHealth enemy)
        {
            if (enemy == null || enemy.Targetable == null || _muzzle == null)
            {
                return false;
            }

            Vector3 offset = enemy.Targetable.TargetPoint.position - _muzzle.position;
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                _muzzle.position,
                offset / distance,
                distance,
                _lineOfSightMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            Targetable nearestTarget = null;
            for (int index = 0; index < hits.Length; index++)
            {
                Collider collider = hits[index].collider;
                if (collider == null || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hits[index].distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hits[index].distance;
                nearestTarget = collider.GetComponentInParent<Targetable>();
                if (nearestTarget == null)
                {
                    return false;
                }
            }

            return nearestTarget == null || nearestTarget == enemy.Targetable;
        }

        public bool TakeDamage(DamageInfo damageInfo)
        {
            if (_deathHandled || _health == null || !_health.TryApplyDamage(damageInfo))
            {
                return false;
            }

            Damaged?.Invoke(damageInfo);
            if (!_health.IsAlive)
            {
                HandleDeath(damageInfo);
            }

            return true;
        }

        private void AcquireTarget()
        {
            _target = null;
            if (_enemyRegistry == null)
            {
                return;
            }

            float nearestSqrDistance = float.PositiveInfinity;
            IReadOnlyList<EnemyHealth> enemies = _enemyRegistry.Enemies;
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyHealth enemy = enemies[index];
                if (!CanTarget(enemy))
                {
                    continue;
                }

                float sqrDistance = (enemy.Targetable.TargetPoint.position - _muzzle.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                _target = enemy;
                nearestSqrDistance = sqrDistance;
            }
        }

        private void Fire(EnemyHealth enemy, float timestamp)
        {
            Vector3 hitPoint = enemy.Targetable.TargetPoint.position;
            if (!enemy.TakeDamage(new DamageInfo(_damage, gameObject, Faction.Factory, hitPoint)))
            {
                return;
            }

            _nextFireTime = timestamp + _shotInterval;
            if (_muzzleFlash != null)
            {
                _muzzleFlash.SetActive(true);
                _muzzleFlashUntil = timestamp + 0.08f;
            }

            SpawnLaserTracer(hitPoint);
            if (_shotAudioSource != null && _shotClip != null && _shotAudioSource.isActiveAndEnabled)
            {
                _shotAudioSource.PlayOneShot(_shotClip);
            }

            Fired?.Invoke(this, enemy);
        }

        private void SpawnLaserTracer(Vector3 hitPoint)
        {
            if (_laserTracerPrefab == null || _muzzle == null || !Application.isPlaying)
            {
                return;
            }

            TurretLaserTracer tracer = Instantiate(
                _laserTracerPrefab,
                _muzzle.position,
                Quaternion.identity);
            tracer.Initialize(_muzzle.position, hitPoint);
        }

        private void HandleDeath(DamageInfo damageInfo)
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            if (_targetable != null)
            {
                _targetable.SetTargetable(false);
            }

            if (_muzzleFlash != null)
            {
                _muzzleFlash.SetActive(false);
            }
            UnregisterTarget();
            Died?.Invoke(damageInfo);
            Destroyed?.Invoke(this);
            if (_owningSpot != null)
            {
                _owningSpot.HandleTurretDestroyed(this);
            }
        }

        private void InitializeComponents()
        {
            ResolveReferences();
            if (_health != null)
            {
                _health.Configure(_maximumHealth);
            }

            if (_factionMember != null)
            {
                _factionMember.Configure(Faction.Factory);
            }

            if (_targetable != null)
            {
                _targetable.Configure(_factionMember, _targetPoint, this, true);
            }
        }

        private void RegisterTarget()
        {
            if (_registered || _factoryRegistry == null || !IsAlive)
            {
                return;
            }

            _factoryRegistry.RegisterTarget(this);
            _registered = true;
        }

        private void UnregisterTarget()
        {
            if (!_registered)
            {
                return;
            }

            if (_factoryRegistry != null)
            {
                _factoryRegistry.UnregisterTarget(this);
            }
            _registered = false;
        }

        private void ResolveReferences()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            if (_factionMember == null)
            {
                _factionMember = GetComponent<FactionMember>();
            }

            if (_targetable == null)
            {
                _targetable = GetComponent<Targetable>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.12f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
