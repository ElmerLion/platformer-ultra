using System;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(FactionMember), typeof(Targetable))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health _health;
        [SerializeField] private FactionMember _factionMember;
        [SerializeField] private Targetable _targetable;
        [SerializeField] private DeathExplosionEmitter _deathExplosion;
        [SerializeField, Min(1)] private int _maximumHealth = 100;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0.35f;

        private bool _deathHandled;

        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
        public int MaximumHealth => _health != null ? _health.MaximumHealth : _maximumHealth;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Targetable Targetable => _targetable;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            ResolveReferences();
            InitializeHealth();
        }

        private void OnValidate()
        {
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _invulnerabilityDuration = Mathf.Max(0f, _invulnerabilityDuration);
            ResolveReferences();
        }

        public void Configure(
            Health health,
            FactionMember factionMember,
            Targetable targetable,
            int maximumHealth = 100,
            float invulnerabilityDuration = 0.35f)
        {
            _health = health;
            _factionMember = factionMember;
            _targetable = targetable;
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
            InitializeHealth();
        }

        public bool TakeDamage(DamageInfo damageInfo)
        {
            if (_deathHandled || _health == null || !_health.TryApplyDamage(damageInfo))
            {
                return false;
            }

            Damaged?.Invoke(damageInfo);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            if (!_health.IsAlive)
            {
                _deathHandled = true;
                _targetable?.SetTargetable(false);
                _deathExplosion?.Play();
                Died?.Invoke(damageInfo);
            }

            return true;
        }

        public void ResetForTesting()
        {
            ResolveReferences();
            _deathHandled = false;
            _health?.RestoreFull();
            _targetable?.SetTargetable(true);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        }

        public void ResetPlayer()
        {
            ResetForTesting();
        }

        public void ConfigureDeathExplosion(DeathExplosionEmitter deathExplosion)
        {
            _deathExplosion = deathExplosion;
        }

        private void InitializeHealth()
        {
            if (_health == null)
            {
                return;
            }

            _health.Configure(_maximumHealth, _invulnerabilityDuration);
            _factionMember?.Configure(Faction.Player);
            _targetable?.SetTargetable(true);
            _deathHandled = false;
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
    }
}
