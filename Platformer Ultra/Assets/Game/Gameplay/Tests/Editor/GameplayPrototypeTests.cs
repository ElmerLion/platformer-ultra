using System.Reflection;
using NUnit.Framework;
using PlatformerUltra.Factory.Conveyors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PlatformerUltra.Gameplay.Tests
{
    public sealed class GameplayPrototypeTests
    {
        [Test]
        public void MovementSettings_DefaultsSupportForgivingPlatforming()
        {
            PlayerMovementSettings settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                Assert.That(settings.MovementSpeed, Is.GreaterThan(0f));
                Assert.That(settings.SprintSpeed, Is.EqualTo(3.525f).Within(0.0001f));
                Assert.That(settings.GroundAcceleration, Is.GreaterThan(settings.AirAcceleration));
                Assert.That(settings.JumpHeight, Is.GreaterThan(0f));
                Assert.That(settings.Gravity, Is.LessThan(0f));
                Assert.That(settings.CoyoteTime, Is.GreaterThan(0f));
                Assert.That(settings.JumpBufferTime, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void PrototypeMovementAsset_UsesOneAndAHalfTimesFasterSprint()
        {
            PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(
                "Assets/Game/Gameplay/Data/DA_PlayerMovement_Prototype.asset");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.SprintSpeed, Is.EqualTo(3.525f).Within(0.0001f));
        }

        [Test]
        public void PlayerInteractor_CameraOffsetDoesNotConsumePlayerReach()
        {
            GameObject player = new GameObject("Player");
            GameObject view = new GameObject("Interaction View");
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                Assert.That(ignoreRaycastLayer, Is.GreaterThanOrEqualTo(0));
                player.layer = ignoreRaycastLayer;

                CharacterController characterController = player.AddComponent<CharacterController>();
                characterController.center = Vector3.up;
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();

                view.transform.position = new Vector3(0f, 1f, -5.5f);
                view.transform.forward = Vector3.forward;

                targetObject.name = "Interaction Target";
                targetObject.transform.position = new Vector3(0f, 1f, 2f);
                DoubleJumpUpgradeStation station = targetObject.AddComponent<DoubleJumpUpgradeStation>();
                InteractionTarget expectedTarget = targetObject.AddComponent<InteractionTarget>();
                expectedTarget.Configure(station);

                LayerMask interactionMask = ~(1 << ignoreRaycastLayer);
                interactor.Configure(view.transform, null, null, interactionMask);
                InvokePrivate(interactor, "Awake");
                Physics.SyncTransforms();
                InvokePrivate(interactor, "RefreshTarget");

                FieldInfo cachedTargetField = typeof(PlayerInteractor).GetField(
                    "_cachedTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(cachedTargetField, Is.Not.Null);
                Assert.That(cachedTargetField.GetValue(interactor), Is.SameAs(expectedTarget));

                view.transform.position = new Vector3(5.5f, 1f, 0f);
                view.transform.forward = Vector3.forward;
                targetObject.transform.position = new Vector3(5.5f, 1f, 2f);
                Physics.SyncTransforms();
                InvokePrivate(interactor, "RefreshTarget");

                Assert.That(cachedTargetField.GetValue(interactor), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(view);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerController_DoubleJumpRemainsExplicitlyUnlockable()
        {
            GameObject player = new GameObject("Player");
            try
            {
                player.AddComponent<CharacterController>();
                ThirdPersonPlayerController controller = player.AddComponent<ThirdPersonPlayerController>();

                Assert.That(controller.DoubleJumpUnlocked, Is.False);
                controller.UnlockDoubleJump();
                Assert.That(controller.DoubleJumpUnlocked, Is.True);
                controller.SetDoubleJumpUnlocked(false);
                Assert.That(controller.DoubleJumpUnlocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void AnimationCadence_ScalesWalkingCycleToPhysicalTravel()
        {
            float normalRate = PlayerAnimationDriver.CalculateLocomotionPlaybackRate(1f, 1f, 1f, 0.25f, 3f);
            float doubleRate = PlayerAnimationDriver.CalculateLocomotionPlaybackRate(2f, 1f, 1f, 0.25f, 3f);
            float stoppedRate = PlayerAnimationDriver.CalculateLocomotionPlaybackRate(0f, 1f, 1f, 0.25f, 3f);
            float runRate = PlayerAnimationDriver.CalculateLocomotionPlaybackRate(
                3.525f,
                2.585f,
                0.733333f,
                0.25f,
                4f);

            Assert.That(normalRate, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(doubleRate, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(stoppedRate, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(runRate, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PlayerAnimator_AirborneStatesLandDirectlyIntoLocomotion()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Game/Gameplay/Animations/AC_Player_Prototype.controller");

            Assert.That(controller, Is.Not.Null);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            Assert.That(stateMachine.anyStateTransitions, Is.Empty);

            Assert.That(FindState(stateMachine, "Falling To Roll"), Is.Null);
            AnimatorState runningState = FindState(stateMachine, "Running");
            Assert.That(runningState, Is.Not.Null);
            Assert.That(runningState.motion, Is.TypeOf<AnimationClip>());
            Assert.That(runningState.motion.name, Is.EqualTo("Standard Run"));
            Assert.That(runningState.speedParameterActive, Is.True);
            Assert.That(runningState.speedParameter, Is.EqualTo(PlayerAnimationDriver.LocomotionRateParameter));
            AssertLandingDestinations(FindState(stateMachine, "Jump"));
            AssertLandingDestinations(FindState(stateMachine, "Falling Idle"));
        }

        [Test]
        public void StandardRunAnimation_IsHumanoidLoopingAndInPlace()
        {
            const string runPath = "Assets/Animations/Paladin J Nordstrom@Standard Run.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(runPath) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            Assert.That(clips, Is.Not.Empty);
            Assert.That(clips[0].loopTime, Is.True);
            Assert.That(clips[0].loopPose, Is.True);
            Assert.That(clips[0].lockRootRotation, Is.True);
            Assert.That(clips[0].lockRootHeightY, Is.True);
            Assert.That(clips[0].lockRootPositionXZ, Is.True);
        }

        [Test]
        public void ConveyorTerminal_FirstUseGeneratesThenLaterUsesCycleRoutes()
        {
            GameObject fixture = new GameObject("Terminal Fixture");
            GameObject conveyorObject = new GameObject("Conveyor");
            try
            {
                ConveyorEndpoint start = CreateEndpoint("Start", fixture.transform, Vector3.zero, ConveyorEndpointKind.Output);
                ConveyorEndpoint routeA = CreateEndpoint("Route A", fixture.transform, Vector3.right * 4f, ConveyorEndpointKind.Input);
                ConveyorEndpoint routeB = CreateEndpoint("Route B", fixture.transform, new Vector3(2f, 3f, 4f), ConveyorEndpointKind.Input);
                ConveyorBelt belt = conveyorObject.AddComponent<ConveyorBelt>();
                ConveyorRouteTerminal terminal = fixture.AddComponent<ConveyorRouteTerminal>();
                terminal.Configure(belt, start, new[] { routeA, routeB }, null);

                Assert.That(conveyorObject.activeSelf, Is.False);
                Assert.That(terminal.InteractionPrompt, Does.StartWith("Generate"));

                terminal.Interact(null);
                Assert.That(conveyorObject.activeSelf, Is.True);
                Assert.That(belt.StartEndpoint, Is.SameAs(start));
                Assert.That(belt.EndEndpoint, Is.SameAs(routeA));
                Assert.That(belt.OperatingState, Is.EqualTo(ConveyorOperatingState.Online));

                terminal.Interact(null);
                Assert.That(belt.EndEndpoint, Is.SameAs(routeB));
                Assert.That(terminal.InteractionPrompt, Does.Contain("2/2"));
            }
            finally
            {
                Object.DestroyImmediate(conveyorObject);
                Object.DestroyImmediate(fixture);
            }
        }

        private static ConveyorEndpoint CreateEndpoint(
            string name,
            Transform parent,
            Vector3 position,
            ConveyorEndpointKind kind)
        {
            GameObject endpointObject = new GameObject(name);
            endpointObject.transform.SetParent(parent, false);
            endpointObject.transform.position = position;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind);
            return endpoint;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            return null;
        }

        private static void AssertLandingDestinations(AnimatorState airborneState)
        {
            Assert.That(airborneState, Is.Not.Null);
            bool landsInIdle = false;
            bool landsInWalking = false;
            bool landsInRunning = false;
            foreach (AnimatorStateTransition transition in airborneState.transitions)
            {
                if (transition.destinationState.name == "Idle")
                {
                    landsInIdle = true;
                }
                else if (transition.destinationState.name == "Walking")
                {
                    landsInWalking = true;
                }
                else if (transition.destinationState.name == "Running")
                {
                    landsInRunning = true;
                }
            }

            Assert.That(landsInIdle, Is.True);
            Assert.That(landsInWalking, Is.True);
            Assert.That(landsInRunning, Is.True);
        }
    }
}
