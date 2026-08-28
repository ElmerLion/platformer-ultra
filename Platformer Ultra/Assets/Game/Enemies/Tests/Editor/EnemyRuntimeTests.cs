using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using System.Reflection;
using UnityEngine;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class EnemyRuntimeTests
    {
        private GameObject _attackerObject;
        private GameObject _targetObject;
        private GameObject _auxiliaryObject;
        private EnemyDefinition _definition;

        [TearDown]
        public void TearDown()
        {
            if (_attackerObject != null)
            {
                Object.DestroyImmediate(_attackerObject);
            }

            if (_targetObject != null)
            {
                Object.DestroyImmediate(_targetObject);
            }

            if (_auxiliaryObject != null)
            {
                Object.DestroyImmediate(_auxiliaryObject);
            }

            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
            }
        }

        [Test]
        public void AttackImpact_AppliesCapturedDamageOnlyOnce()
        {
            EnemyAttackController attack = CreateConfiguredAttackController(12, out Targetable target, out PlayerHealth damageable);
            int damageCallCount = 0;
            int presentationImpactCount = 0;
            damageable.Damaged += _ => damageCallCount++;
            attack.AttackImpacted += (_, _) => presentationImpactCount++;

            Assert.That(attack.TryBeginAttack(target, false, 0f), Is.True);
            attack.OnAttackImpact();
            attack.OnAttackImpact();

            Assert.That(attack.ImpactApplied, Is.True);
            Assert.That(damageCallCount, Is.EqualTo(1));
            Assert.That(presentationImpactCount, Is.EqualTo(1));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(88));
        }

        [Test]
        public void AttackImpact_InvalidatedCapturedTargetCannotDamageLater()
        {
            EnemyAttackController attack = CreateConfiguredAttackController(12, out Targetable target, out PlayerHealth damageable);

            Assert.That(attack.TryBeginAttack(target, false, 0f), Is.True);
            target.SetTargetable(false);
            attack.OnAttackImpact();
            target.SetTargetable(true);
            attack.OnAttackImpact();

            Assert.That(attack.ImpactApplied, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void MeleeImpact_TargetMovedBeyondRange_DoesNotApplyDamage()
        {
            EnemyAttackController attack = CreateConfiguredAttackController(12, out Targetable target, out PlayerHealth damageable);

            target.transform.position = Vector3.right;
            Assert.That(attack.TryBeginAttack(target, false, 0f), Is.True);
            target.transform.position = Vector3.right * 10f;
            attack.OnAttackImpact();

            Assert.That(attack.ImpactApplied, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void MeleeImpact_ElevatedTargetWithinPlanarRange_AppliesDamage()
        {
            EnemyAttackController attack = CreateConfiguredAttackController(12, out Targetable target, out PlayerHealth damageable);

            target.transform.position = new Vector3(2.1f, 1.7f, 0f);
            Assert.That(attack.TryBeginAttack(target, false, 0f), Is.True);
            attack.OnAttackImpact();

            Assert.That(damageable.CurrentHealth, Is.EqualTo(88));
        }

        [Test]
        public void ArmoredSpecial_FailedLandingDoesNotStartOrConsumeCooldown()
        {
            EnemyAttackController attack = CreateConfiguredSpecialAttackController(
                out Targetable target,
                out _,
                out TestEnemyMotor motor);
            motor.CanResolveLanding = false;

            Assert.That(attack.TryBeginAttack(target, true, 4f), Is.False);
            Assert.That(attack.IsAttacking, Is.False);
            Assert.That(float.IsNegativeInfinity(attack.LastSpecialAttackTime), Is.True);
            Assert.That(motor.BeginScriptedMotionCount, Is.Zero);

            motor.CanResolveLanding = true;
            Assert.That(attack.TryBeginAttack(target, true, 4f), Is.True);
            Assert.That(attack.LastSpecialAttackTime, Is.EqualTo(4f));
            Assert.That(motor.BeginScriptedMotionCount, Is.EqualTo(1));
        }

        [Test]
        public void ArmoredSpecial_CapturedTargetOutsideImpactRadius_DoesNotReceiveFallbackDamage()
        {
            EnemyAttackController attack = CreateConfiguredSpecialAttackController(
                out Targetable target,
                out PlayerHealth damageable,
                out _);

            Assert.That(attack.TryBeginAttack(target, true, 0f), Is.True);
            target.transform.position = Vector3.right * 10f;
            attack.OnSpecialImpact();

            Assert.That(attack.ImpactApplied, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void AttackController_DisabledDuringLeap_CancelsAndCleansScriptedMotion()
        {
            EnemyAttackController attack = CreateConfiguredSpecialAttackController(
                out Targetable target,
                out _,
                out TestEnemyMotor motor);

            Assert.That(attack.TryBeginAttack(target, true, 0f), Is.True);
            InvokeLifecycle(attack, "OnDisable");

            Assert.That(attack.IsAttacking, Is.False);
            Assert.That(motor.EndScriptedMotionCount, Is.EqualTo(1));
            Assert.That(motor.IsInScriptedMotion, Is.False);
        }

        [Test]
        public void DroneProjectile_TargetDodgesAfterLaunch_MissesCapturedAimPoint()
        {
            _attackerObject = new GameObject("Projectile");
            EnemyProjectile projectile = _attackerObject.AddComponent<EnemyProjectile>();
            Targetable target = CreateTarget(new Vector3(2f, 0f, 0f), out PlayerHealth damageable);
            _auxiliaryObject = new GameObject("Drone Source");

            projectile.Initialize(_auxiliaryObject, target, 8, 10f, 5f);
            target.transform.position = new Vector3(0f, 0f, 2f);
            projectile.Tick(0.25f, Time.time);

            Assert.That(projectile.IsResolved, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void DroneStop_ZerosVelocityAndKeepsRootStationary()
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureMovement(30, 3.8f, 3.8f, 20f, 20f, 300f, 0f, 0.12f, 1.4f);
            _attackerObject = new GameObject("Drone");
            _attackerObject.AddComponent<BoxCollider>().size = Vector3.one;
            DroneFlightMotor motor = _attackerObject.AddComponent<DroneFlightMotor>();
            motor.Configure(_definition);
            motor.ConfigureVisual(null, 0);
            motor.TryPlace(Vector3.zero, 1f);
            motor.SetDestination(Vector3.right * 10f, 0.1f, false);
            motor.Tick(0.25f, 0f);

            Assert.That(motor.Velocity.sqrMagnitude, Is.GreaterThan(0f));
            motor.Stop();
            Vector3 stoppedPosition = motor.transform.position;
            Quaternion stoppedRotation = motor.transform.rotation;
            motor.Tick(1f, 1f);

            Assert.That(motor.Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(motor.transform.position, Is.EqualTo(stoppedPosition));
            Assert.That(motor.transform.rotation, Is.EqualTo(stoppedRotation));
        }

        [Test]
        public void DroneFullBodyCast_HeadOnWallSteersWithoutPhasingThrough()
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureMovement(30, 3.8f, 3.8f, 100f, 100f, 300f, 0f, 0f, 0f);
            _attackerObject = new GameObject("Drone");
            _attackerObject.AddComponent<BoxCollider>().size = Vector3.one;
            DroneFlightMotor motor = _attackerObject.AddComponent<DroneFlightMotor>();
            motor.Configure(_definition);
            motor.ConfigureVisual(null, ~0);
            motor.TryPlace(Vector3.zero, 1f);

            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject.name = "Wall";
            _targetObject.transform.position = new Vector3(2f, 0f, 0f);
            _targetObject.transform.localScale = new Vector3(0.2f, 4f, 10f);
            Physics.SyncTransforms();

            motor.SetDestination(Vector3.right * 10f, 0.1f, false);
            motor.Tick(1f, 0f);

            Assert.That(motor.transform.position.x, Is.LessThan(1.5f));
            Assert.That(Mathf.Abs(motor.transform.position.z), Is.GreaterThan(0.1f));
        }

        [Test]
        public void ArmoredSpecialPolicy_RequiresCooldownDistanceAndReasonableLanding()
        {
            Assert.That(ArmoredSpecialAttackPolicy.IsEligible(10f, 2f, 7f, 5f, 3f, 7f, true), Is.True);
            Assert.That(ArmoredSpecialAttackPolicy.IsEligible(8.99f, 2f, 7f, 5f, 3f, 7f, true), Is.False);
            Assert.That(ArmoredSpecialAttackPolicy.IsEligible(10f, 2f, 7f, 2.99f, 3f, 7f, true), Is.False);
            Assert.That(ArmoredSpecialAttackPolicy.IsEligible(10f, 2f, 7f, 7.01f, 3f, 7f, true), Is.False);
            Assert.That(ArmoredSpecialAttackPolicy.IsEligible(10f, 2f, 7f, 5f, 3f, 7f, false), Is.False);
        }

        [Test]
        public void SpawnWeights_ExcludeArmoredUntilUnlockedAndUseConfiguredWeights()
        {
            EnemySpawnWeight[] weights =
            {
                new EnemySpawnWeight(EnemyArchetype.Drone, 1f),
                new EnemySpawnWeight(EnemyArchetype.Saboteur, 3f),
                new EnemySpawnWeight(EnemyArchetype.Armored, 6f)
            };

            Assert.That(
                EnemySpawnPoint.TryChooseArchetype(weights, false, 0.99f, out EnemyArchetype lockedChoice),
                Is.True);
            Assert.That(lockedChoice, Is.EqualTo(EnemyArchetype.Saboteur));

            Assert.That(
                EnemySpawnPoint.TryChooseArchetype(weights, true, 0.5f, out EnemyArchetype unlockedChoice),
                Is.True);
            Assert.That(unlockedChoice, Is.EqualTo(EnemyArchetype.Armored));
        }

        [Test]
        public void SpawnWeightSelection_RejectsMissingOrZeroWeightTables()
        {
            Assert.That(
                EnemySpawnPoint.TryChooseArchetype(null, true, 0.5f, out _),
                Is.False);
            Assert.That(
                EnemySpawnPoint.TryChooseArchetype(
                    new[] { new EnemySpawnWeight(EnemyArchetype.Armored, 0f) },
                    true,
                    0.5f,
                    out _),
                Is.False);
        }

        [TestCase(0, 6, true)]
        [TestCase(5, 6, true)]
        [TestCase(6, 6, false)]
        [TestCase(7, 6, false)]
        [TestCase(0, 0, false)]
        public void ActiveCapLogic_IsStrictlyBelowConfiguredCap(int active, int cap, bool expected)
        {
            Assert.That(EnemySpawnManager.CanSpawnForCap(active, cap), Is.EqualTo(expected));
        }

        [Test]
        public void EnemySpawning_UnlocksPersistentlyWhenSmelterBecomesOperational()
        {
            GameObject terminalObject = new GameObject("Smelter Activation Terminal");
            GameObject managerObject = new GameObject("Enemy Spawn Manager");
            try
            {
                FactoryObjectiveTerminal smelter = terminalObject.AddComponent<FactoryObjectiveTerminal>();
                smelter.Configure(
                    "Smelter",
                    null,
                    null,
                    System.Array.Empty<GameObject>(),
                    System.Array.Empty<Light>(),
                    System.Array.Empty<PlatformerUltra.Factory.Conveyors.ConveyorBelt>(),
                    false);

                EnemySpawnManager manager = managerObject.AddComponent<EnemySpawnManager>();
                manager.Configure(
                    null,
                    null,
                    null,
                    System.Array.Empty<EnemySpawnPoint>(),
                    null,
                    null,
                    null,
                    smelter,
                    null,
                    6f,
                    12f,
                    18f,
                    6,
                    90f);

                Assert.That(smelter.IsOperational, Is.False);
                Assert.That(manager.SpawningUnlocked, Is.False);
                smelter.Activate();
                Assert.That(smelter.IsOperational, Is.True);
                Assert.That(manager.SpawningUnlocked, Is.True);

                manager.SetSpawningEnabled(false);
                Assert.That(manager.SpawningEnabled, Is.False);
                Assert.That(manager.SpawningUnlocked, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(terminalObject);
            }
        }

        [Test]
        public void EnemyRuntimeRegistry_DeduplicatesAndUnregistersImmediately()
        {
            GameObject registryObject = new GameObject("Registry");
            GameObject enemyObject = new GameObject("Enemy");
            try
            {
                EnemyRuntimeRegistry registry = registryObject.AddComponent<EnemyRuntimeRegistry>();
                EnemyHealth enemy = enemyObject.AddComponent<EnemyHealth>();

                registry.Register(enemy);
                registry.Register(enemy);
                Assert.That(registry.ActiveCount, Is.EqualTo(1));

                registry.Unregister(enemy);
                Assert.That(registry.ActiveCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(registryObject);
            }
        }

        [Test]
        public void EnemyHealth_DamageDeathAndRemovalEventsFireOnce()
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureIdentity(EnemyArchetype.Drone, null, null, null, null, 1f);
            _definition.ConfigureMovement(30, 3.8f, 3.8f, 7f, 10f, 300f, 1.85f, 0.12f, 1.4f);

            _attackerObject = new GameObject("Enemy");
            EnemyHealth enemy = _attackerObject.AddComponent<EnemyHealth>();
            Health health = _attackerObject.GetComponent<Health>();
            FactionMember faction = _attackerObject.GetComponent<FactionMember>();
            Targetable targetable = _attackerObject.GetComponent<Targetable>();
            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(_attackerObject.transform, false);
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();
            targetable.Configure(faction, targetPoint, enemy, true);
            enemy.Configure(_definition, health, faction, targetable, null);

            _targetObject = new GameObject("Enemy Registry");
            EnemyRuntimeRegistry registry = _targetObject.AddComponent<EnemyRuntimeRegistry>();
            enemy.InitializeRuntime(registry);

            int damagedCount = 0;
            int diedCount = 0;
            int removedCount = 0;
            enemy.Damaged += _ => damagedCount++;
            enemy.Died += _ => diedCount++;
            enemy.Removed += _ => removedCount++;

            Assert.That(enemy.TakeDamage(new DamageInfo(10, null, Faction.Factory, Vector3.zero)), Is.True);
            Assert.That(enemy.TakeDamage(new DamageInfo(20, null, Faction.Factory, Vector3.zero)), Is.True);
            Assert.That(enemy.TakeDamage(new DamageInfo(1, null, Faction.Factory, Vector3.zero)), Is.False);

            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(targetable.IsTargetable, Is.False);
            Assert.That(registry.ActiveCount, Is.Zero);
            Assert.That(damagedCount, Is.EqualTo(2));
            Assert.That(diedCount, Is.EqualTo(1));
            Assert.That(removedCount, Is.EqualTo(1));
        }

        [Test]
        public void EnemyHealth_DisableEnable_ReregistersAndNotifiesOncePerActiveLifecycle()
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureIdentity(EnemyArchetype.Drone, null, null, null, null, 1f);
            _definition.ConfigureMovement(30, 3.8f, 3.8f, 7f, 10f, 300f, 1.85f, 0.12f, 1.4f);

            _attackerObject = new GameObject("Enemy");
            EnemyHealth enemy = _attackerObject.AddComponent<EnemyHealth>();
            Health health = _attackerObject.GetComponent<Health>();
            FactionMember faction = _attackerObject.GetComponent<FactionMember>();
            Targetable targetable = _attackerObject.GetComponent<Targetable>();
            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(_attackerObject.transform, false);
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();
            targetable.Configure(faction, targetPoint, enemy, true);
            enemy.Configure(_definition, health, faction, targetable, null);

            _targetObject = new GameObject("Enemy Registry");
            EnemyRuntimeRegistry registry = _targetObject.AddComponent<EnemyRuntimeRegistry>();
            int removedCount = 0;
            enemy.Removed += _ => removedCount++;
            enemy.InitializeRuntime(registry);
            enemy.InitializeRuntime(registry);

            Assert.That(registry.ActiveCount, Is.EqualTo(1));
            InvokeLifecycle(enemy, "OnDisable");
            Assert.That(registry.ActiveCount, Is.Zero);
            Assert.That(removedCount, Is.EqualTo(1));

            InvokeLifecycle(enemy, "OnEnable");
            Assert.That(registry.ActiveCount, Is.EqualTo(1));
            InvokeLifecycle(enemy, "OnDisable");
            Assert.That(registry.ActiveCount, Is.Zero);
            Assert.That(removedCount, Is.EqualTo(2));
        }

        [Test]
        public void EnemyDefinition_ClampsDisengageAndLeapRanges()
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureTargeting(8f, 4f, 1f, 2f, 2f);
            _definition.ConfigureSpecial(0.225f, 7f, 7f, 3f, 2f, 35, 40, 3.7f, 1.4f);

            Assert.That(_definition.PlayerDisengageDistance, Is.EqualTo(8f));
            Assert.That(_definition.MaximumLeapDistance, Is.EqualTo(7f));
            Assert.That(_definition.SpecialChance, Is.EqualTo(0.225f).Within(0.0001f));
        }

        private EnemyAttackController CreateConfiguredAttackController(
            int playerDamage,
            out Targetable target,
            out PlayerHealth damageable)
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureIdentity(EnemyArchetype.Saboteur, null, null, null, null, 1f);
            _definition.ConfigureRegularAttack(1.1f, 2.633f, 0.46f, playerDamage, 16, 0f, 12f);

            _attackerObject = new GameObject("Attacker");
            EnemyAttackController attack = _attackerObject.AddComponent<EnemyAttackController>();
            attack.Configure(_definition, null, null, null, null, ~0);

            target = CreateTarget(Vector3.zero, out damageable);
            return attack;
        }

        private EnemyAttackController CreateConfiguredSpecialAttackController(
            out Targetable target,
            out PlayerHealth damageable,
            out TestEnemyMotor motor)
        {
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition.ConfigureIdentity(EnemyArchetype.Armored, null, null, null, null, 1f);
            _definition.ConfigureMovement(180, 1.8f, 1.8f, 8f, 12f, 360f, 0f, 0f, 0f);
            _definition.ConfigureTargeting(6f, 10f, 1f, 2.4f, 2.4f);
            _definition.ConfigureRegularAttack(1.8f, 1f, 0.6f, 22, 28, 0f, 12f);
            _definition.ConfigureSpecial(0.225f, 7f, 3f, 7f, 2f, 35, 40, 1.4f, 1.4f);

            _attackerObject = new GameObject("Armored Attacker");
            motor = _attackerObject.AddComponent<TestEnemyMotor>();
            EnemyAttackController attack = _attackerObject.AddComponent<EnemyAttackController>();
            attack.Configure(_definition, null, motor, null, null, ~0);
            target = CreateTarget(Vector3.right * 5f, out damageable);
            return attack;
        }

        private Targetable CreateTarget(Vector3 position, out PlayerHealth damageable)
        {
            _targetObject = new GameObject("Target");
            _targetObject.transform.position = position;
            Health health = _targetObject.AddComponent<Health>();
            FactionMember faction = _targetObject.AddComponent<FactionMember>();
            Targetable target = _targetObject.AddComponent<Targetable>();
            damageable = _targetObject.AddComponent<PlayerHealth>();

            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(_targetObject.transform, false);
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();
            target.Configure(faction, targetPoint, damageable, true);
            damageable.Configure(health, faction, target, 100, 0f);
            return target;
        }

        private static void InvokeLifecycle(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {component.GetType().Name}.{methodName} to exist.");
            method.Invoke(component, null);
        }
    }
}
