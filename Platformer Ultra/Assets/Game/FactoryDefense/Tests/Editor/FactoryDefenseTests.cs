using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.FactoryDefense.Tests
{
    public sealed class FactoryDefenseTests
    {
        [Test]
        public void ConstructionCompletion_CreatesAndRegistersOneTurret()
        {
            DefenseFixture fixture = new DefenseFixture();
            GameObject interactor = new GameObject("Interactor");
            try
            {
                Assert.That(fixture.Spot.BeginTimedInteraction(interactor), Is.True);
                Assert.That(fixture.Spot.CompleteTimedInteraction(interactor), Is.True);

                Assert.That(fixture.Spot.IsBuilt, Is.True);
                Assert.That(fixture.FactoryRegistry.Targets, Has.Count.EqualTo(1));
                Assert.That(fixture.DamagedVisual.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(interactor);
                fixture.Dispose();
            }
        }

        [Test]
        public void ConstructionCancellation_PreservesDamagedBuildSpotState()
        {
            DefenseFixture fixture = new DefenseFixture();
            GameObject interactor = new GameObject("Interactor");
            try
            {
                TimedInteractionSession session = new TimedInteractionSession();
                Assert.That(session.TryBegin(fixture.Spot, interactor), Is.True);
                session.Tick(6f, true);
                Assert.That(session.Tick(0f, false), Is.EqualTo(TimedInteractionTickResult.Cancelled));

                Assert.That(fixture.Spot.IsBuilt, Is.False);
                Assert.That(fixture.DamagedVisual.activeSelf, Is.True);
                Assert.That(fixture.Trigger.enabled, Is.True);
                Assert.That(fixture.FactoryRegistry.Targets, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(interactor);
                fixture.Dispose();
            }
        }

        [Test]
        public void TurretTargeting_RejectsFriendlyDeadOutOfRangeAndOccludedTargets()
        {
            DefenseFixture fixture = new DefenseFixture();
            EnemyFixture enemy = new EnemyFixture(fixture.EnemyRegistry, new Vector3(0f, 0f, 5f), 30);
            GameObject occluder = null;
            try
            {
                FactoryTurret turret = fixture.BuildTurret();
                Assert.That(turret.CanTarget(enemy.Enemy), Is.True);

                enemy.Faction.Configure(Faction.Factory);
                Assert.That(turret.CanTarget(enemy.Enemy), Is.False);
                enemy.Faction.Configure(Faction.Enemy);

                enemy.Root.transform.position = new Vector3(0f, 0f, 13f);
                Physics.SyncTransforms();
                Assert.That(turret.CanTarget(enemy.Enemy), Is.False);
                enemy.Root.transform.position = new Vector3(0f, 0f, 5f);

                occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                occluder.transform.position = new Vector3(0f, 0.75f, 2.5f);
                occluder.transform.localScale = new Vector3(2f, 3f, 0.5f);
                Physics.SyncTransforms();
                Assert.That(turret.CanTarget(enemy.Enemy), Is.False);
                Object.DestroyImmediate(occluder);
                occluder = null;

                enemy.Enemy.TakeDamage(new DamageInfo(30, turret.gameObject, Faction.Factory, Vector3.zero));
                Assert.That(turret.CanTarget(enemy.Enemy), Is.False);
            }
            finally
            {
                if (occluder != null)
                {
                    Object.DestroyImmediate(occluder);
                }

                enemy.Dispose();
                fixture.Dispose();
            }
        }

        [Test]
        public void TurretFire_AppliesTenDamageAndRespectsCooldown()
        {
            DefenseFixture fixture = new DefenseFixture();
            EnemyFixture enemy = new EnemyFixture(fixture.EnemyRegistry, new Vector3(0f, 0f, 5f), 30);
            try
            {
                FactoryTurret turret = fixture.BuildTurret();
                Physics.SyncTransforms();

                turret.Tick(1f, 0f);
                Assert.That(enemy.Enemy.CurrentHealth, Is.EqualTo(20));
                turret.Tick(0.1f, 0.1f);
                Assert.That(enemy.Enemy.CurrentHealth, Is.EqualTo(20));
                turret.Tick(1.1f, 1.2f);
                Assert.That(enemy.Enemy.CurrentHealth, Is.EqualTo(10));
            }
            finally
            {
                enemy.Dispose();
                fixture.Dispose();
            }
        }

        [Test]
        public void EnemyTargeting_SelectsReachableTurretThroughFactoryRegistry()
        {
            DefenseFixture fixture = new DefenseFixture();
            GameObject enemyObject = new GameObject("Enemy Brain");
            try
            {
                FactoryTurret turret = fixture.BuildTurret();
                Assert.That(turret.IsEligibleTarget, Is.True);
                Assert.That(fixture.FactoryRegistry.FindNearestEligibleTarget(Vector3.zero), Is.SameAs(turret));
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();
                brain.InitializeRuntime(fixture.FactoryRegistry, null, fixture.EnemyRegistry);

                Assert.That(brain.CurrentFactoryTarget, Is.SameAs(turret));
                Assert.That(brain.CurrentTarget, Is.SameAs(turret.Targetable));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                fixture.Dispose();
            }
        }

        [Test]
        public void EnemyDamage_DestroysTurretAndRestoresRebuildableSpot()
        {
            DefenseFixture fixture = new DefenseFixture();
            GameObject enemyObject = new GameObject("Attacking Enemy");
            try
            {
                FactoryTurret turret = fixture.BuildTurret();
                Assert.That(turret.TakeDamage(new DamageInfo(
                    80,
                    enemyObject,
                    Faction.Enemy,
                    turret.transform.position)), Is.True);

                Assert.That(turret.IsAlive, Is.False);
                Assert.That(fixture.Spot.IsBuilt, Is.False);
                Assert.That(fixture.DamagedVisual.activeSelf, Is.True);
                Assert.That(fixture.Trigger.enabled, Is.True);
                Assert.That(fixture.FactoryRegistry.Targets, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                fixture.Dispose();
            }
        }

        private sealed class DefenseFixture
        {
            private readonly GameObject _turretTemplateRoot;

            public DefenseFixture()
            {
                RegistryRoot = new GameObject("Registries");
                FactoryRegistry = RegistryRoot.AddComponent<MachineTargetRegistry>();
                EnemyRegistry = RegistryRoot.AddComponent<EnemyRuntimeRegistry>();
                _turretTemplateRoot = CreateTurretTemplate();

                SpotRoot = new GameObject("Turret Spot");
                DamagedVisual = new GameObject("Damaged Installation");
                DamagedVisual.transform.SetParent(SpotRoot.transform, false);
                Trigger = SpotRoot.AddComponent<BoxCollider>();
                Trigger.isTrigger = true;
                Spot = SpotRoot.AddComponent<TurretBuildSpot>();
                Spot.Configure(
                    _turretTemplateRoot.GetComponent<FactoryTurret>(),
                    SpotRoot.transform,
                    DamagedVisual,
                    Trigger,
                    EnemyRegistry,
                    FactoryRegistry,
                    12f);
            }

            public GameObject RegistryRoot { get; }
            public GameObject SpotRoot { get; }
            public MachineTargetRegistry FactoryRegistry { get; }
            public EnemyRuntimeRegistry EnemyRegistry { get; }
            public TurretBuildSpot Spot { get; }
            public GameObject DamagedVisual { get; }
            public BoxCollider Trigger { get; }

            public FactoryTurret BuildTurret()
            {
                GameObject interactor = new GameObject("Builder");
                try
                {
                    Assert.That(Spot.CompleteTimedInteraction(interactor), Is.True);
                    return Spot.BuiltTurret;
                }
                finally
                {
                    Object.DestroyImmediate(interactor);
                }
            }

            public void Dispose()
            {
                Object.DestroyImmediate(SpotRoot);
                Object.DestroyImmediate(_turretTemplateRoot);
                Object.DestroyImmediate(RegistryRoot);
            }

            private static GameObject CreateTurretTemplate()
            {
                GameObject root = new GameObject("Turret Template");
                Health health = root.AddComponent<Health>();
                FactionMember faction = root.AddComponent<FactionMember>();
                Targetable targetable = root.AddComponent<Targetable>();
                FactoryTurret turret = root.AddComponent<FactoryTurret>();

                GameObject yaw = new GameObject("Yaw");
                yaw.transform.SetParent(root.transform, false);
                GameObject muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(yaw.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.75f, 0.5f);
                GameObject targetPointObject = new GameObject("Target Point");
                targetPointObject.transform.SetParent(root.transform, false);
                targetPointObject.transform.localPosition = Vector3.up;
                TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();

                targetable.Configure(faction, targetPoint, turret, true);
                turret.Configure(
                    health,
                    faction,
                    targetable,
                    targetPoint,
                    yaw.transform,
                    muzzle.transform,
                    null,
                    ~0,
                    80,
                    12f,
                    10,
                    1.2f,
                    360f,
                    5f,
                    0.2f);
                return root;
            }
        }

        private sealed class EnemyFixture
        {
            private readonly EnemyDefinition _definition;

            public EnemyFixture(EnemyRuntimeRegistry registry, Vector3 position, int healthValue)
            {
                Root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Root.name = "Enemy";
                Root.transform.position = position;
                Health health = Root.AddComponent<Health>();
                Faction = Root.AddComponent<FactionMember>();
                Targetable targetable = Root.AddComponent<Targetable>();
                Enemy = Root.AddComponent<EnemyHealth>();
                GameObject pointObject = new GameObject("Target Point");
                pointObject.transform.SetParent(Root.transform, false);
                pointObject.transform.localPosition = Vector3.up;
                TargetPoint targetPoint = pointObject.AddComponent<TargetPoint>();

                _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                _definition.ConfigureMovement(healthValue, 2f, 3f, 10f, 10f, 360f, 0f, 0f, 1f);
                targetable.Configure(Faction, targetPoint, Enemy, true);
                Enemy.Configure(_definition, health, Faction, targetable, null);
                Enemy.InitializeRuntime(registry);
                Physics.SyncTransforms();
            }

            public GameObject Root { get; }
            public EnemyHealth Enemy { get; }
            public FactionMember Faction { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(_definition);
            }
        }
    }
}
