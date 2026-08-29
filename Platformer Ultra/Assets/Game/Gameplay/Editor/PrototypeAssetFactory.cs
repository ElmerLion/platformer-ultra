using System.Collections.Generic;
using PlatformerUltra.CharacterArt.Editor;
using PlatformerUltra.Combat;
using PlatformerUltra.Factory.Conveyors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay.Editor
{
    public static class PrototypeAssetFactory
    {
        private const string GameplayRoot = "Assets/Game/Gameplay";
        private const string DataFolder = GameplayRoot + "/Data";
        private const string InputFolder = "Assets/Game/Input";
        private const string AnimationFolder = GameplayRoot + "/Animations";
        private const string MaterialFolder = GameplayRoot + "/Materials";
        private const string PrefabFolder = GameplayRoot + "/Prefabs";
        private const string SceneFolder = "Assets/Game/Scenes";
        private const string UiFolder = "Assets/Game/UI";

        private const string MovementSettingsPath = DataFolder + "/DA_PlayerMovement_Prototype.asset";
        private const string InputActionsPath = InputFolder + "/IA_Gameplay.asset";
        private const string LegacyInputActionsPath = InputFolder + "/IA_Gameplay.inputactions";
        private const string MoveReferencePath = InputFolder + "/IAR_Move.asset";
        private const string LookReferencePath = InputFolder + "/IAR_Look.asset";
        private const string JumpReferencePath = InputFolder + "/IAR_Jump.asset";
        private const string SprintReferencePath = InputFolder + "/IAR_Sprint.asset";
        private const string DashReferencePath = InputFolder + "/IAR_Dash.asset";
        private const string InteractReferencePath = InputFolder + "/IAR_Interact.asset";
        private const string PauseReferencePath = InputFolder + "/IAR_Pause.asset";
        private const string PanelSettingsPath = UiFolder + "/PS_PrototypeHUD.asset";
        private const string HudLayoutPath = UiFolder + "/PrototypeHUD.uxml";
        private const string HudStylePath = UiFolder + "/PrototypeHUD.uss";

        private const string PlayerPrefabPath = PrefabFolder + "/PF_Player_Prototype.prefab";
        private const string TerminalPrefabPath = PrefabFolder + "/PF_ConveyorRouteTerminal.prefab";
        private const string PlatformPrefabPath = PrefabFolder + "/PF_FactoryPlatform_4x4.prefab";
        private const string OrePrefabPath = PrefabFolder + "/PF_OreChunk_Prototype.prefab";
        private const string PortalCorePrefabPath = PrefabFolder + "/PF_PortalCore_Prototype.prefab";
        private const string ScenePath = SceneFolder + "/ConveyorTestScene.unity";
        private const string PlayerAnimatorControllerPath = AnimationFolder + "/AC_Player_Prototype.controller";

        private const string IdleAnimationPath = "Assets/Animations/Paladin J Nordstrom@Idle.fbx";
        private const string WalkingAnimationPath = "Assets/Animations/Paladin J Nordstrom@Walking.fbx";
        private const string RunningAnimationPath = "Assets/Animations/Paladin J Nordstrom@Standard Run.fbx";
        private const string JumpAnimationPath = "Assets/Animations/Paladin J Nordstrom@Jump 1.fbx";
        private const string FallingIdleAnimationPath = "Assets/Animations/Paladin J Nordstrom@Falling Idle.fbx";
        private const string JumpAirborneClipName = "Jump Airborne";

        private const string ConveyorPrefabPath = "Assets/Game/Factory/Conveyors/Prefabs/PF_Conveyor_PointToPoint.prefab";
        private const string ConsoleVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Props/SM_Prop_ControlPanel_04.prefab";
        private const string GeneratorVisualPath = "Assets/Synty/PolygonConstruction/Prefabs/Props/SM_Prop_Generator_Large_01.prefab";
        private const string CrateVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Props/SM_Prop_Crate_02.prefab";
        private const string PipeVisualPath = "Assets/Synty/PolygonGeneric/Prefabs/Building/SM_Gen_Bld_Pipe_Straight_03.prefab";

        private static readonly string[] GeneratedInputAssetPaths =
        {
            MoveReferencePath,
            LookReferencePath,
            JumpReferencePath,
            SprintReferencePath,
            DashReferencePath,
            InteractReferencePath,
            PauseReferencePath,
            InputActionsPath,
            LegacyInputActionsPath
        };

        [InitializeOnLoadMethod]
        private static void QueueFirstBuild()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/Platformer Ultra/Build Prototype Assets and Conveyor Test Scene")]
        public static void BuildAll()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(InputFolder);
            EnsureFolder(AnimationFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(UiFolder);

            GeometricCharacterAssetFactory.BuildAssets();

            PrototypeMaterials materials = BuildMaterials();
            PlayerMovementSettings movementSettings = BuildMovementSettings();
            PrototypeInputReferences input = BuildInputAssets();
            PanelSettings panelSettings = BuildPanelSettings();
            BuildPlatformPrefab(materials.Platform);
            BuildOrePrefab(materials.Ore);
            BuildPortalCorePrefab(materials.PortalCore, materials.Accent);
            BuildTerminalPrefab(materials.Frame, materials.Accent);
            BuildPlayerPrefab(movementSettings, input);
            BuildTestScene(materials, movementSettings, input, panelSettings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddSceneToBuildSettings();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Prototype assets and conveyor test scene built at {ScenePath}.");
        }

        [MenuItem("Tools/Platformer Ultra/Character Art/Rebuild Player Character")]
        public static void BuildPlayerCharacterOnly()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(InputFolder);
            EnsureFolder(PrefabFolder);
            GeometricCharacterAssetFactory.BuildAssets();
            PlayerMovementSettings movementSettings = BuildMovementSettings();
            PrototypeInputReferences input = BuildInputAssets();
            BuildPlayerPrefab(movementSettings, input);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Debug.Log("Rebuilt the player with the procedural maintenance-unit visual.");
        }

        private static void BuildIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(TerminalPrefabPath) == null)
            {
                BuildAll();
            }
        }

        private static PrototypeMaterials BuildMaterials()
        {
            return new PrototypeMaterials
            {
                Floor = CreateOrUpdateMaterial("M_Prototype_Floor", new Color(0.055f, 0.075f, 0.085f), 0.35f, 0.28f, Color.black),
                Platform = CreateOrUpdateMaterial("M_Prototype_Platform", new Color(0.17f, 0.21f, 0.23f), 0.65f, 0.48f, Color.black),
                Frame = CreateOrUpdateMaterial("M_Prototype_Frame", new Color(0.82f, 0.27f, 0.045f), 0.55f, 0.38f, Color.black),
                Accent = CreateOrUpdateMaterial("M_Prototype_Accent", new Color(0.1f, 0.72f, 0.88f), 0.25f, 0.7f, new Color(0.05f, 0.8f, 1.2f)),
                Hazard = CreateOrUpdateMaterial("M_Prototype_Hazard", new Color(1f, 0.62f, 0.04f), 0.15f, 0.3f, new Color(0.35f, 0.09f, 0f)),
                Ore = CreateOrUpdateMaterial("M_Prototype_Ore", new Color(0.18f, 0.25f, 0.28f), 0.7f, 0.32f, new Color(0f, 0.05f, 0.07f)),
                PortalCore = CreateOrUpdateMaterial("M_Prototype_PortalCore", new Color(0.18f, 0.9f, 1f), 0.15f, 0.82f, new Color(0.1f, 1.3f, 1.8f))
            };
        }

        private static PlayerMovementSettings BuildMovementSettings()
        {
            PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
                AssetDatabase.CreateAsset(settings, MovementSettingsPath);
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("_movementSpeed").floatValue = 3.525f;
            serializedSettings.FindProperty("_sprintSpeed").floatValue = 3.525f;
            serializedSettings.FindProperty("_groundAcceleration").floatValue = 18f;
            serializedSettings.FindProperty("_groundDeceleration").floatValue = 24f;
            serializedSettings.FindProperty("_airAcceleration").floatValue = 7f;
            serializedSettings.FindProperty("_dashDistance").floatValue = 2.4f;
            serializedSettings.FindProperty("_dashDuration").floatValue = 0.2f;
            serializedSettings.FindProperty("_dashCooldown").floatValue = 1.5f;
            serializedSettings.FindProperty("_dashInputBufferTime").floatValue = 0.12f;
            serializedSettings.FindProperty("_dashExitSpeed").floatValue = 3.525f;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static PrototypeInputReferences BuildInputAssets()
        {
            InputActionAsset existingAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            PrototypeInputReferences existing = new PrototypeInputReferences
            {
                Move = AssetDatabase.LoadAssetAtPath<InputActionReference>(MoveReferencePath),
                Look = AssetDatabase.LoadAssetAtPath<InputActionReference>(LookReferencePath),
                Jump = AssetDatabase.LoadAssetAtPath<InputActionReference>(JumpReferencePath),
                Sprint = AssetDatabase.LoadAssetAtPath<InputActionReference>(SprintReferencePath),
                Dash = AssetDatabase.LoadAssetAtPath<InputActionReference>(DashReferencePath),
                Interact = AssetDatabase.LoadAssetAtPath<InputActionReference>(InteractReferencePath),
                Pause = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseReferencePath)
            };
            if (existingAsset != null && existing.Move?.action != null && existing.Look?.action != null &&
                existing.Jump?.action != null && existing.Sprint?.action != null && existing.Dash?.action != null &&
                existing.Interact?.action != null && existing.Pause?.action != null)
            {
                return existing;
            }

            foreach (string path in GeneratedInputAssetPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "IA_Gameplay";
            InputActionMap gameplay = asset.AddActionMap("Gameplay");

            InputAction move = gameplay.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            InputAction look = gameplay.AddAction("Look", InputActionType.Value);
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            InputAction jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            InputAction sprint = gameplay.AddAction("Sprint", InputActionType.Button);

            InputAction dash = gameplay.AddAction("Dash", InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftShift");
            dash.AddBinding("<Gamepad>/leftStickPress");
            dash.AddBinding("<Gamepad>/buttonEast");

            InputAction interact = gameplay.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            InputAction pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            AssetDatabase.CreateAsset(asset, InputActionsPath);
            return new PrototypeInputReferences
            {
                Move = SaveActionReference(move, MoveReferencePath),
                Look = SaveActionReference(look, LookReferencePath),
                Jump = SaveActionReference(jump, JumpReferencePath),
                Sprint = SaveActionReference(sprint, SprintReferencePath),
                Dash = SaveActionReference(dash, DashReferencePath),
                Interact = SaveActionReference(interact, InteractReferencePath),
                Pause = SaveActionReference(pause, PauseReferencePath)
            };
        }

        private static InputActionReference SaveActionReference(InputAction action, string path)
        {
            InputActionReference reference = InputActionReference.Create(action);
            reference.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(reference, path);
            return reference;
        }

        private static PanelSettings BuildPanelSettings()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void ConfigureAnimationImports()
        {
            ConfigureAnimationImport(IdleAnimationPath, true, false);
            ConfigureAnimationImport(WalkingAnimationPath, true, false);
            ConfigureAnimationImport(RunningAnimationPath, true, false);
            ConfigureJumpAnimationImport();
            ConfigureAnimationImport(FallingIdleAnimationPath, true, false);
        }

        private static void ConfigureAnimationImport(string assetPath, bool loop, bool preserveHorizontalRootMotion)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Animation asset is missing or is not an FBX: {assetPath}");
                return;
            }

            bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                           importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                           !importer.importAnimation;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
                changed = true;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                ModelImporterClipAnimation clip = clips[index];
                changed |= clip.loopTime != loop ||
                           clip.loopPose != loop ||
                           !clip.lockRootRotation ||
                           !clip.lockRootHeightY ||
                           clip.lockRootPositionXZ == preserveHorizontalRootMotion ||
                           clip.keepOriginalPositionXZ != preserveHorizontalRootMotion;
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = false;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.lockRootPositionXZ = !preserveHorizontalRootMotion;
                clip.keepOriginalPositionXZ = preserveHorizontalRootMotion;
            }

            if (!changed)
            {
                return;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureJumpAnimationImport()
        {
            ModelImporter importer = AssetImporter.GetAtPath(JumpAnimationPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Animation asset is missing or is not an FBX: {JumpAnimationPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            ModelImporterClipAnimation sourceClip = importer.defaultClipAnimations[0];
            ModelImporterClipAnimation jumpStartClip = CreateJumpClip(sourceClip, "Jump Start", 0f, 22f, false);
            ModelImporterClipAnimation jumpAirborneClip = CreateJumpClip(sourceClip, JumpAirborneClipName, 30f, 50f, false);
            ModelImporterClipAnimation fullJumpClip = CreateJumpClip(sourceClip, "Jump 1", 0f, 65f, false);
            importer.clipAnimations = new[] { jumpStartClip, jumpAirborneClip, fullJumpClip };
            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation CreateJumpClip(
            ModelImporterClipAnimation sourceClip,
            string clipName,
            float firstFrame,
            float lastFrame,
            bool loop)
        {
            return new ModelImporterClipAnimation
            {
                name = clipName,
                takeName = sourceClip.takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loopPose = loop,
                lockRootRotation = true,
                keepOriginalOrientation = false,
                lockRootHeightY = true,
                keepOriginalPositionY = false,
                heightFromFeet = true,
                lockRootPositionXZ = true,
                keepOriginalPositionXZ = false
            };
        }

        private static RuntimeAnimatorController BuildPlayerAnimatorController(PlayerMovementSettings movementSettings)
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(PlayerAnimatorControllerPath);
            }

            AnimationClip idleClip = LoadAnimationClip(IdleAnimationPath);
            AnimationClip walkingClip = LoadAnimationClip(WalkingAnimationPath);
            AnimationClip runningClip = LoadAnimationClip(RunningAnimationPath);
            AnimationClip jumpClip = LoadAnimationClip(JumpAnimationPath, "Jump Start");
            AnimationClip jumpAirborneClip = LoadAnimationClip(JumpAnimationPath, JumpAirborneClipName);
            AnimationClip fallingIdleClip = LoadAnimationClip(FallingIdleAnimationPath);
            if (idleClip == null || walkingClip == null || runningClip == null ||
                jumpClip == null || jumpAirborneClip == null || fallingIdleClip == null)
            {
                throw new System.InvalidOperationException("One or more required player animation clips could not be loaded.");
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorControllerPath);
            controller.AddParameter(PlayerAnimationDriver.MoveSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimationDriver.LocomotionRateParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimationDriver.IsSprintingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimationDriver.IsGroundedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimationDriver.VerticalSpeedParameter, AnimatorControllerParameterType.Float);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(180f, 40f));
            AnimatorState walkingState = stateMachine.AddState("Walking", new Vector3(430f, 40f));
            AnimatorState runningState = stateMachine.AddState("Running", new Vector3(680f, 40f));
            AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(180f, 180f));
            AnimatorState jumpAirborneState = stateMachine.AddState("Jump Airborne", new Vector3(430f, 180f));
            AnimatorState fallingState = stateMachine.AddState("Falling Idle", new Vector3(430f, 180f));
            stateMachine.defaultState = idleState;

            idleState.motion = idleClip;
            walkingState.motion = walkingClip;
            walkingState.speedParameterActive = true;
            walkingState.speedParameter = PlayerAnimationDriver.LocomotionRateParameter;
            runningState.motion = runningClip;
            runningState.speedParameterActive = true;
            runningState.speedParameter = PlayerAnimationDriver.LocomotionRateParameter;
            jumpState.motion = jumpClip;
            jumpState.speed = CalculateJumpStartPlaybackSpeed(jumpClip, movementSettings);
            jumpAirborneState.motion = jumpAirborneClip;
            jumpAirborneState.speed = CalculateJumpAirbornePlaybackSpeed(jumpAirborneClip, movementSettings);
            fallingState.motion = fallingIdleClip;

            AnimatorStateTransition idleToWalking = AddImmediateTransition(idleState, walkingState, 0.12f);
            idleToWalking.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            idleToWalking.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            idleToWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsSprintingParameter);
            AnimatorStateTransition idleToRunning = AddImmediateTransition(idleState, runningState, 0.12f);
            idleToRunning.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            idleToRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            idleToRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsSprintingParameter);
            AnimatorStateTransition walkingToIdle = AddImmediateTransition(walkingState, idleState, 0.12f);
            walkingToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            walkingToIdle.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            AnimatorStateTransition walkingToRunning = AddImmediateTransition(walkingState, runningState, 0.1f);
            walkingToRunning.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            walkingToRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsSprintingParameter);
            walkingToRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            AnimatorStateTransition runningToWalking = AddImmediateTransition(runningState, walkingState, 0.1f);
            runningToWalking.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            runningToWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsSprintingParameter);
            runningToWalking.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            AnimatorStateTransition runningToIdle = AddImmediateTransition(runningState, idleState, 0.12f);
            runningToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            runningToIdle.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);

            AddAirborneTransitions(idleState, jumpState, fallingState);
            AddAirborneTransitions(walkingState, jumpState, fallingState);
            AddAirborneTransitions(runningState, jumpState, fallingState);

            AnimatorStateTransition jumpToAirborne = AddImmediateTransition(jumpState, jumpAirborneState, 0.1f);
            jumpToAirborne.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsGroundedParameter);
            jumpToAirborne.hasExitTime = true;
            jumpToAirborne.exitTime = 0.9f;

            AddLandingTransitions(jumpState, idleState, walkingState, runningState, 0.12f);
            AddLandingTransitions(jumpAirborneState, idleState, walkingState, runningState, 0.16f);
            AddLandingTransitions(fallingState, idleState, walkingState, runningState, 0.16f);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static float CalculateJumpStartPlaybackSpeed(
            AnimationClip jumpClip,
            PlayerMovementSettings movementSettings)
        {
            if (jumpClip == null || movementSettings == null || movementSettings.Gravity >= 0f)
            {
                return 1f;
            }

            float ascentDuration = Mathf.Sqrt(2f * movementSettings.JumpHeight / -movementSettings.Gravity);
            return ascentDuration > 0.01f ? jumpClip.length / ascentDuration : 1f;
        }

        private static float CalculateJumpAirbornePlaybackSpeed(
            AnimationClip jumpClip,
            PlayerMovementSettings movementSettings)
        {
            if (jumpClip == null || movementSettings == null || movementSettings.Gravity >= 0f)
            {
                return 1f;
            }

            float descentDuration = Mathf.Sqrt(2f * movementSettings.JumpHeight / -movementSettings.Gravity);
            return descentDuration > 0.01f ? jumpClip.length / descentDuration : 1f;
        }

        private static void AddLandingTransitions(
            AnimatorState airborneState,
            AnimatorState idleState,
            AnimatorState walkingState,
            AnimatorState runningState,
            float duration)
        {
            AnimatorStateTransition toIdle = AddImmediateTransition(airborneState, idleState, duration);
            toIdle.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);

            AnimatorStateTransition toWalking = AddImmediateTransition(airborneState, walkingState, duration);
            toWalking.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            toWalking.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            toWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsSprintingParameter);

            AnimatorStateTransition toRunning = AddImmediateTransition(airborneState, runningState, duration);
            toRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsGroundedParameter);
            toRunning.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.MoveSpeedParameter);
            toRunning.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimationDriver.IsSprintingParameter);
        }

        private static void AddAirborneTransitions(
            AnimatorState source,
            AnimatorState jumpState,
            AnimatorState fallingState)
        {
            AnimatorStateTransition toJump = AddImmediateTransition(source, jumpState, 0.08f);
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsGroundedParameter);
            toJump.AddCondition(AnimatorConditionMode.Greater, 0.05f, PlayerAnimationDriver.VerticalSpeedParameter);

            AnimatorStateTransition toFalling = AddImmediateTransition(source, fallingState, 0.1f);
            toFalling.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimationDriver.IsGroundedParameter);
            toFalling.AddCondition(AnimatorConditionMode.Less, -0.05f, PlayerAnimationDriver.VerticalSpeedParameter);
        }

        private static AnimationClip LoadAnimationClip(string assetPath, string clipName = null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int index = 0; index < assets.Length; index++)
            {
                AnimationClip clip = assets[index] as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__") &&
                    (clipName == null || clip.name == clipName))
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimatorStateTransition AddImmediateTransition(
            AnimatorState source,
            AnimatorState destination,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureImmediateTransition(transition, duration);
            return transition;
        }

        private static void ConfigureImmediateTransition(AnimatorStateTransition transition, float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
        }

        private static void BuildPlayerPrefab(
            PlayerMovementSettings settings,
            PrototypeInputReferences input)
        {
            GameObject root = new GameObject("PF_Player_Prototype");
            try
            {
                SetLayerRecursively(root, 2);
                CharacterController characterController = root.AddComponent<CharacterController>();
                characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.height = 1.8f;
                characterController.radius = 0.42f;
                characterController.slopeLimit = 55f;
                characterController.stepOffset = 0.35f;

                Health health = root.AddComponent<Health>();
                FactionMember factionMember = root.AddComponent<FactionMember>();
                Targetable targetable = root.AddComponent<Targetable>();
                PlayerHealth playerHealth = root.AddComponent<PlayerHealth>();
                GameObject targetPointObject = new GameObject("Target Point");
                targetPointObject.transform.SetParent(root.transform, false);
                targetPointObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();
                targetable.Configure(factionMember, targetPoint, playerHealth, true);
                playerHealth.Configure(health, factionMember, targetable, 100, 0.35f);

                ConveyorPassenger passenger = root.AddComponent<ConveyorPassenger>();
                passenger.Configure(null, characterController, false);
                ThirdPersonPlayerController controller = root.AddComponent<ThirdPersonPlayerController>();
                PlayerInteractor interactor = root.AddComponent<PlayerInteractor>();
                interactor.Configure(null, input.Interact, null, ~0);

                GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    GeometricCharacterAssetFactory.PlayerVisualPrefabPath);
                if (visualPrefab == null)
                {
                    throw new System.InvalidOperationException(
                        $"Player visual prefab is missing: {GeometricCharacterAssetFactory.PlayerVisualPrefabPath}");
                }

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.name = "Maintenance Unit Visual";
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                SetLayerRecursively(visual, 2);
                foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>(true))
                {
                    visualCollider.enabled = false;
                }

                ProceduralPlayerAnimator proceduralAnimator = visual.GetComponent<ProceduralPlayerAnimator>();
                if (proceduralAnimator == null || !proceduralAnimator.RigConfigured)
                {
                    throw new System.InvalidOperationException(
                        "The maintenance-unit visual is missing its configured procedural rig.");
                }

                proceduralAnimator.BindController(controller);
                controller.Configure(
                    characterController,
                    null,
                    passenger,
                    settings,
                    input.Move,
                    input.Jump,
                    input.Sprint,
                    input.Dash);

                SetLayerRecursively(root, 2);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildTerminalPrefab(Material frameMaterial, Material accentMaterial)
        {
            GameObject root = new GameObject("PF_ConveyorRouteTerminal");
            try
            {
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.75f, 0f);
                collider.size = new Vector3(1.45f, 1.5f, 1.1f);

                CreatePrimitiveChild(
                    PrimitiveType.Cube,
                    "Terminal Pedestal",
                    root.transform,
                    new Vector3(0f, 0.43f, 0f),
                    new Vector3(0.82f, 0.86f, 0.68f),
                    frameMaterial,
                    false);

                GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConsoleVisualPath);
                if (visualPrefab != null)
                {
                    GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                    visual.name = "Synty Control Panel Visual";
                    visual.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.83f, 0f), Quaternion.identity);
                    visual.transform.localScale = Vector3.one * 0.38f;
                    foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true))
                    {
                        childCollider.enabled = false;
                    }
                }
                else
                {
                    CreatePrimitiveChild(
                        PrimitiveType.Cube,
                        "Terminal Body",
                        root.transform,
                        new Vector3(0f, 0.75f, 0f),
                        new Vector3(1.1f, 1.5f, 0.75f),
                        frameMaterial,
                        false);
                }

                GameObject indicator = CreatePrimitiveChild(
                    PrimitiveType.Cube,
                    "Route Indicator",
                    root.transform,
                    new Vector3(0f, 1.32f, -0.45f),
                    new Vector3(0.62f, 0.13f, 0.06f),
                    accentMaterial,
                    false);

                ConveyorRouteTerminal terminal = root.AddComponent<ConveyorRouteTerminal>();
                terminal.Configure(null, null, null, indicator.GetComponent<Renderer>());
                root.AddComponent<InteractionTarget>().Configure(terminal);
                PrefabUtility.SaveAsPrefabAsset(root, TerminalPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildPlatformPrefab(Material material)
        {
            GameObject root = new GameObject("PF_FactoryPlatform_4x4");
            try
            {
                CreatePrimitiveChild(
                    PrimitiveType.Cube,
                    "Platform Deck",
                    root.transform,
                    Vector3.zero,
                    new Vector3(4f, 0.5f, 4f),
                    material,
                    true);
                PrefabUtility.SaveAsPrefabAsset(root, PlatformPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildOrePrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PF_OreChunk_Prototype";
            try
            {
                root.transform.localScale = new Vector3(0.48f, 0.38f, 0.42f);
                root.transform.localRotation = Quaternion.Euler(18f, 34f, 8f);
                root.GetComponent<Renderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(root, OrePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildPortalCorePrefab(Material coreMaterial, Material accentMaterial)
        {
            GameObject root = new GameObject("PF_PortalCore_Prototype");
            try
            {
                CreatePrimitiveChild(
                    PrimitiveType.Sphere,
                    "Energy Core",
                    root.transform,
                    Vector3.zero,
                    Vector3.one * 0.85f,
                    coreMaterial,
                    false);
                CreatePrimitiveChild(
                    PrimitiveType.Cylinder,
                    "Containment Ring Horizontal",
                    root.transform,
                    Vector3.zero,
                    new Vector3(0.62f, 0.055f, 0.62f),
                    accentMaterial,
                    false);
                GameObject verticalRing = CreatePrimitiveChild(
                    PrimitiveType.Cylinder,
                    "Containment Ring Vertical",
                    root.transform,
                    Vector3.zero,
                    new Vector3(0.62f, 0.055f, 0.62f),
                    accentMaterial,
                    false);
                verticalRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, PortalCorePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildTestScene(
            PrototypeMaterials materials,
            PlayerMovementSettings movementSettings,
            PrototypeInputReferences input,
            PanelSettings panelSettings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ConveyorTestScene";

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.21f, 0.25f);
            RenderSettings.ambientEquatorColor = new Color(0.075f, 0.095f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.03f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.065f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 48f;

            GameObject environment = new GameObject("Environment");
            CreateCube("Factory Floor", environment.transform, new Vector3(0f, -0.5f, 3f), new Vector3(30f, 1f, 30f), materials.Floor, true);
            CreateCube("Rear Wall", environment.transform, new Vector3(0f, 6f, 17.5f), new Vector3(30f, 12f, 1f), materials.Floor, true);
            CreateCube("Left Wall", environment.transform, new Vector3(-14.5f, 4f, 3f), new Vector3(1f, 8f, 30f), materials.Floor, true);
            CreateCube("Right Wall", environment.transform, new Vector3(14.5f, 4f, 3f), new Vector3(1f, 8f, 30f), materials.Floor, true);

            GameObject platformGroup = new GameObject("Platforming Blockout");
            platformGroup.transform.SetParent(environment.transform, false);
            CreateCube("Low Loading Pad", platformGroup.transform, new Vector3(-6f, 0.25f, 2f), new Vector3(4f, 0.5f, 5f), materials.Platform, true);
            CreateCube("Middle Platform", platformGroup.transform, new Vector3(5f, 2.15f, 5f), new Vector3(5f, 0.5f, 5f), materials.Platform, true);
            CreateCube("Upper Platform", platformGroup.transform, new Vector3(-1f, 4.75f, 9f), new Vector3(5f, 0.5f, 5f), materials.Platform, true);
            CreateCube("Jump Step 1", platformGroup.transform, new Vector3(8f, 0.35f, -1f), new Vector3(2.5f, 0.7f, 2.5f), materials.Platform, true);
            CreateCube("Jump Step 2", platformGroup.transform, new Vector3(9f, 1.1f, 1.2f), new Vector3(2.5f, 0.7f, 2.5f), materials.Platform, true);
            CreateCube("Jump Step 3", platformGroup.transform, new Vector3(8f, 1.85f, 3.4f), new Vector3(2.5f, 0.7f, 2.5f), materials.Platform, true);

            GameObject visualObstruction = CreateCube(
                "Non-Colliding Pipe Chase - Conveyor May Clip Through",
                environment.transform,
                new Vector3(-1.7f, 1.15f, 2f),
                new Vector3(1.1f, 3f, 1.1f),
                materials.Hazard,
                false);
            PlacePrefab(PipeVisualPath, visualObstruction.transform.position, Quaternion.Euler(0f, 0f, 90f), Vector3.one * 1.25f, environment.transform, "Pipe Detail");

            GameObject conveyorRig = new GameObject("Runtime Conveyor Generation Rig");
            ConveyorEndpoint startEndpoint = CreateEndpoint("Start Socket", conveyorRig.transform, new Vector3(-6f, 0.55f, 2f), ConveyorEndpointKind.Output, materials.Accent);
            ConveyorEndpoint routeA = CreateEndpoint("Route A - Straight", conveyorRig.transform, new Vector3(2f, 0.55f, 2f), ConveyorEndpointKind.Input, materials.Accent);
            ConveyorEndpoint routeB = CreateEndpoint("Route B - Middle Platform", conveyorRig.transform, new Vector3(5f, 2.55f, 5f), ConveyorEndpointKind.Input, materials.Hazard);
            ConveyorEndpoint routeC = CreateEndpoint("Route C - Upper Platform", conveyorRig.transform, new Vector3(-1f, 5.15f, 9f), ConveyorEndpointKind.Input, materials.PortalCore);

            GameObject conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            GameObject conveyorObject = conveyorPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(conveyorPrefab)
                : new GameObject("Generated Point-to-Point Conveyor");
            conveyorObject.name = "Generated Point-to-Point Conveyor";
            conveyorObject.transform.SetParent(conveyorRig.transform, true);
            if (PrefabUtility.IsPartOfPrefabInstance(conveyorObject))
            {
                PrefabUtility.UnpackPrefabInstance(
                    conveyorObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            ConveyorBelt conveyor = conveyorObject.GetComponent<ConveyorBelt>();
            if (conveyor == null)
            {
                conveyor = conveyorObject.AddComponent<ConveyorBelt>();
            }

            ConveyorEndpoint[] prefabEndpoints = conveyorObject.GetComponentsInChildren<ConveyorEndpoint>(true);
            conveyor.SetEndpoints(startEndpoint, routeA);
            foreach (ConveyorEndpoint endpoint in prefabEndpoints)
            {
                Object.DestroyImmediate(endpoint.gameObject);
            }
            conveyor.RebuildNow();

            GameObject terminalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerminalPrefabPath);
            GameObject terminalObject = (GameObject)PrefabUtility.InstantiatePrefab(terminalPrefab);
            terminalObject.name = "Conveyor Route Terminal";
            terminalObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -5f), Quaternion.identity);
            ConveyorRouteTerminal terminal = terminalObject.GetComponent<ConveyorRouteTerminal>();
            Renderer indicator = terminalObject.transform.Find("Route Indicator")?.GetComponent<Renderer>();
            terminal.Configure(conveyor, startEndpoint, new[] { routeA, routeB, routeC }, indicator);

            GameObject lighting = new GameObject("Lighting");
            Light sun = new GameObject("Factory Key Light").AddComponent<Light>();
            sun.transform.SetParent(lighting.transform, false);
            sun.type = LightType.Directional;
            sun.color = new Color(0.72f, 0.84f, 1f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            CreatePointLight("Terminal Work Light", lighting.transform, new Vector3(0f, 3.5f, -3f), new Color(1f, 0.35f, 0.08f), 7f, 5f);
            CreatePointLight("Upper Factory Light", lighting.transform, new Vector3(-1f, 8f, 9f), new Color(0.1f, 0.75f, 1f), 11f, 7f);

            GameObject portalCorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalCorePrefabPath);
            if (portalCorePrefab != null)
            {
                GameObject core = (GameObject)PrefabUtility.InstantiatePrefab(portalCorePrefab);
                core.name = "Portal Core Goal Marker";
                core.transform.position = new Vector3(-1f, 6.25f, 9f);
            }

            PlacePrefab(GeneratorVisualPath, new Vector3(10f, 0f, 10f), Quaternion.Euler(0f, -90f, 0f), Vector3.one, environment.transform, "Dormant Generator Set Dressing");
            PlacePrefab(CrateVisualPath, new Vector3(-10f, 0f, 7f), Quaternion.Euler(0f, 24f, 0f), Vector3.one, environment.transform, "Cargo Crate A");
            PlacePrefab(CrateVisualPath, new Vector3(-11.2f, 0f, 8.1f), Quaternion.Euler(0f, -12f, 0f), Vector3.one, environment.transform, "Cargo Crate B");

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -8f), Quaternion.identity);
            SetLayerRecursively(player, 2);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 66f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 120f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonOrbitCamera orbitCamera = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbitCamera.Configure(player.transform, input.Look, ~(1 << 2));
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, -13f), Quaternion.identity);

            GameObject hudObject = new GameObject("Prototype HUD");
            UIDocument document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudLayoutPath);
            InteractionPromptPresenter prompt = hudObject.AddComponent<InteractionPromptPresenter>();
            prompt.Configure(document, AssetDatabase.LoadAssetAtPath<StyleSheet>(HudStylePath));

            CharacterController characterController = player.GetComponent<CharacterController>();
            ConveyorPassenger passenger = player.GetComponent<ConveyorPassenger>();
            passenger.Configure(null, characterController, false);
            player.GetComponent<ThirdPersonPlayerController>().Configure(
                characterController,
                cameraObject.transform,
                passenger,
                movementSettings,
                input.Move,
                input.Jump,
                input.Sprint,
                input.Dash);
            player.GetComponent<PlayerInteractor>().Configure(cameraObject.transform, input.Interact, prompt, ~(1 << 2));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static ConveyorEndpoint CreateEndpoint(
            string objectName,
            Transform parent,
            Vector3 position,
            ConveyorEndpointKind kind,
            Material beaconMaterial)
        {
            GameObject endpointObject = new GameObject(objectName);
            endpointObject.transform.SetParent(parent, true);
            endpointObject.transform.position = position;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind, 0.25f);
            CreatePrimitiveChild(
                PrimitiveType.Cylinder,
                "Socket Beacon",
                endpointObject.transform,
                new Vector3(0f, -0.08f, 0f),
                new Vector3(0.42f, 0.06f, 0.42f),
                beaconMaterial,
                false);
            return endpoint;
        }

        private static void CreatePointLight(
            string objectName,
            Transform parent,
            Vector3 position,
            Color color,
            float range,
            float intensity)
        {
            Light light = new GameObject(objectName).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static GameObject PlacePrefab(
            string assetPath,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Transform parent,
            string objectName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = objectName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            return instance;
        }

        private static GameObject CreateCube(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, true);
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private static GameObject CreatePrimitiveChild(
            PrimitiveType primitiveType,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            if (material != null)
            {
                primitive.GetComponent<Renderer>().sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Object.DestroyImmediate(primitive.GetComponent<Collider>());
            }

            return primitive;
        }

        private static Material CreateOrUpdateMaterial(
            string assetName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emission)
        {
            string path = MaterialFolder + "/" + assetName + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            for (int index = 0; index < target.transform.childCount; index++)
            {
                SetLayerRecursively(target.transform.GetChild(index).gameObject, layer);
            }
        }

        private sealed class PrototypeInputReferences
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Jump;
            public InputActionReference Sprint;
            public InputActionReference Dash;
            public InputActionReference Interact;
            public InputActionReference Pause;
        }

        private sealed class PrototypeMaterials
        {
            public Material Floor;
            public Material Platform;
            public Material Frame;
            public Material Accent;
            public Material Hazard;
            public Material Ore;
            public Material PortalCore;
        }
    }
}
