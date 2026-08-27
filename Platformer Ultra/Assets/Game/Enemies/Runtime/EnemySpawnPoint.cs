using System;
using System.Collections.Generic;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [Serializable]
    public struct EnemySpawnWeight
    {
        [SerializeField] private EnemyArchetype _archetype;
        [SerializeField, Min(0f)] private float _weight;

        public EnemySpawnWeight(EnemyArchetype archetype, float weight)
        {
            _archetype = archetype;
            _weight = Mathf.Max(0f, weight);
        }

        public EnemyArchetype Archetype => _archetype;
        public float Weight => _weight;
    }

    [DisallowMultipleComponent]
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private EnemySpawnWeight[] _weights = Array.Empty<EnemySpawnWeight>();
        [SerializeField, Min(0.1f)] private float _clearanceRadius = 1.2f;
        [SerializeField, Min(0.2f)] private float _clearanceHeight = 3.2f;
        [SerializeField, Min(0f)] private float _playerSafetyDistance = 5f;
        [SerializeField] private LayerMask _enemyLayerMask;

        public Vector3 SpawnPosition => transform.position;
        public Quaternion SpawnRotation => Quaternion.LookRotation(transform.forward, Vector3.up);
        public float ClearanceRadius => _clearanceRadius;
        public IReadOnlyList<EnemySpawnWeight> Weights => _weights;

        public void Configure(
            EnemySpawnWeight[] weights,
            float clearanceRadius,
            float clearanceHeight,
            float playerSafetyDistance,
            LayerMask enemyLayerMask)
        {
            _weights = weights ?? Array.Empty<EnemySpawnWeight>();
            _clearanceRadius = Mathf.Max(0.1f, clearanceRadius);
            _clearanceHeight = Mathf.Max(_clearanceRadius * 2f, clearanceHeight);
            _playerSafetyDistance = Mathf.Max(0f, playerSafetyDistance);
            _enemyLayerMask = enemyLayerMask;
        }

        public bool CanSpawn(Targetable player)
        {
            if (player != null && player.IsTargetable)
            {
                float safetyDistanceSquared = _playerSafetyDistance * _playerSafetyDistance;
                if ((player.TargetPoint.position - transform.position).sqrMagnitude < safetyDistanceSquared)
                {
                    return false;
                }
            }

            Vector3 bottom = transform.position + Vector3.up * _clearanceRadius;
            Vector3 top = transform.position + Vector3.up * (_clearanceHeight - _clearanceRadius);
            return !Physics.CheckCapsule(
                bottom,
                top,
                _clearanceRadius,
                _enemyLayerMask,
                QueryTriggerInteraction.Ignore);
        }

        public bool TryChooseArchetype(bool armoredAllowed, float sample, out EnemyArchetype archetype)
        {
            return TryChooseArchetype(_weights, armoredAllowed, sample, out archetype);
        }

        public static bool TryChooseArchetype(
            IReadOnlyList<EnemySpawnWeight> weights,
            bool armoredAllowed,
            float sample,
            out EnemyArchetype archetype)
        {
            archetype = EnemyArchetype.Drone;
            if (weights == null)
            {
                return false;
            }

            float total = 0f;
            for (int index = 0; index < weights.Count; index++)
            {
                EnemySpawnWeight entry = weights[index];
                if ((armoredAllowed || entry.Archetype != EnemyArchetype.Armored) && entry.Weight > 0f)
                {
                    total += entry.Weight;
                }
            }

            if (total <= 0f)
            {
                return false;
            }

            float cursor = Mathf.Clamp01(sample) * total;
            EnemyArchetype lastEligible = EnemyArchetype.Drone;
            for (int index = 0; index < weights.Count; index++)
            {
                EnemySpawnWeight entry = weights[index];
                if ((!armoredAllowed && entry.Archetype == EnemyArchetype.Armored) || entry.Weight <= 0f)
                {
                    continue;
                }

                lastEligible = entry.Archetype;
                cursor -= entry.Weight;
                if (cursor <= 0f)
                {
                    archetype = entry.Archetype;
                    return true;
                }
            }

            archetype = lastEligible;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * (_clearanceHeight * 0.5f), _clearanceRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, transform.forward * 2f);
        }
    }
}
