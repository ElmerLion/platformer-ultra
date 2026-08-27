using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class EnemyTargetingBehaviorTests
    {
        private readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void MachineSelection_ChoosesNearestEligibleMachine()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth nearMachine = CreateMachine("Near", new Vector3(4f, 0f, 0f), registry);
            CreateMachine("Far", new Vector3(9f, 0f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(40f, 0f, 0f));

            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            Assert.That(brain.CurrentMachineTarget, Is.SameAs(nearMachine));
            Assert.That(brain.State, Is.EqualTo(EnemyState.MoveToMachine));
        }

        [Test]
        public void PlayerOverride_UsesHysteresisThenResumesPreviousMachine()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth machine = CreateMachine("Mine", new Vector3(4f, 0f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(5f, 0f, 0f));
            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            brain.Tick(0.02f, 0f, 1f);
            Assert.That(brain.IsTargetingPlayer, Is.True);
            Assert.That(brain.PreviousMachineTarget, Is.SameAs(machine));

            player.transform.position = new Vector3(11f, 0f, 0f);
            brain.Tick(0.5f, 0.5f, 1f);
            brain.Tick(0.49f, 0.99f, 1f);
            Assert.That(brain.IsTargetingPlayer, Is.True, "Disengagement should not happen before the configured delay.");

            brain.Tick(0.02f, 1.01f, 1f);
            Assert.That(brain.IsTargetingPlayer, Is.False);
            Assert.That(brain.CurrentMachineTarget, Is.SameAs(machine));
        }

        [Test]
        public void BrokenMachine_NotifiesBrainAndForcesRetarget()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth first = CreateMachine("Mine", new Vector3(4f, 0f, 0f), registry);
            FactoryMachineHealth second = CreateMachine("Smelter", new Vector3(8f, 0f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(40f, 0f, 0f));
            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            Assert.That(brain.CurrentMachineTarget, Is.SameAs(first));
            first.TakeDamage(new DamageInfo(999, brain.gameObject, Faction.Enemy, first.transform.position));
            brain.Tick(0.02f, 0.02f, 1f);

            Assert.That(first.State, Is.EqualTo(FactoryMachineState.Broken));
            Assert.That(brain.CurrentMachineTarget, Is.SameAs(second));
        }

        [Test]
        public void InvalidPreviousMachine_IsReplacedWhenPlayerDisengages()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth first = CreateMachine("Mine", new Vector3(4f, 0f, 0f), registry);
            FactoryMachineHealth second = CreateMachine("Smelter", new Vector3(8f, 0f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(5f, 0f, 0f));
            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            brain.Tick(0.02f, 0f, 1f);
            Assert.That(brain.IsTargetingPlayer, Is.True);
            first.TakeDamage(new DamageInfo(999, brain.gameObject, Faction.Enemy, first.transform.position));

            player.transform.position = new Vector3(12f, 0f, 0f);
            brain.Tick(1.1f, 1.1f, 1f);

            Assert.That(brain.IsTargetingPlayer, Is.False);
            Assert.That(brain.CurrentMachineTarget, Is.SameAs(second));
        }

        [Test]
        public void ElevatedMachineTarget_UsesPlanarRangeAndReceivesMeleeDamage()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth machine = CreateMachine("Elevated Machine", new Vector3(1.9f, 0.7f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(40f, 0f, 0f));
            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            brain.Tick(0.02f, 0f, 1f);
            Assert.That(brain.State, Is.EqualTo(EnemyState.AttackMachine));

            EnemyAttackController attack = brain.GetComponent<EnemyAttackController>();
            Assert.That(attack.IsAttacking, Is.True);
            attack.OnAttackImpact();
            Assert.That(machine.CurrentHealth, Is.EqualTo(84));
        }

        [Test]
        public void Brain_ReenabledAfterMachineBreak_ResubscribesAndRetargets()
        {
            MachineTargetRegistry registry = CreateRegistry();
            FactoryMachineHealth first = CreateMachine("Mine", new Vector3(4f, 0f, 0f), registry);
            FactoryMachineHealth second = CreateMachine("Smelter", new Vector3(8f, 0f, 0f), registry);
            PlayerHealth player = CreatePlayer(new Vector3(40f, 0f, 0f));
            EnemyBrain brain = CreateEnemy(Vector3.zero, registry, player.Targetable);

            InvokeLifecycle(brain, "OnDisable");
            first.TakeDamage(new DamageInfo(999, brain.gameObject, Faction.Enemy, first.transform.position));
            InvokeLifecycle(brain, "OnEnable");

            Assert.That(brain.CurrentMachineTarget, Is.SameAs(second));
            Assert.That(brain.State, Is.EqualTo(EnemyState.MoveToMachine));
        }

        private MachineTargetRegistry CreateRegistry()
        {
            GameObject root = Track(new GameObject("Machine Registry"));
            return root.AddComponent<MachineTargetRegistry>();
        }

        private FactoryMachineHealth CreateMachine(
            string machineName,
            Vector3 position,
            MachineTargetRegistry registry)
        {
            GameObject root = Track(new GameObject(machineName));
            root.transform.position = position;
            Health health = root.AddComponent<Health>();
            FactionMember factionMember = root.AddComponent<FactionMember>();
            Targetable targetable = root.AddComponent<Targetable>();
            FactoryMachineHealth machine = root.AddComponent<FactoryMachineHealth>();
            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(root.transform, false);
            aimObject.transform.localPosition = Vector3.up;
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();
            targetable.Configure(factionMember, targetPoint, machine, true);
            machine.Configure(machineName, 100, 1, health, factionMember, targetable);
            machine.AssignRegistry(registry);

            GameObject terminalObject = new GameObject(machineName + " Terminal");
            terminalObject.transform.SetParent(root.transform, false);
            FactoryObjectiveTerminal terminal = terminalObject.AddComponent<FactoryObjectiveTerminal>();
            terminal.Configure(
                machineName,
                null,
                null,
                Array.Empty<GameObject>(),
                Array.Empty<Light>(),
                Array.Empty<PlatformerUltra.Factory.Conveyors.ConveyorBelt>(),
                false,
                machine,
                1);
            terminal.Activate();
            return machine;
        }

        private PlayerHealth CreatePlayer(Vector3 position)
        {
            GameObject root = Track(new GameObject("Player"));
            root.transform.position = position;
            Health health = root.AddComponent<Health>();
            FactionMember factionMember = root.AddComponent<FactionMember>();
            Targetable targetable = root.AddComponent<Targetable>();
            PlayerHealth playerHealth = root.AddComponent<PlayerHealth>();
            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(root.transform, false);
            aimObject.transform.localPosition = Vector3.up;
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();
            targetable.Configure(factionMember, targetPoint, playerHealth, true);
            playerHealth.Configure(health, factionMember, targetable, 100, 0f);
            return playerHealth;
        }

        private EnemyBrain CreateEnemy(
            Vector3 position,
            MachineTargetRegistry registry,
            Targetable player)
        {
            GameObject root = Track(new GameObject("Enemy"));
            root.transform.position = position;
            Health health = root.AddComponent<Health>();
            FactionMember factionMember = root.AddComponent<FactionMember>();
            Targetable targetable = root.AddComponent<Targetable>();
            TestEnemyMotor motor = root.AddComponent<TestEnemyMotor>();
            EnemyAttackController attackController = root.AddComponent<EnemyAttackController>();
            EnemyBrain brain = root.AddComponent<EnemyBrain>();
            EnemyHealth enemyHealth = root.AddComponent<EnemyHealth>();
            GameObject aimObject = new GameObject("Target Point");
            aimObject.transform.SetParent(root.transform, false);
            aimObject.transform.localPosition = Vector3.up;
            TargetPoint targetPoint = aimObject.AddComponent<TargetPoint>();

            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.ConfigureIdentity(EnemyArchetype.Saboteur, null, null, null, null, 1f);
            definition.ConfigureMovement(60, 2f, 4f, 10f, 14f, 540f, 0f, 0f, 0f);
            definition.ConfigureTargeting(6f, 10f, 1f, 2f, 2f);
            definition.ConfigureRegularAttack(1.1f, 1f, 0.55f, 12, 16, 0f, 1f);
            _objects.Add(definition);

            targetable.Configure(factionMember, targetPoint, enemyHealth, true);
            motor.Configure(definition);
            attackController.Configure(definition, null, motor, null, null, ~0);
            enemyHealth.Configure(definition, health, factionMember, targetable, brain);
            brain.Configure(definition, enemyHealth, attackController, motor, null);
            brain.InitializeRuntime(registry, player, null);
            return brain;
        }

        private GameObject Track(GameObject gameObject)
        {
            _objects.Add(gameObject);
            return gameObject;
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

    public sealed class TestEnemyMotor : MonoBehaviour, IEnemyMotor
    {
        public bool IsReady => true;
        public bool IsMoving => false;
        public Vector3 Velocity => Vector3.zero;
        public bool CanResolveLanding { get; set; } = true;
        public bool IsInScriptedMotion { get; private set; }
        public int BeginScriptedMotionCount { get; private set; }
        public int EndScriptedMotionCount { get; private set; }

        public void Configure(EnemyDefinition definition)
        {
        }

        public bool TryPlace(Vector3 position, float searchRadius)
        {
            transform.position = position;
            return true;
        }

        public bool SetDestination(Vector3 position, float stoppingDistance, bool chasingPlayer)
        {
            return true;
        }

        public bool CanReach(Vector3 position, float searchRadius)
        {
            return true;
        }

        public bool TryResolveLanding(Vector3 desiredPosition, float searchRadius, out Vector3 landingPosition)
        {
            landingPosition = desiredPosition;
            return CanResolveLanding;
        }

        public void Stop()
        {
        }

        public void FaceTarget(Vector3 targetPosition, float deltaTime)
        {
        }

        public void BeginScriptedMotion()
        {
            IsInScriptedMotion = true;
            BeginScriptedMotionCount++;
        }

        public void SetScriptedPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void EndScriptedMotion(Vector3 landingPosition)
        {
            transform.position = landingPosition;
            IsInScriptedMotion = false;
            EndScriptedMotionCount++;
        }
    }
}
