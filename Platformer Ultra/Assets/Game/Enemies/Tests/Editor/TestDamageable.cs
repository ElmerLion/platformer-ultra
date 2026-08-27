using System;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class TestDamageable : MonoBehaviour, IDamageable
    {
        private int _currentHealth;

        public int CurrentHealth => _currentHealth;
        public int MaximumHealth { get; private set; }
        public bool IsAlive => _currentHealth > 0;
        public int DamageCallCount { get; private set; }

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        public void Configure(int maximumHealth)
        {
            MaximumHealth = Mathf.Max(1, maximumHealth);
            _currentHealth = MaximumHealth;
            DamageCallCount = 0;
        }

        public bool TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || damageInfo.Amount <= 0)
            {
                return false;
            }

            DamageCallCount++;
            _currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
            Damaged?.Invoke(damageInfo);
            if (_currentHealth == 0)
            {
                Died?.Invoke(damageInfo);
            }

            return true;
        }
    }
}
