using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlatformerUltra.Enemies.Tests.PlayMode
{
    public sealed class FactoryEnemySystemSmokeTests
    {
        private const string FactorySceneName = "FactoryVerticalMap";
        private const float SceneLoadTimeoutSeconds = 15f;
        private const float MovementTimeoutSeconds = 4f;
        private const float GroundAttackTimeoutSeconds = 30f;

        [UnitySetUp]
        public IEnumerator LoadFactoryMap()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(FactorySceneName, LoadSceneMode.Single);
            Assert.That(
                load,
                Is.Not.Null,
                "FactoryVerticalMap must be enabled in Build Settings before its PlayMode tests run.");

            float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
            while (!load.isDone && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(load.isDone, Is.True, "FactoryVerticalMap did not finish loading within the bounded timeout.");

            // Allow Awake, OnEnable, Start, and NavMesh registration to settle.
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator FactoryMap_HasTwoWorkingEntrancesAndCompleteGroundRoutes()
        {
            EnemySpawnManager[] managers = Object.FindObjectsByType<EnemySpawnManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            EnemySpawnPoint[] allSpawnPoints = Object.FindObjectsByType<EnemySpawnPoint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            MachineTargetRegistry[] machineRegistries = Object.FindObjectsByType<MachineTargetRegistry>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            EnemyRuntimeRegistry[] enemyRegistries = Object.FindObjectsByType<EnemyRuntimeRegistry>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            PlayerHealth[] players = Object.FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(managers, Has.Length.EqualTo(1), "The real factory map must own exactly one spawn manager.");
            Assert.That(allSpawnPoints, Has.Length.EqualTo(2), "Only the Mine and Generator service-door entrances should remain.");
            Assert.That(machineRegistries, Has.Length.EqualTo(1));
            Assert.That(enemyRegistries, Has.Length.EqualTo(1));
            Assert.That(players, Has.Length.EqualTo(1));

            EnemySpawnPoint[] lowerDoorSpawnPoints = allSpawnPoints
                .Where(IsRequiredLowerDoorSpawn)
                .OrderBy(point => GetOwningDoorName(point.transform))
                .ToArray();
            Assert.That(
                lowerDoorSpawnPoints,
                Has.Length.EqualTo(2),
                "Both bottom service doors must contain an EnemySpawnPoint.");

            MachineTargetRegistry machineRegistry = machineRegistries[0];
            Assert.That(machineRegistry.Machines, Has.Count.EqualTo(4));
            Assert.That(NavMesh.CalculateTriangulation().vertices, Is.Not.Empty, "The factory NavMesh was not loaded.");

            AssertCompleteGroundRoutes(lowerDoorSpawnPoints, machineRegistry.Machines);

            FactoryObjectiveTerminal[] terminals = Object.FindObjectsByType<FactoryObjectiveTerminal>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            ActivateProgressionInOrder(terminals);
            yield return null;

            Assert.That(machineRegistry.HasOperationalMachines, Is.True);
            Assert.That(managers[0].SpawningUnlocked, Is.True);

            HashSet<EnemySpawnPoint> usedEntrances = new HashSet<EnemySpawnPoint>();
            List<EnemyHealth> spawnedEnemies = new List<EnemyHealth>();
            managers[0].EnemySpawned += (enemy, spawnPoint) =>
            {
                spawnedEnemies.Add(enemy);
                usedEntrances.Add(spawnPoint);
            };

            Assert.That(
                managers[0].TrySpawn(1000f, 0f, 0.15f, 0f),
                Is.True,
                "The first configured lower entrance could not spawn an enemy.");
            yield return null;
            Assert.That(
                managers[0].TrySpawn(2000f, 0.999f, 0.15f, 0f),
                Is.True,
                "The second configured lower entrance could not spawn an enemy.");
            yield return null;

            Assert.That(spawnedEnemies, Has.Count.EqualTo(2));
            Assert.That(usedEntrances.SetEquals(lowerDoorSpawnPoints), Is.True);
            foreach (EnemyHealth enemy in spawnedEnemies)
            {
                Assert.That(enemy, Is.Not.Null);
                Assert.That(enemy.IsAlive, Is.True);
                Assert.That(enemy.Targetable, Is.Not.Null);
                Assert.That(enemy.Targetable.IsTargetable, Is.True);
                Assert.That(enemy.GetComponent<EnemyBrain>().CurrentMachineTarget, Is.Not.Null);
                Object.Destroy(enemy.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GroundEnemy_ClimbsToAndDamagesOnlineSmelter()
        {
            PrepareCombatScenario(
                out EnemySpawnManager manager,
                out PlayerHealth player,
                out IReadOnlyList<FactoryMachineHealth> machines);

            FactoryMachineHealth smelter = GetMachine(machines, "Smelter");
            Assert.That(smelter.State, Is.EqualTo(FactoryMachineState.Online));
            Assert.That(smelter.transform.position.y, Is.GreaterThan(4f), "The tested machine must be on an elevated tier.");

            EnemyHealth saboteur = SpawnEnemy(
                manager,
                1000f,
                0f,
                0.1f,
                EnemyArchetype.Saboteur);
            EnemyBrain brain = saboteur.GetComponent<EnemyBrain>();
            NavMeshEnemyMotor motor = saboteur.GetComponent<NavMeshEnemyMotor>();
            Assert.That(brain, Is.Not.Null);
            Assert.That(motor, Is.Not.Null);
            Assert.That(motor.IsReady, Is.True, "The spawned Saboteur was not placed on the baked NavMesh.");

            brain.ForceMachineTargetForTests(smelter);
            int startingHealth = smelter.CurrentHealth;
            float startingHeight = saboteur.transform.position.y;
            float maximumObservedHeight = startingHeight;
            float deadline = Time.realtimeSinceStartup + GroundAttackTimeoutSeconds;

            while (smelter.CurrentHealth == startingHealth && Time.realtimeSinceStartup < deadline)
            {
                maximumObservedHeight = Mathf.Max(maximumObservedHeight, saboteur.transform.position.y);
                yield return null;
            }

            Assert.That(
                maximumObservedHeight,
                Is.GreaterThan(startingHeight + 3f),
                "The Saboteur never climbed from the lower service level toward the Smelter.");
            Assert.That(
                smelter.CurrentHealth,
                Is.LessThan(startingHealth),
                "The Saboteur reached no damaging attack window within the bounded traversal time. " +
                $"Enemy position: {saboteur.transform.position}; state: {brain.State}; " +
                $"distance to target: {Vector3.Distance(saboteur.transform.position, smelter.Targetable.TargetPoint.position):F2}.");

            Object.Destroy(saboteur.gameObject);
            player.Targetable.SetTargetable(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Drone_MovesTowardFactoryTargetWithoutWalkingAnimator()
        {
            PrepareCombatScenario(
                out EnemySpawnManager manager,
                out PlayerHealth player,
                out IReadOnlyList<FactoryMachineHealth> machines);

            EnemyHealth drone = SpawnEnemy(
                manager,
                2000f,
                0.999f,
                0.1f,
                EnemyArchetype.Drone);
            EnemyBrain brain = drone.GetComponent<EnemyBrain>();
            DroneFlightMotor motor = drone.GetComponent<DroneFlightMotor>();
            Assert.That(brain, Is.Not.Null);
            Assert.That(motor, Is.Not.Null);
            Assert.That(
                drone.GetComponentsInChildren<Animator>(true),
                Is.Empty,
                "The Drone must use procedural flight rather than a walking Animator.");

            FactoryMachineHealth assembler = GetMachine(machines, "Assembler");
            brain.ForceMachineTargetForTests(assembler);
            Vector3 startingPosition = drone.transform.position;
            Vector3 targetPosition = assembler.Targetable.TargetPoint.position;
            float startingTargetDistance = Vector3.Distance(startingPosition, targetPosition);
            bool observedMovingState = false;
            float deadline = Time.realtimeSinceStartup + MovementTimeoutSeconds;

            while (Vector3.Distance(startingPosition, drone.transform.position) < 0.5f &&
                   Time.realtimeSinceStartup < deadline)
            {
                observedMovingState |= motor.IsMoving;
                yield return null;
            }

            observedMovingState |= motor.IsMoving;
            Assert.That(observedMovingState, Is.True, "DroneFlightMotor never reported procedural motion.");
            Assert.That(
                Vector3.Distance(startingPosition, drone.transform.position),
                Is.GreaterThanOrEqualTo(0.5f),
                "The Drone did not visibly move toward its assigned factory target.");
            Assert.That(
                Vector3.Distance(drone.transform.position, targetPosition),
                Is.LessThan(startingTargetDistance),
                "The Drone moved, but did not close distance to its assigned factory target.");

            Object.Destroy(drone.gameObject);
            player.Targetable.SetTargetable(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_PlayerOverrideDisengagesAndResumesPreviousMachine()
        {
            PrepareCombatScenario(
                out EnemySpawnManager manager,
                out PlayerHealth player,
                out IReadOnlyList<FactoryMachineHealth> machines);

            EnemyHealth saboteur = SpawnEnemy(
                manager,
                3000f,
                0f,
                0.1f,
                EnemyArchetype.Saboteur);
            EnemyBrain brain = saboteur.GetComponent<EnemyBrain>();
            Assert.That(brain, Is.Not.Null);

            FactoryMachineHealth previousMachine = GetMachine(machines, "Smelter");
            brain.ForceMachineTargetForTests(previousMachine);
            player.Targetable.SetTargetable(true);
            TeleportPlayerForTest(player, saboteur.transform.position + Vector3.right * 2f);
            brain.Tick(0f, 3000.1f, 1f);

            Assert.That(brain.IsTargetingPlayer, Is.True, "A nearby live player did not override the machine target.");
            Assert.That(brain.CurrentTarget, Is.SameAs(player.Targetable));
            Assert.That(brain.PreviousMachineTarget, Is.SameAs(previousMachine));

            float disengageDistance = saboteur.Definition.PlayerDisengageDistance + 2f;
            TeleportPlayerForTest(
                player,
                saboteur.transform.position + Vector3.right * disengageDistance);
            brain.Tick(saboteur.Definition.PlayerDisengageDelay + 0.05f, 3000.2f, 1f);

            Assert.That(brain.IsTargetingPlayer, Is.False, "The enemy did not disengage after the hysteresis delay.");
            Assert.That(brain.CurrentMachineTarget, Is.SameAs(previousMachine));
            Assert.That(brain.CurrentTarget, Is.SameAs(previousMachine.Targetable));
            Assert.That(
                brain.State,
                Is.EqualTo(EnemyState.MoveToMachine).Or.EqualTo(EnemyState.AttackMachine),
                "The enemy did not resume its previous machine objective.");

            Object.Destroy(saboteur.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BrokenMachine_TimedRepairRestoresOnlineState()
        {
            PrepareCombatScenario(
                out EnemySpawnManager manager,
                out PlayerHealth player,
                out IReadOnlyList<FactoryMachineHealth> machines);
            manager.enabled = false;

            FactoryMachineHealth smelter = GetMachine(machines, "Smelter");
            FactoryObjectiveTerminal terminal = smelter.Terminal;
            Assert.That(terminal, Is.Not.Null, "The Smelter machine is not bound to its objective terminal.");
            bool damageApplied = smelter.TakeDamage(new DamageInfo(
                smelter.MaximumHealth,
                player.gameObject,
                Faction.Enemy,
                smelter.Targetable.TargetPoint.position));

            Assert.That(damageApplied, Is.True);
            Assert.That(smelter.State, Is.EqualTo(FactoryMachineState.Broken));
            Assert.That(terminal.IsOperational, Is.False);
            Assert.That(smelter.Targetable.IsTargetable, Is.False);

            Assert.That(terminal.BeginTimedInteraction(player.gameObject), Is.True);
            Assert.That(terminal.CompleteTimedInteraction(player.gameObject), Is.True);

            Assert.That(smelter.State, Is.EqualTo(FactoryMachineState.Online));
            Assert.That(smelter.CurrentHealth, Is.EqualTo(smelter.MaximumHealth));
            Assert.That(smelter.Targetable.IsTargetable, Is.True);
            Assert.That(terminal.IsOperational, Is.True);
            Assert.That(terminal.LastInteractionFeedback, Does.Contain("repaired"));
            yield return null;
        }

        private static void AssertCompleteGroundRoutes(
            IReadOnlyList<EnemySpawnPoint> spawnPoints,
            IReadOnlyList<FactoryMachineHealth> machines)
        {
            foreach (EnemySpawnPoint spawnPoint in spawnPoints)
            {
                Assert.That(
                    NavMesh.SamplePosition(spawnPoint.SpawnPosition, out NavMeshHit start, 4f, NavMesh.AllAreas),
                    Is.True,
                    GetOwningDoorName(spawnPoint.transform) + " is not adjacent to the baked NavMesh.");

                foreach (FactoryMachineHealth machine in machines)
                {
                    Vector3 targetPosition = machine.Targetable != null
                        ? machine.Targetable.TargetPoint.position
                        : machine.transform.position;
                    Assert.That(
                        NavMesh.SamplePosition(targetPosition, out NavMeshHit destination, 6f, NavMesh.AllAreas),
                        Is.True,
                        machine.MachineName + " has no nearby navigable attack position.");

                    NavMeshPath path = new NavMeshPath();
                    bool calculated = NavMesh.CalculatePath(
                        start.position,
                        destination.position,
                        NavMesh.AllAreas,
                        path);
                    Assert.That(calculated, Is.True, RouteMessage(spawnPoint, machine));
                    Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), RouteMessage(spawnPoint, machine));
                }
            }
        }

        private static void ActivateProgressionInOrder(IReadOnlyList<FactoryObjectiveTerminal> terminals)
        {
            string[] terminalNames =
            {
                "Mine Activation Terminal",
                "Smelter Activation Terminal",
                "Generator Activation Terminal",
                "Assembler Activation Terminal"
            };

            foreach (string terminalName in terminalNames)
            {
                FactoryObjectiveTerminal terminal = terminals.SingleOrDefault(
                    candidate => candidate.gameObject.name == terminalName);
                Assert.That(terminal, Is.Not.Null, terminalName + " is missing from FactoryVerticalMap.");
                terminal.Activate();
                Assert.That(terminal.IsActivated, Is.True, terminalName + " could not be activated in progression order.");
                Assert.That(terminal.IsOperational, Is.True, terminalName + " did not bring its machine online.");
            }
        }

        private static void PrepareCombatScenario(
            out EnemySpawnManager manager,
            out PlayerHealth player,
            out IReadOnlyList<FactoryMachineHealth> machines)
        {
            manager = Object.FindObjectsByType<EnemySpawnManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            player = Object.FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            MachineTargetRegistry machineRegistry = Object.FindObjectsByType<MachineTargetRegistry>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single();
            FactoryObjectiveTerminal[] terminals = Object.FindObjectsByType<FactoryObjectiveTerminal>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            ActivateProgressionInOrder(terminals);
            manager.enabled = false;
            player.Targetable.SetTargetable(false);
            machines = machineRegistry.Machines;
            Assert.That(machines, Has.Count.EqualTo(4));
        }

        private static EnemyHealth SpawnEnemy(
            EnemySpawnManager manager,
            float timestamp,
            float pointSample,
            float weightSample,
            EnemyArchetype expectedArchetype)
        {
            EnemyHealth spawnedEnemy = null;
            void CaptureSpawn(EnemyHealth enemy, EnemySpawnPoint spawnPoint)
            {
                spawnedEnemy = enemy;
            }

            manager.EnemySpawned += CaptureSpawn;
            try
            {
                Assert.That(
                    manager.TrySpawn(timestamp, pointSample, weightSample, 0f),
                    Is.True,
                    $"The configured spawn point could not produce a deterministic {expectedArchetype}.");
            }
            finally
            {
                manager.EnemySpawned -= CaptureSpawn;
            }

            Assert.That(spawnedEnemy, Is.Not.Null);
            Assert.That(spawnedEnemy.Definition, Is.Not.Null);
            Assert.That(spawnedEnemy.Definition.Archetype, Is.EqualTo(expectedArchetype));
            return spawnedEnemy;
        }

        private static void TeleportPlayerForTest(PlayerHealth player, Vector3 position)
        {
            CharacterController characterController = player.GetComponent<CharacterController>();
            bool restoreController = characterController != null && characterController.enabled;
            if (restoreController)
            {
                characterController.enabled = false;
            }

            player.transform.position = position;
            Physics.SyncTransforms();

            if (restoreController)
            {
                characterController.enabled = true;
            }
        }

        private static FactoryMachineHealth GetMachine(
            IEnumerable<FactoryMachineHealth> machines,
            string machineName)
        {
            FactoryMachineHealth machine = machines.SingleOrDefault(candidate => candidate.MachineName == machineName);
            Assert.That(machine, Is.Not.Null, machineName + " machine is missing from FactoryVerticalMap.");
            return machine;
        }

        private static bool IsRequiredLowerDoorSpawn(EnemySpawnPoint spawnPoint)
        {
            string doorName = GetOwningDoorName(spawnPoint.transform);
            return doorName == "Future Enemy Entrance - Mine Service Door" ||
                   doorName == "Future Enemy Entrance - Generator Service Door";
        }

        private static string GetOwningDoorName(Transform node)
        {
            Transform current = node;
            while (current != null)
            {
                if (current.name.StartsWith("Future Enemy Entrance - ", System.StringComparison.Ordinal))
                {
                    return current.name;
                }

                current = current.parent;
            }

            return string.Empty;
        }

        private static string RouteMessage(EnemySpawnPoint spawnPoint, FactoryMachineHealth machine)
        {
            return "No complete ground route from " + GetOwningDoorName(spawnPoint.transform) +
                   " to " + machine.MachineName + ".";
        }
    }
}
