using System;
using System.Collections;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(FactionMember), typeof(Targetable))]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private Health _health;
        [SerializeField] private FactionMember _factionMember;
        [SerializeField] private Targetable _targetable;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private DeathExplosionEmitter _deathExplosion;

        private EnemyRuntimeRegistry _registry;
        private bool _deathHandled;
        private bool _removalNotified;
        private bool _runtimeInitialized;
        private bool _activeRuntimeLifecycle;
        private bool _registered;

        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
        public int MaximumHealth => _health != null ? _health.MaximumHealth : 0;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Targetable Targetable => _targetable;
        public EnemyDefinition Definition => _definition;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<EnemyHealth> Removed;

        private void Awake()
        {
            ResolveReferences();
            InitializeHealth();
        }

        private void OnEnable()
        {
            if (!_runtimeInitialized || _deathHandled || !IsAlive)
            {
                return;
            }

            _removalNotified = false;
            _activeRuntimeLifecycle = true;
            RegisterWithRuntimeRegistry();
        }

        private void OnDisable()
        {
            if (!_runtimeInitialized || !_activeRuntimeLifecycle)
            {
                return;
            }

            UnregisterFromRuntimeRegistry();
            _activeRuntimeLifecycle = false;
            NotifyRemoved();
        }

        public void Configure(
            EnemyDefinition definition,
            Health health,
            FactionMember factionMember,
            Targetable targetable,
            EnemyBrain brain)
        {
            _definition = definition;
            _health = health;
            _factionMember = factionMember;
            _targetable = targetable;
            _brain = brain;
            InitializeHealth();
        }

        public void InitializeRuntime(EnemyRuntimeRegistry registry)
        {
            UnregisterFromRuntimeRegistry();
            _registry = registry;
            _runtimeInitialized = true;
            _deathHandled = false;
            _removalNotified = false;
            _activeRuntimeLifecycle = false;
            InitializeHealth();
            if (_health == null || _definition == null)
            {
                Debug.LogError($"{name} cannot initialize enemy health without a Health component and EnemyDefinition.", this);
                return;
            }

            _health.RestoreFull();
            _targetable?.SetTargetable(true);
            if (isActiveAndEnabled)
            {
                _activeRuntimeLifecycle = true;
                RegisterWithRuntimeRegistry();
            }
        }

        public void ConfigureDeathExplosion(DeathExplosionEmitter deathExplosion)
        {
            _deathExplosion = deathExplosion;
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

        private void HandleDeath(DamageInfo damageInfo)
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            _targetable?.SetTargetable(false);
            _brain?.Die();
            UnregisterFromRuntimeRegistry();
            _activeRuntimeLifecycle = false;
            _deathExplosion?.Play();
            Died?.Invoke(damageInfo);
            NotifyRemoved();

            if (Application.isPlaying)
            {
                StartCoroutine(RemoveAfterDelay());
            }
        }

        private IEnumerator RemoveAfterDelay()
        {
            float delay = _definition != null ? _definition.DeathRemovalDelay : 1.5f;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Destroy(gameObject);
        }

        private void InitializeHealth()
        {
            if (_health == null || _definition == null)
            {
                return;
            }

            _health.Configure(_definition.MaximumHealth);
            _factionMember?.Configure(Faction.Enemy);
            if (_targetable != null)
            {
                _targetable.SetTargetable(true);
            }
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

            if (_brain == null)
            {
                _brain = GetComponent<EnemyBrain>();
            }
        }

        private void NotifyRemoved()
        {
            if (_removalNotified)
            {
                return;
            }

            _removalNotified = true;
            Removed?.Invoke(this);
        }

        private void RegisterWithRuntimeRegistry()
        {
            if (_registered || _registry == null)
            {
                return;
            }

            _registry.Register(this);
            _registered = true;
        }

        private void UnregisterFromRuntimeRegistry()
        {
            if (!_registered)
            {
                return;
            }

            _registry?.Unregister(this);
            _registered = false;
        }
    }
}
