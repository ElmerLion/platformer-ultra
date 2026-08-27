using NUnit.Framework;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class EnemyDeathPresentationTests
    {
        [Test]
        public void EnemyDeath_SpawnsConfiguredExplosionExactlyOnce()
        {
            GameObject enemyObject = new GameObject("Enemy");
            GameObject effectTemplate = new GameObject("Shared Mechanical Death Explosion");
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            GameObject spawnedEffect = null;
            try
            {
                definition.ConfigureMovement(20, 2f, 3f, 10f, 10f, 360f, 0f, 0f, 1f);
                Health health = enemyObject.AddComponent<Health>();
                FactionMember faction = enemyObject.AddComponent<FactionMember>();
                Targetable targetable = enemyObject.AddComponent<Targetable>();
                EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
                GameObject pointObject = new GameObject("Target Point");
                pointObject.transform.SetParent(enemyObject.transform, false);
                TargetPoint targetPoint = pointObject.AddComponent<TargetPoint>();
                targetable.Configure(faction, targetPoint, enemyHealth, true);
                enemyHealth.Configure(definition, health, faction, targetable, null);
                DeathExplosionEmitter emitter = enemyObject.AddComponent<DeathExplosionEmitter>();
                emitter.Configure(effectTemplate, targetPoint.transform, 1f);
                enemyHealth.ConfigureDeathExplosion(emitter);

                Assert.That(enemyHealth.TakeDamage(new DamageInfo(
                    20,
                    null,
                    Faction.Factory,
                    enemyObject.transform.position)), Is.True);
                spawnedEffect = emitter.LastSpawnedEffect;
                Assert.That(enemyHealth.IsAlive, Is.False);
                Assert.That(emitter.SpawnCount, Is.EqualTo(1));
                Assert.That(spawnedEffect, Is.Not.Null);
                Assert.That(enemyHealth.TakeDamage(new DamageInfo(
                    1,
                    null,
                    Faction.Factory,
                    enemyObject.transform.position)), Is.False);
                Assert.That(emitter.SpawnCount, Is.EqualTo(1));
            }
            finally
            {
                if (spawnedEffect != null)
                {
                    Object.DestroyImmediate(spawnedEffect);
                }

                Object.DestroyImmediate(effectTemplate);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(definition);
            }
        }
    }
}
