using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyRuntimeRegistry : MonoBehaviour
    {
        private readonly List<EnemyHealth> _enemies = new List<EnemyHealth>();

        public IReadOnlyList<EnemyHealth> Enemies => _enemies;
        public int ActiveCount => _enemies.Count;

        public event Action<EnemyHealth> EnemyRegistered;
        public event Action<EnemyHealth> EnemyUnregistered;

        public void Register(EnemyHealth enemy)
        {
            if (enemy == null || _enemies.Contains(enemy))
            {
                return;
            }

            _enemies.Add(enemy);
            EnemyRegistered?.Invoke(enemy);
        }

        public void Unregister(EnemyHealth enemy)
        {
            if (enemy == null || !_enemies.Remove(enemy))
            {
                return;
            }

            EnemyUnregistered?.Invoke(enemy);
        }

        private void LateUpdate()
        {
            for (int index = _enemies.Count - 1; index >= 0; index--)
            {
                EnemyHealth enemy = _enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    _enemies.RemoveAt(index);
                    EnemyUnregistered?.Invoke(enemy);
                }
            }
        }
    }
}
