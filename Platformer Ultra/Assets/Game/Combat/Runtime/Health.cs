using System;
using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _maximumHealth = 100;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration;
        [SerializeField] private bool _restoreOnEnable;

        private int _currentHealth;
        private float _invulnerableUntil = float.NegativeInfinity;
        private bool _initialized;

        public int CurrentHealth => _currentHealth;
        public int MaximumHealth => _maximumHealth;
        public bool IsAlive => _initialized && _currentHealth > 0;
        public float InvulnerabilityDuration => _invulnerabilityDuration;

        public event Action<int, int> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (_restoreOnEnable)
            {
                RestoreFull();
            }
            else
            {
                EnsureInitialized();
            }
        }

        private void OnValidate()
        {
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _invulnerabilityDuration = Mathf.Max(0f, _invulnerabilityDuration);
            if (!Application.isPlaying && _initialized)
            {
                _currentHealth = Mathf.Clamp(_currentHealth, 0, _maximumHealth);
            }
        }

        public void Configure(int maximumHealth, float invulnerabilityDuration = 0f, bool restoreOnEnable = false)
        {
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
            _restoreOnEnable = restoreOnEnable;
            _initialized = true;
            _currentHealth = _maximumHealth;
            _invulnerableUntil = float.NegativeInfinity;
            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
        }

        public bool TryApplyDamage(DamageInfo damageInfo)
        {
            return TryApplyDamage(damageInfo, Time.time);
        }

        public bool TryApplyDamage(DamageInfo damageInfo, float timestamp)
        {
            EnsureInitialized();
            if (_currentHealth <= 0 || damageInfo.Amount <= 0 || timestamp < _invulnerableUntil)
            {
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
            _invulnerableUntil = timestamp + _invulnerabilityDuration;
            Damaged?.Invoke(damageInfo);
            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
            if (_currentHealth == 0)
            {
                Died?.Invoke(damageInfo);
            }

            return true;
        }

        public void RestoreFull()
        {
            _initialized = true;
            _currentHealth = _maximumHealth;
            _invulnerableUntil = float.NegativeInfinity;
            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
        }

        public void SetCurrentHealth(int health)
        {
            _initialized = true;
            _currentHealth = Mathf.Clamp(health, 0, _maximumHealth);
            _invulnerableUntil = float.NegativeInfinity;
            HealthChanged?.Invoke(_currentHealth, _maximumHealth);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _currentHealth = Mathf.Max(1, _maximumHealth);
        }
    }
}
