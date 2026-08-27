using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class FactoryHealthTests
    {
        [Test]
        public void TimedInteraction_CompletesExactlyOnceAtConfiguredDuration()
        {
            GameObject interactor = new GameObject("Interactor");
            GameObject targetObject = new GameObject("Target");
            try
            {
                TimedTestInteractable target = targetObject.AddComponent<TimedTestInteractable>();
                target.Configure(5f);
                TimedInteractionSession session = new TimedInteractionSession();

                Assert.That(session.TryBegin(target, interactor), Is.True);
                Assert.That(session.Tick(4.99f, true), Is.EqualTo(TimedInteractionTickResult.InProgress));
                Assert.That(target.Completions, Is.Zero);
                Assert.That(session.Tick(0.01f, true), Is.EqualTo(TimedInteractionTickResult.Completed));
                Assert.That(target.Completions, Is.EqualTo(1));
                Assert.That(session.Tick(10f, true), Is.EqualTo(TimedInteractionTickResult.Inactive));
                Assert.That(target.Completions, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(interactor);
            }
        }

        [Test]
        public void TimedInteraction_CancellationResetsAndAllowsCleanRestart()
        {
            GameObject interactor = new GameObject("Interactor");
            GameObject targetObject = new GameObject("Target");
            try
            {
                TimedTestInteractable target = targetObject.AddComponent<TimedTestInteractable>();
                target.Configure(5f);
                TimedInteractionSession session = new TimedInteractionSession();

                Assert.That(session.TryBegin(target, interactor), Is.True);
                session.Tick(2.5f, true);
                Assert.That(session.Progress, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(session.Tick(0f, false), Is.EqualTo(TimedInteractionTickResult.Cancelled));
                Assert.That(session.Progress, Is.Zero);
                Assert.That(target.Cancellations, Is.EqualTo(1));

                Assert.That(session.TryBegin(target, interactor), Is.True);
                Assert.That(session.Tick(5f, true), Is.EqualTo(TimedInteractionTickResult.Completed));
                Assert.That(target.Completions, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(interactor);
            }
        }

        [Test]
        public void FactoryRepair_CompletionRestoresHealthPoweredObjectsAndConveyor()
        {
            MachineFixture fixture = new MachineFixture("Mine", 30, 5f);
            GameObject player = new GameObject("Player");
            GameObject poweredObject = new GameObject("Powered Object");
            GameObject conveyorObject = new GameObject("Conveyor");
            GameObject terminalObject = new GameObject("Terminal");
            try
            {
                ConveyorBelt conveyor = conveyorObject.AddComponent<ConveyorBelt>();
                FactoryObjectiveTerminal terminal = terminalObject.AddComponent<FactoryObjectiveTerminal>();
                terminal.Configure(
                    "Mine",
                    null,
                    null,
                    new[] { poweredObject },
                    null,
                    new[] { conveyor },
                    false,
                    fixture.Machine,
                    5f);

                terminal.Activate();
                fixture.Machine.TakeDamage(CreateDamage(30));
                Assert.That(terminal.InteractionPrompt, Is.EqualTo("Hold [E] to Repair Mine"));
                Assert.That(poweredObject.activeSelf, Is.False);
                Assert.That(conveyor.OperatingState, Is.EqualTo(ConveyorOperatingState.Sabotaged));

                Assert.That(terminal.BeginTimedInteraction(player), Is.True);
                Assert.That(terminal.CompleteTimedInteraction(player), Is.True);

                Assert.That(fixture.Machine.State, Is.EqualTo(FactoryMachineState.Online));
                Assert.That(fixture.Machine.CurrentHealth, Is.EqualTo(30));
                Assert.That(fixture.BrokenMarker.activeSelf, Is.False);
                Assert.That(poweredObject.activeSelf, Is.True);
                Assert.That(conveyor.OperatingState, Is.EqualTo(ConveyorOperatingState.Online));
                Assert.That(terminal.LastInteractionFeedback, Is.EqualTo("Mine repaired."));
            }
            finally
            {
                Object.DestroyImmediate(terminalObject);
                Object.DestroyImmediate(conveyorObject);
                Object.DestroyImmediate(poweredObject);
                Object.DestroyImmediate(player);
                fixture.Dispose();
            }
        }

        [Test]
        public void FactoryRepair_CancellationLeavesMachineBrokenAndUnlocksLocomotion()
        {
            MachineFixture fixture = new MachineFixture("Generator", 40, 5f);
            GameObject terminalObject = new GameObject("Terminal");
            GameObject player = new GameObject("Player");
            try
            {
                FactoryObjectiveTerminal terminal = terminalObject.AddComponent<FactoryObjectiveTerminal>();
                terminal.Configure("Generator", null, null, null, null, null, false, fixture.Machine, 5f);
                terminal.Activate();
                fixture.Machine.TakeDamage(CreateDamage(40));

                ThirdPersonPlayerController controller = player.AddComponent<ThirdPersonPlayerController>();
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
                Assert.That(interactor.TryBeginTimedInteraction(terminal), Is.True);
                Assert.That(controller.LocomotionLocked, Is.True);

                interactor.CancelActiveInteraction();

                Assert.That(controller.LocomotionLocked, Is.False);
                Assert.That(fixture.Machine.State, Is.EqualTo(FactoryMachineState.Broken));
                Assert.That(fixture.Machine.CurrentHealth, Is.Zero);
                Assert.That(terminal.LastInteractionFeedback, Does.Contain("cancelled"));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(terminalObject);
                fixture.Dispose();
            }
        }

        [Test]
        public void PlayerHealth_DamageDeathAndResetPreserveThePlayerObject()
        {
            GameObject player = new GameObject("Player");
            try
            {
                Health health = player.AddComponent<Health>();
                FactionMember factionMember = player.AddComponent<FactionMember>();
                Targetable targetable = player.AddComponent<Targetable>();
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                TargetPoint targetPoint = CreateTargetPoint(player.transform);
                targetable.Configure(factionMember, targetPoint, playerHealth, true);
                playerHealth.Configure(health, factionMember, targetable, 25, 0f);

                playerHealth.TakeDamage(CreateDamage(25));
                Assert.That(playerHealth.IsAlive, Is.False);
                playerHealth.ResetPlayer();
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(25));
                Assert.That(targetable.IsTargetable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerDeath_UpdatesHealthAndSpawnsSharedExplosionExactlyOnce()
        {
            GameObject player = new GameObject("Player");
            GameObject effectTemplate = new GameObject("Shared Mechanical Death Explosion");
            GameObject spawnedEffect = null;
            try
            {
                Health health = player.AddComponent<Health>();
                FactionMember factionMember = player.AddComponent<FactionMember>();
                Targetable targetable = player.AddComponent<Targetable>();
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                TargetPoint targetPoint = CreateTargetPoint(player.transform);
                targetable.Configure(factionMember, targetPoint, playerHealth, true);
                playerHealth.Configure(health, factionMember, targetable, 25, 0f);
                DeathExplosionEmitter emitter = player.AddComponent<DeathExplosionEmitter>();
                emitter.Configure(effectTemplate, targetPoint.transform, 1.1f);
                playerHealth.ConfigureDeathExplosion(emitter);

                int reportedCurrentHealth = -1;
                int healthChangeCount = 0;
                playerHealth.HealthChanged += (current, maximum) =>
                {
                    reportedCurrentHealth = current;
                    healthChangeCount++;
                };

                Assert.That(playerHealth.TakeDamage(CreateDamage(25)), Is.True);
                spawnedEffect = emitter.LastSpawnedEffect;
                Assert.That(playerHealth.IsAlive, Is.False);
                Assert.That(reportedCurrentHealth, Is.Zero);
                Assert.That(healthChangeCount, Is.EqualTo(1));
                Assert.That(emitter.SpawnCount, Is.EqualTo(1));
                Assert.That(spawnedEffect, Is.Not.Null);
                Assert.That(playerHealth.TakeDamage(CreateDamage(1)), Is.False);
                Assert.That(emitter.SpawnCount, Is.EqualTo(1));
            }
            finally
            {
                if (spawnedEffect != null)
                {
                    Object.DestroyImmediate(spawnedEffect);
                }

                Object.DestroyImmediate(effectTemplate);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GameOver_PlayerDeathLocksControlsHidesVisualAndExposesRetry()
        {
            GameObject player = new GameObject("Player");
            GameObject cameraObject = new GameObject("Camera");
            GameObject hudObject = new GameObject("HUD");
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                visualObject.transform.SetParent(player.transform, false);
                Renderer playerRenderer = visualObject.GetComponent<Renderer>();
                Health health = player.AddComponent<Health>();
                FactionMember factionMember = player.AddComponent<FactionMember>();
                Targetable targetable = player.AddComponent<Targetable>();
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                TargetPoint targetPoint = CreateTargetPoint(player.transform);
                targetable.Configure(factionMember, targetPoint, playerHealth, true);
                playerHealth.Configure(health, factionMember, targetable, 25, 0f);

                player.AddComponent<CharacterController>();
                ThirdPersonPlayerController playerController = player.AddComponent<ThirdPersonPlayerController>();
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
                cameraObject.AddComponent<Camera>();
                ThirdPersonOrbitCamera orbitCamera = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
                PlayerStatusPresenter presenter = hudObject.AddComponent<PlayerStatusPresenter>();
                FactoryGameOverController gameOver = hudObject.AddComponent<FactoryGameOverController>();
                gameOver.Configure(
                    playerHealth,
                    presenter,
                    playerController,
                    interactor,
                    orbitCamera,
                    new[] { playerRenderer });

                int retryRequests = 0;
                gameOver.RetryRequested += () => retryRequests++;
                playerHealth.TakeDamage(CreateDamage(25));

                Assert.That(gameOver.IsGameOver, Is.True);
                Assert.That(playerController.LocomotionLocked, Is.True);
                Assert.That(interactor.enabled, Is.False);
                Assert.That(orbitCamera.enabled, Is.False);
                Assert.That(playerRenderer.enabled, Is.False);
                Assert.That(presenter.IsGameOverVisible, Is.True);

                presenter.RequestRetry();
                Assert.That(retryRequests, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void MachineRegistry_FindsNearestEligibleMachine()
        {
            GameObject registryObject = new GameObject("Registry");
            MachineFixture near = new MachineFixture("Near", 10, 5f);
            MachineFixture far = new MachineFixture("Far", 10, 5f);
            try
            {
                MachineTargetRegistry registry = registryObject.AddComponent<MachineTargetRegistry>();
                near.Root.transform.position = Vector3.right * 2f;
                far.Root.transform.position = Vector3.right * 8f;
                near.Machine.SetProgressionActivated();
                far.Machine.SetProgressionActivated();
                near.Machine.AssignRegistry(registry);
                far.Machine.AssignRegistry(registry);

                Assert.That(registry.Targets, Has.Count.EqualTo(2));
                Assert.That(registry.FindNearestEligible(Vector3.zero), Is.SameAs(near.Machine));
                Assert.That(registry.FindNearestEligibleTarget(Vector3.zero), Is.SameAs(near.Machine));
            }
            finally
            {
                near.Dispose();
                far.Dispose();
                Object.DestroyImmediate(registryObject);
            }
        }

        private static DamageInfo CreateDamage(int amount)
        {
            return new DamageInfo(amount, null, Faction.Enemy, Vector3.zero);
        }

        private static TargetPoint CreateTargetPoint(Transform parent)
        {
            GameObject pointObject = new GameObject("Target Point");
            pointObject.transform.SetParent(parent, false);
            return pointObject.AddComponent<TargetPoint>();
        }

        private sealed class MachineFixture
        {
            public MachineFixture(string machineName, int maximumHealth, float repairDuration)
            {
                Root = new GameObject(machineName);
                Health health = Root.AddComponent<Health>();
                FactionMember factionMember = Root.AddComponent<FactionMember>();
                Targetable targetable = Root.AddComponent<Targetable>();
                Machine = Root.AddComponent<FactoryMachineHealth>();
                BrokenMarker = new GameObject("Broken Machine Marker");
                BrokenMarker.transform.SetParent(Root.transform, false);
                BrokenMarker.SetActive(false);
                TargetPoint targetPoint = CreateTargetPoint(Root.transform);
                targetable.Configure(factionMember, targetPoint, Machine, true);
                Machine.Configure(
                    machineName,
                    maximumHealth,
                    repairDuration,
                    health,
                    factionMember,
                    targetable,
                    null,
                    BrokenMarker);
            }

            public GameObject Root { get; }
            public FactoryMachineHealth Machine { get; }
            public GameObject BrokenMarker { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
            }
        }

        private sealed class TimedTestInteractable : MonoBehaviour, ITimedInteractable
        {
            public int Completions { get; private set; }
            public int Cancellations { get; private set; }
            public float InteractionDuration { get; private set; }
            public string InteractionPrompt => "Hold";
            public string InteractionActionLabel => "Working";

            public void Configure(float duration)
            {
                InteractionDuration = duration;
            }

            public bool CanInteract(GameObject interactor)
            {
                return interactor != null;
            }

            public void Interact(GameObject interactor)
            {
            }

            public bool BeginTimedInteraction(GameObject interactor)
            {
                return CanInteract(interactor);
            }

            public void CancelTimedInteraction(GameObject interactor)
            {
                Cancellations++;
            }

            public bool CompleteTimedInteraction(GameObject interactor)
            {
                Completions++;
                return true;
            }
        }
    }
}
