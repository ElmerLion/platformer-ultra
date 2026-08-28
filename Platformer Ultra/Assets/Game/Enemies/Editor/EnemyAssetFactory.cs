using System;
using System.Collections.Generic;
using PlatformerUltra.CharacterArt.Editor;
using PlatformerUltra.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace PlatformerUltra.Enemies.Editor
{
    public static class EnemyAssetFactory
    {
        public const string DronePrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Drone.prefab";
        public const string SaboteurPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Saboteur.prefab";
        public const string ArmoredPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Armored.prefab";
        public const string DroneProjectilePrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_DroneBolt.prefab";
        public const string DeathExplosionPrefabPath = "Assets/Game/Combat/VFX/PF_MechanicalDeathExplosion.prefab";
        public const string MeleeImpactEffectPath = "Assets/Game/Combat/VFX/PF_VFX_MeleeImpact.prefab";
        public const string ArmoredSlamEffectPath = "Assets/Game/Combat/VFX/PF_VFX_ArmoredSlam.prefab";
        public const string DroneImpactEffectPath = "Assets/Game/Combat/VFX/PF_VFX_DroneImpact.prefab";
        public const string PlayerJumpEffectPath = "Assets/Game/Combat/VFX/PF_VFX_PlayerJump.prefab";
        public const string DoubleJumpEffectPath = "Assets/Game/Combat/VFX/PF_VFX_DoubleJump.prefab";
        public const string PlayerDashEffectPath = "Assets/Game/Combat/VFX/PF_VFX_PlayerDash.prefab";
        public const string PlayerHitEffectPath = "Assets/Game/Combat/VFX/PF_VFX_PlayerHit.prefab";
        public const string MachineBreakEffectPath = "Assets/Game/Combat/VFX/PF_VFX_MachineBreak.prefab";
        public const string RepairLoopEffectPath = "Assets/Game/Combat/VFX/PF_VFX_RepairLoop.prefab";

        public const string DroneDefinitionPath = "Assets/Game/Enemies/Data/DA_Enemy_Drone.asset";
        public const string SaboteurDefinitionPath = "Assets/Game/Enemies/Data/DA_Enemy_Saboteur.asset";
        public const string ArmoredDefinitionPath = "Assets/Game/Enemies/Data/DA_Enemy_Armored.asset";

        public const string SaboteurControllerPath = "Assets/Game/Enemies/Animations/AC_Enemy_Saboteur.controller";
        public const string ArmoredControllerPath = "Assets/Game/Enemies/Animations/AC_Enemy_Armored.controller";

        private const string EnemyRoot = "Assets/Game/Enemies";
        private const string DataFolder = EnemyRoot + "/Data";
        private const string AnimationFolder = EnemyRoot + "/Animations";
        private const string MaterialFolder = EnemyRoot + "/Materials";
        private const string PrefabFolder = EnemyRoot + "/Prefabs";
        private const string DeathVfxFolder = "Assets/Game/Combat/VFX";
        private const string DeathGlowMaterialPath = DeathVfxFolder + "/M_DeathExplosion_Glow.mat";
        private const string DeathSmokeMaterialPath = DeathVfxFolder + "/M_DeathExplosion_Smoke.mat";
        private const string DeathParticleTexturePath = DeathVfxFolder + "/T_DeathExplosion_SoftDisc.asset";
        private const string GameplayGlowMaterialPath = DeathVfxFolder + "/M_GameplayVfx_Glow.mat";
        private const string GameplaySmokeMaterialPath = DeathVfxFolder + "/M_GameplayVfx_Smoke.mat";

        private const string DroneVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Veh_Drone_Attach_01.prefab";
        private const string SaboteurVisualPath = GeometricCharacterAssetFactory.SaboteurVisualPrefabPath;
        private const string ArmoredVisualPath = GeometricCharacterAssetFactory.ArmoredVisualPrefabPath;

        private const string IdleAnimationPath = "Assets/Animations/Paladin J Nordstrom@Idle.fbx";
        private const string WalkingAnimationPath = "Assets/Animations/Paladin J Nordstrom@Walking.fbx";
        private const string RunningAnimationPath = "Assets/Animations/Paladin J Nordstrom@Standard Run.fbx";
        private const string ZombieAttackAnimationPath = "Assets/Animations/Paladin J Nordstrom@Zombie Attack.fbx";
        private const string ZombieDeathAnimationPath = "Assets/Animations/Paladin J Nordstrom@Zombie Death.fbx";
        private const string MutantSwipeAnimationPath = "Assets/Animations/Paladin J Nordstrom@Mutant Swiping.fbx";
        private const string MutantJumpAttackAnimationPath = "Assets/Animations/Paladin J Nordstrom@Mutant Jump Attack.fbx";
        private const string DyingAnimationPath = "Assets/Animations/Paladin J Nordstrom@Dying.fbx";

        private const string DroneBoltMaterialPath = MaterialFolder + "/M_Enemy_DroneBolt.mat";
        private const string DroneTelegraphMaterialPath = MaterialFolder + "/M_Enemy_DroneTelegraph.mat";
        private const string EnemyLayerName = "Enemy";

        // These normalized times are owned by the factory and are also used by the
        // generated definitions. They intentionally remain data, not runtime magic.
        private const float ZombieImpactNormalizedTime = 0.46f;
        private const float SwipeImpactNormalizedTime = 0.43f;
        private const float JumpImpactNormalizedTime = 0.60f;
        private const float DeathTailDuration = 0.4f;

        [MenuItem("Tools/Factory/Build Enemy Assets")]
        public static void BuildAll()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(AnimationFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(DeathVfxFolder);

            GeometricCharacterAssetFactory.BuildAssets();

            int enemyLayer = EnsureLayer(EnemyLayerName);
            Material boltMaterial = CreateOrUpdateEmissionMaterial(
                DroneBoltMaterialPath,
                new Color(0.08f, 0.72f, 1f, 1f),
                new Color(0.05f, 1.8f, 4.5f, 1f));
            Material telegraphMaterial = CreateOrUpdateEmissionMaterial(
                DroneTelegraphMaterialPath,
                new Color(1f, 0.38f, 0.04f, 1f),
                new Color(4.5f, 0.7f, 0.03f, 1f));
            GameObject deathExplosionPrefab = BuildDeathExplosionPrefab();
            GameplayEffectAssets effectAssets = BuildGameplayEffectAssets();
            GameObject projectilePrefab = BuildDroneProjectilePrefab(
                enemyLayer,
                boltMaterial,
                effectAssets.DroneImpact);

            GameObject droneVisual = RequireAsset<GameObject>(DroneVisualPath);
            GameObject saboteurVisual = RequireAsset<GameObject>(SaboteurVisualPath);
            GameObject armoredVisual = RequireAsset<GameObject>(ArmoredVisualPath);

            EnemyDefinition droneDefinition = BuildDroneDefinition(droneVisual, projectilePrefab);
            EnemyDefinition saboteurDefinition = BuildSaboteurDefinition(saboteurVisual);
            EnemyDefinition armoredDefinition = BuildArmoredDefinition(armoredVisual);

            GameObject dronePrefab = BuildEnemyPrefab(new EnemyPrefabSpec(
                "PF_Enemy_Drone",
                DronePrefabPath,
                droneDefinition,
                droneVisual,
                null,
                EnemyArchetype.Drone,
                enemyLayer,
                telegraphMaterial), deathExplosionPrefab, effectAssets.DroneImpact, null, null);
            GameObject saboteurPrefab = BuildEnemyPrefab(new EnemyPrefabSpec(
                "PF_Enemy_Saboteur",
                SaboteurPrefabPath,
                saboteurDefinition,
                saboteurVisual,
                null,
                EnemyArchetype.Saboteur,
                enemyLayer,
                null), deathExplosionPrefab, effectAssets.MeleeImpact, null, null);
            GameObject armoredPrefab = BuildEnemyPrefab(new EnemyPrefabSpec(
                "PF_Enemy_Armored",
                ArmoredPrefabPath,
                armoredDefinition,
                armoredVisual,
                null,
                EnemyArchetype.Armored,
                enemyLayer,
                null), deathExplosionPrefab, effectAssets.MeleeImpact, effectAssets.ArmoredSlam, effectAssets.PlayerJump);

            droneDefinition.SetSpawnPrefab(dronePrefab);
            saboteurDefinition.SetSpawnPrefab(saboteurPrefab);
            armoredDefinition.SetSpawnPrefab(armoredPrefab);
            EditorUtility.SetDirty(droneDefinition);
            EditorUtility.SetDirty(saboteurDefinition);
            EditorUtility.SetDirty(armoredDefinition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = dronePrefab;
            Debug.Log("Enemy assets built or refreshed without replacing existing asset files.");
        }

        [MenuItem("Tools/Factory/Build Player Dash Effect")]
        public static void BuildPlayerDashEffectOnly()
        {
            EnsureFolder(DeathVfxFolder);
            Texture2D softDisc = CreateOrUpdateSoftDiscTexture();
            Material glowMaterial = CreateOrUpdateParticleMaterial(
                GameplayGlowMaterialPath,
                softDisc,
                Color.white,
                true);
            Material smokeMaterial = CreateOrUpdateParticleMaterial(
                GameplaySmokeMaterialPath,
                softDisc,
                Color.white,
                false);
            BuildDashGameplayEffect(glowMaterial, smokeMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player dash effect rebuilt.");
        }

        private static void ConfigureAnimationImports()
        {
            ConfigureAnimationImport(IdleAnimationPath, true, null, 0f);
            ConfigureAnimationImport(WalkingAnimationPath, true, null, 0f);
            ConfigureAnimationImport(RunningAnimationPath, true, null, 0f);
            ConfigureAnimationImport(
                ZombieAttackAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnAttackImpact),
                ZombieImpactNormalizedTime);
            ConfigureAnimationImport(ZombieDeathAnimationPath, false, null, 0f);
            ConfigureAnimationImport(
                MutantSwipeAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnAttackImpact),
                SwipeImpactNormalizedTime);
            ConfigureAnimationImport(
                MutantJumpAttackAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnSpecialImpact),
                JumpImpactNormalizedTime);
            ConfigureAnimationImport(DyingAnimationPath, false, null, 0f);
        }

        private static void ConfigureAnimationImport(
            string assetPath,
            bool loop,
            string impactFunction,
            float impactNormalizedTime)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Required animation FBX is missing: " + assetPath);
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
                           clip.keepOriginalOrientation ||
                           !clip.lockRootHeightY ||
                           clip.keepOriginalPositionY ||
                           !clip.heightFromFeet ||
                           !clip.lockRootPositionXZ ||
                           clip.keepOriginalPositionXZ;

                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = false;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = false;

                AnimationEvent[] desiredEvents = BuildAnimationEvents(
                    clip.events,
                    impactFunction,
                    impactNormalizedTime);
                if (!AnimationEventsMatch(clip.events, desiredEvents))
                {
                    clip.events = desiredEvents;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationEvent[] BuildAnimationEvents(
            AnimationEvent[] existingEvents,
            string impactFunction,
            float impactNormalizedTime)
        {
            List<AnimationEvent> events = new List<AnimationEvent>();
            if (existingEvents != null)
            {
                for (int index = 0; index < existingEvents.Length; index++)
                {
                    AnimationEvent animationEvent = existingEvents[index];
                    if (animationEvent.functionName != nameof(EnemyAnimationEventRelay.OnAttackImpact) &&
                        animationEvent.functionName != nameof(EnemyAnimationEventRelay.OnSpecialImpact))
                    {
                        events.Add(animationEvent);
                    }
                }
            }

            if (!string.IsNullOrEmpty(impactFunction))
            {
                // ModelImporterClipAnimation stores event time in normalized clip
                // coordinates. Supplying seconds here is silently clamped to the
                // clip end when Unity imports the FBX.
                float eventTime = Mathf.Clamp(impactNormalizedTime, 0.01f, 0.99f);
                events.Add(new AnimationEvent
                {
                    functionName = impactFunction,
                    time = eventTime,
                    messageOptions = SendMessageOptions.RequireReceiver
                });
            }

            events.Sort((left, right) => left.time.CompareTo(right.time));
            return events.ToArray();
        }

        private static bool AnimationEventsMatch(AnimationEvent[] current, AnimationEvent[] desired)
        {
            current = current ?? Array.Empty<AnimationEvent>();
            desired = desired ?? Array.Empty<AnimationEvent>();
            if (current.Length != desired.Length)
            {
                return false;
            }

            for (int index = 0; index < current.Length; index++)
            {
                AnimationEvent left = current[index];
                AnimationEvent right = desired[index];
                if (left.functionName != right.functionName ||
                    Mathf.Abs(left.time - right.time) > 0.0001f ||
                    left.stringParameter != right.stringParameter ||
                    left.intParameter != right.intParameter ||
                    Mathf.Abs(left.floatParameter - right.floatParameter) > 0.0001f ||
                    left.objectReferenceParameter != right.objectReferenceParameter ||
                    left.messageOptions != right.messageOptions)
                {
                    return false;
                }
            }

            return true;
        }

        private static AnimatorController BuildSaboteurController(
            AnimationClip idleClip,
            AnimationClip walkingClip,
            AnimationClip runningClip,
            AnimationClip attackClip,
            AnimationClip deathClip)
        {
            AnimatorController controller = LoadOrCreateController(SaboteurControllerPath);
            AnimatorStateMachine stateMachine = ResetController(controller);
            AddEnemyParameters(controller, false);

            AnimatorState idle = AddState(stateMachine, "Idle", idleClip, new Vector3(170f, 40f), false);
            AnimatorState walk = AddState(stateMachine, "Walk", walkingClip, new Vector3(420f, 10f), true);
            AnimatorState run = AddState(stateMachine, "Run", runningClip, new Vector3(420f, 100f), true);
            AnimatorState attack = AddState(stateMachine, "Zombie Attack", attackClip, new Vector3(680f, 55f), false);
            AnimatorState death = AddState(stateMachine, "Zombie Death", deathClip, new Vector3(680f, 165f), false);
            stateMachine.defaultState = idle;

            AddLocomotionTransition(idle, walk, AnimatorConditionMode.Greater, 0.05f, false);
            AddLocomotionTransition(idle, run, AnimatorConditionMode.Greater, 0.05f, true);
            AddSpeedTransition(walk, idle, AnimatorConditionMode.Less, 0.05f);
            AddSpeedTransition(run, idle, AnimatorConditionMode.Less, 0.05f);
            AddChaseTransition(walk, run, true);
            AddChaseTransition(run, walk, false);

            AddAnyStateTriggerTransition(
                stateMachine,
                attack,
                EnemyAnimatorDriver.AttackParameter,
                0.08f);
            AddAttackExitTransitions(attack, idle, walk, run);
            AddAnyStateTriggerTransition(
                stateMachine,
                death,
                EnemyAnimatorDriver.DeathParameter,
                0.08f);

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController BuildArmoredController(
            AnimationClip idleClip,
            AnimationClip walkingClip,
            AnimationClip swipeClip,
            AnimationClip jumpAttackClip,
            AnimationClip deathClip)
        {
            AnimatorController controller = LoadOrCreateController(ArmoredControllerPath);
            AnimatorStateMachine stateMachine = ResetController(controller);
            AddEnemyParameters(controller, true);

            AnimatorState idle = AddState(stateMachine, "Idle", idleClip, new Vector3(170f, 50f), false);
            AnimatorState walk = AddState(stateMachine, "Walk", walkingClip, new Vector3(420f, 50f), true);
            AnimatorState swipe = AddState(stateMachine, "Mutant Swipe", swipeClip, new Vector3(680f, 10f), false);
            AnimatorState jump = AddState(stateMachine, "Mutant Jump Attack", jumpAttackClip, new Vector3(680f, 110f), false);
            AnimatorState death = AddState(stateMachine, "Dying", deathClip, new Vector3(930f, 60f), false);
            stateMachine.defaultState = idle;

            AddSpeedTransition(idle, walk, AnimatorConditionMode.Greater, 0.05f);
            AddSpeedTransition(walk, idle, AnimatorConditionMode.Less, 0.05f);
            AddAnyStateTriggerTransition(
                stateMachine,
                swipe,
                EnemyAnimatorDriver.AttackParameter,
                0.08f);
            AddAnyStateTriggerTransition(
                stateMachine,
                jump,
                EnemyAnimatorDriver.SpecialAttackParameter,
                0.05f);
            AddAttackExitTransitions(swipe, idle, walk, null);
            AddAttackExitTransitions(jump, idle, walk, null);
            AddAnyStateTriggerTransition(
                stateMachine,
                death,
                EnemyAnimatorDriver.DeathParameter,
                0.08f);

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController LoadOrCreateController(string path)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller == null)
            {
                throw new InvalidOperationException("Could not create Animator Controller: " + path);
            }

            return controller;
        }

        private static AnimatorStateMachine ResetController(AnimatorController controller)
        {
            for (int index = controller.parameters.Length - 1; index >= 0; index--)
            {
                controller.RemoveParameter(index);
            }

            AnimatorControllerLayer[] layers = controller.layers;
            if (layers == null || layers.Length == 0 || layers[0].stateMachine == null)
            {
                throw new InvalidOperationException("Animator Controller has no base state machine: " + controller.name);
            }

            if (layers.Length > 1)
            {
                controller.layers = new[] { layers[0] };
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorStateTransition[] anyTransitions = stateMachine.anyStateTransitions;
            for (int index = 0; index < anyTransitions.Length; index++)
            {
                stateMachine.RemoveAnyStateTransition(anyTransitions[index]);
            }

            AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;
            for (int index = 0; index < entryTransitions.Length; index++)
            {
                stateMachine.RemoveEntryTransition(entryTransitions[index]);
            }

            ChildAnimatorState[] states = stateMachine.states;
            for (int index = 0; index < states.Length; index++)
            {
                stateMachine.RemoveState(states[index].state);
            }

            ChildAnimatorStateMachine[] childStateMachines = stateMachine.stateMachines;
            for (int index = 0; index < childStateMachines.Length; index++)
            {
                stateMachine.RemoveStateMachine(childStateMachines[index].stateMachine);
            }

            stateMachine.defaultState = null;
            return stateMachine;
        }

        private static void AddEnemyParameters(AnimatorController controller, bool includeSpecial)
        {
            controller.AddParameter(EnemyAnimatorDriver.MoveSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyAnimatorDriver.LocomotionRateParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyAnimatorDriver.ChasingPlayerParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(EnemyAnimatorDriver.AttackParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(EnemyAnimatorDriver.DeathParameter, AnimatorControllerParameterType.Trigger);
            if (includeSpecial)
            {
                controller.AddParameter(EnemyAnimatorDriver.SpecialAttackParameter, AnimatorControllerParameterType.Trigger);
            }
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string name,
            Motion motion,
            Vector3 position,
            bool scalePlayback)
        {
            AnimatorState state = stateMachine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            if (scalePlayback)
            {
                state.speedParameterActive = true;
                state.speedParameter = EnemyAnimatorDriver.LocomotionRateParameter;
            }

            return state;
        }

        private static void AddLocomotionTransition(
            AnimatorState source,
            AnimatorState destination,
            AnimatorConditionMode speedMode,
            float threshold,
            bool chasingPlayer)
        {
            AnimatorStateTransition transition = AddImmediateTransition(source, destination, 0.1f);
            transition.AddCondition(speedMode, threshold, EnemyAnimatorDriver.MoveSpeedParameter);
            transition.AddCondition(
                chasingPlayer ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                EnemyAnimatorDriver.ChasingPlayerParameter);
        }

        private static void AddSpeedTransition(
            AnimatorState source,
            AnimatorState destination,
            AnimatorConditionMode conditionMode,
            float threshold)
        {
            AnimatorStateTransition transition = AddImmediateTransition(source, destination, 0.1f);
            transition.AddCondition(conditionMode, threshold, EnemyAnimatorDriver.MoveSpeedParameter);
        }

        private static void AddChaseTransition(AnimatorState source, AnimatorState destination, bool chasingPlayer)
        {
            AnimatorStateTransition transition = AddImmediateTransition(source, destination, 0.08f);
            transition.AddCondition(AnimatorConditionMode.Greater, 0.05f, EnemyAnimatorDriver.MoveSpeedParameter);
            transition.AddCondition(
                chasingPlayer ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                EnemyAnimatorDriver.ChasingPlayerParameter);
        }

        private static void AddAnyStateTriggerTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destination,
            string trigger,
            float duration)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
            ConfigureImmediateTransition(transition, duration);
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddAttackExitTransitions(
            AnimatorState attack,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState run)
        {
            AnimatorStateTransition toIdle = AddExitTimeTransition(attack, idle, 0.08f);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, EnemyAnimatorDriver.MoveSpeedParameter);

            AnimatorStateTransition toWalk = AddExitTimeTransition(attack, walk, 0.08f);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.05f, EnemyAnimatorDriver.MoveSpeedParameter);
            if (run != null)
            {
                toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, EnemyAnimatorDriver.ChasingPlayerParameter);
                AnimatorStateTransition toRun = AddExitTimeTransition(attack, run, 0.08f);
                toRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, EnemyAnimatorDriver.MoveSpeedParameter);
                toRun.AddCondition(AnimatorConditionMode.If, 0f, EnemyAnimatorDriver.ChasingPlayerParameter);
            }
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

        private static AnimatorStateTransition AddExitTimeTransition(
            AnimatorState source,
            AnimatorState destination,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 0.96f;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            return transition;
        }

        private static EnemyDefinition BuildDroneDefinition(GameObject visualPrefab, GameObject projectilePrefab)
        {
            EnemyDefinition definition = LoadOrCreateDefinition(DroneDefinitionPath);
            definition.ConfigureIdentity(EnemyArchetype.Drone, visualPrefab, null, definition.SpawnPrefab, projectilePrefab, 1f);
            definition.ConfigureMovement(30, 3.8f, 3.8f, 7f, 10f, 300f, 1.85f, 0.12f, 1.4f);
            definition.ConfigureTargeting(8f, 12f, 1f, 7f, 7f);
            definition.ConfigureRegularAttack(1.3f, 0.8f, 0.5625f, 8, 6, 0.45f, 14f);
            definition.ConfigureSpecial(0f, 0f, 0f, 0f, 0f, 0, 0, 0.8f, 0f);
            SetDeathRemovalDelay(definition, 1.5f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static EnemyDefinition BuildSaboteurDefinition(GameObject visualPrefab)
        {
            EnemyDefinition definition = LoadOrCreateDefinition(SaboteurDefinitionPath);
            definition.ConfigureIdentity(
                EnemyArchetype.Saboteur,
                visualPrefab,
                null,
                definition.SpawnPrefab,
                null,
                1.15f);
            definition.ConfigureMovement(60, 2f, 4f, 14f, 18f, 540f, 0f, 0f, 0f);
            definition.ConfigureTargeting(6f, 10f, 1f, 2.15f, 2.15f);
            definition.ConfigureRegularAttack(
                1.1f,
                0.92f,
                0.43f,
                12,
                16,
                0f,
                12f);
            definition.ConfigureSpecial(0f, 0f, 0f, 0f, 0f, 0, 0, 1f, 0f);
            SetDeathRemovalDelay(definition, 1.35f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static EnemyDefinition BuildArmoredDefinition(GameObject visualPrefab)
        {
            EnemyDefinition definition = LoadOrCreateDefinition(ArmoredDefinitionPath);
            definition.ConfigureIdentity(
                EnemyArchetype.Armored,
                visualPrefab,
                null,
                definition.SpawnPrefab,
                null,
                0.55f);
            definition.ConfigureMovement(180, 1.8f, 1.8f, 9f, 14f, 360f, 0f, 0f, 0f);
            definition.ConfigureTargeting(6f, 10f, 1f, 3.4f, 3.4f);
            definition.ConfigureRegularAttack(
                1.8f,
                1.35f,
                0.58f,
                22,
                28,
                0f,
                12f);
            definition.ConfigureSpecial(
                0.225f,
                7f,
                3f,
                7f,
                3f,
                35,
                40,
                1.75f,
                1.4f);
            SetDeathRemovalDelay(definition, 2.25f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static EnemyDefinition LoadOrCreateDefinition(string path)
        {
            EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            return definition;
        }

        private static void SetDeathRemovalDelay(EnemyDefinition definition, float seconds)
        {
            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty property = serializedDefinition.FindProperty("_deathRemovalDelay");
            if (property != null)
            {
                property.floatValue = Mathf.Max(0f, seconds);
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject BuildEnemyPrefab(
            EnemyPrefabSpec spec,
            GameObject deathExplosionPrefab,
            GameObject normalImpactEffectPrefab,
            GameObject specialImpactEffectPrefab,
            GameObject specialLaunchEffectPrefab)
        {
            GameObject root = new GameObject(spec.Name);
            try
            {
                root.layer = spec.EnemyLayer;

                Collider gameplayCollider = AddGameplayCollider(root, spec.Archetype);
                Health health = root.AddComponent<Health>();
                FactionMember factionMember = root.AddComponent<FactionMember>();
                Targetable targetable = root.AddComponent<Targetable>();
                EnemyHealth enemyHealth = root.AddComponent<EnemyHealth>();
                EnemyBrain brain = root.AddComponent<EnemyBrain>();
                EnemyAttackController attackController = root.AddComponent<EnemyAttackController>();

                GameObject targetPointObject = new GameObject("Target Point");
                targetPointObject.transform.SetParent(root.transform, false);
                targetPointObject.transform.localPosition = GetTargetPointPosition(spec.Archetype);
                TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();
                DeathExplosionEmitter deathExplosion = root.AddComponent<DeathExplosionEmitter>();
                deathExplosion.Configure(
                    deathExplosionPrefab,
                    targetPoint.transform,
                    GetDeathExplosionScale(spec.Archetype));

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(spec.VisualPrefab, root.transform);
                if (visual == null)
                {
                    throw new InvalidOperationException("Could not instantiate enemy visual: " + spec.VisualPrefab.name);
                }

                visual.name = "Visual";
                visual.transform.SetLocalPositionAndRotation(GetVisualOffset(spec.Archetype), Quaternion.identity);
                ProceduralEnemyAnimator proceduralAnimator = visual.GetComponent<ProceduralEnemyAnimator>();
                visual.transform.localScale = spec.Archetype == EnemyArchetype.Armored && proceduralAnimator == null
                    ? Vector3.one * 2f
                    : Vector3.one;
                SetLayerRecursively(visual, spec.EnemyLayer);
                DisableVisualColliders(visual);

                MonoBehaviour motorBehaviour;
                EnemyAnimatorDriver animatorDriver = null;
                Transform muzzle = null;
                GameObject telegraph = null;
                if (spec.Archetype == EnemyArchetype.Drone)
                {
                    DroneFlightMotor motor = root.AddComponent<DroneFlightMotor>();
                    motor.Configure(spec.Definition);
                    motor.ConfigureVisual(visual.transform, 1 << 0);
                    motorBehaviour = motor;
                    muzzle = CreateMuzzle(root.transform, spec.EnemyLayer);
                    telegraph = CreateDroneTelegraph(muzzle, spec.TelegraphMaterial, spec.EnemyLayer);
                    DisableAllAnimators(visual);
                }
                else
                {
                    NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                    ConfigureAgent(agent, spec.Archetype, spec.Definition);
                    NavMeshEnemyMotor motor = root.AddComponent<NavMeshEnemyMotor>();
                    motor.Configure(spec.Definition);
                    motorBehaviour = motor;

                    if (proceduralAnimator != null)
                    {
                        if (!proceduralAnimator.RigConfigured)
                        {
                            throw new InvalidOperationException(
                                spec.VisualPrefab.name + " has an incomplete procedural rig.");
                        }
                    }
                    else
                    {
                        Animator animator = visual.GetComponent<Animator>();
                        if (animator == null)
                        {
                            throw new InvalidOperationException(
                                spec.VisualPrefab.name + " has neither a procedural rig nor a root Animator.");
                        }

                        ConfigureVisualAnimators(visual, animator, spec.AnimatorController);
                        animatorDriver = visual.AddComponent<EnemyAnimatorDriver>();
                        EnemyAnimationEventRelay relay = visual.AddComponent<EnemyAnimationEventRelay>();
                        animatorDriver.Configure(animator, motorBehaviour, spec.Definition, brain);
                        relay.Configure(attackController);
                    }
                }

                health.Configure(spec.Definition.MaximumHealth);
                factionMember.Configure(Faction.Enemy);
                targetable.Configure(factionMember, targetPoint, enemyHealth, true);
                enemyHealth.Configure(spec.Definition, health, factionMember, targetable, brain, animatorDriver);
                enemyHealth.ConfigureDeathExplosion(deathExplosion);
                attackController.Configure(
                    spec.Definition,
                    animatorDriver,
                    motorBehaviour,
                    muzzle,
                    telegraph,
                    ~(1 << spec.EnemyLayer));
                EnemyAttackPresentation attackPresentation = root.AddComponent<EnemyAttackPresentation>();
                bool armored = spec.Archetype == EnemyArchetype.Armored;
                attackPresentation.Configure(
                    attackController,
                    normalImpactEffectPrefab,
                    specialImpactEffectPrefab,
                    specialLaunchEffectPrefab,
                    armored ? 1.8f : (spec.Archetype == EnemyArchetype.Drone ? 0.72f : 1f),
                    armored ? 2f : 1f,
                    armored ? 0.12f : 0.065f,
                    armored ? 0.38f : 0f,
                    armored ? 26f : 18f);
                brain.Configure(spec.Definition, enemyHealth, attackController, motorBehaviour, animatorDriver);
                if (proceduralAnimator != null)
                {
                    proceduralAnimator.ConfigureRuntime(
                        spec.Definition,
                        motorBehaviour,
                        brain,
                        attackController,
                        enemyHealth);
                }

                SetLayerRecursively(root, spec.EnemyLayer);
                gameplayCollider.enabled = true;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, spec.OutputPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Could not save enemy prefab: " + spec.OutputPath);
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Collider AddGameplayCollider(GameObject root, EnemyArchetype archetype)
        {
            if (archetype == EnemyArchetype.Drone)
            {
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.7f, 0f);
                box.size = new Vector3(2.15f, 1.32f, 3.5f);
                return box;
            }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            if (archetype == EnemyArchetype.Armored)
            {
                capsule.center = new Vector3(0f, 2.1f, 0f);
                capsule.height = 4.2f;
                capsule.radius = 1.05f;
            }
            else
            {
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.height = 1.8f;
                capsule.radius = 0.42f;
            }

            return capsule;
        }

        private static void ConfigureAgent(
            NavMeshAgent agent,
            EnemyArchetype archetype,
            EnemyDefinition definition)
        {
            agent.agentTypeID = 0;
            agent.baseOffset = 0f;
            agent.height = archetype == EnemyArchetype.Armored ? 4.2f : 1.8f;
            agent.radius = archetype == EnemyArchetype.Armored ? 1.05f : 0.42f;
            agent.speed = definition.MachineTravelSpeed;
            agent.acceleration = definition.Acceleration;
            agent.angularSpeed = definition.RotationSpeed;
            agent.stoppingDistance = Mathf.Max(0.1f, definition.MachineAttackRange * 0.8f);
            agent.autoBraking = true;
            agent.autoRepath = true;
            agent.autoTraverseOffMeshLink = false;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.updateUpAxis = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = archetype == EnemyArchetype.Armored ? 40 : 50;
            agent.areaMask = NavMesh.AllAreas;
        }

        private static Vector3 GetVisualOffset(EnemyArchetype archetype)
        {
            return archetype == EnemyArchetype.Drone ? new Vector3(0f, 0.7f, 0f) : Vector3.zero;
        }

        private static Vector3 GetTargetPointPosition(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Drone:
                    return new Vector3(0f, 0.8f, 0f);
                case EnemyArchetype.Armored:
                    return new Vector3(0f, 3.1f, 0f);
                default:
                    return new Vector3(0f, 1.35f, 0f);
            }
        }

        private static Transform CreateMuzzle(Transform root, int layer)
        {
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.layer = layer;
            muzzle.transform.SetParent(root, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.65f, 1.9f);
            return muzzle.transform;
        }

        private static GameObject CreateDroneTelegraph(Transform muzzle, Material material, int layer)
        {
            GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            telegraph.name = "Electrical Shot Telegraph";
            telegraph.layer = layer;
            telegraph.transform.SetParent(muzzle, false);
            telegraph.transform.localPosition = Vector3.zero;
            telegraph.transform.localScale = Vector3.one * 0.34f;
            telegraph.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(telegraph.GetComponent<Collider>());
            return telegraph;
        }

        private static void ConfigureVisualAnimators(
            GameObject visual,
            Animator rootAnimator,
            RuntimeAnimatorController controller)
        {
            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int index = 0; index < animators.Length; index++)
            {
                Animator animator = animators[index];
                animator.applyRootMotion = false;
                if (animator == rootAnimator)
                {
                    animator.enabled = true;
                    animator.runtimeAnimatorController = controller;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                else
                {
                    animator.enabled = false;
                    animator.runtimeAnimatorController = null;
                }
            }
        }

        private static void DisableAllAnimators(GameObject visual)
        {
            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int index = 0; index < animators.Length; index++)
            {
                animators[index].applyRootMotion = false;
                animators[index].runtimeAnimatorController = null;
                animators[index].enabled = false;
            }
        }

        private static void DisableVisualColliders(GameObject visual)
        {
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }
        }

        private static GameObject BuildDroneProjectilePrefab(
            int enemyLayer,
            Material material,
            GameObject impactEffectPrefab)
        {
            GameObject root = new GameObject("PF_Enemy_DroneBolt");
            try
            {
                root.layer = enemyLayer;
                EnemyProjectile projectile = root.AddComponent<EnemyProjectile>();
                projectile.ConfigureImpactEffect(impactEffectPrefab, 0.85f);

                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bolt.name = "Electrical Bolt Visual";
                bolt.layer = enemyLayer;
                bolt.transform.SetParent(root.transform, false);
                bolt.transform.localScale = new Vector3(0.2f, 0.2f, 0.55f);
                bolt.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(bolt.GetComponent<Collider>());

                TrailRenderer trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.22f;
                trail.minVertexDistance = 0.03f;
                trail.widthMultiplier = 0.18f;
                trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                trail.sharedMaterial = material;
                trail.alignment = LineAlignment.View;
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;

                Light light = new GameObject("Electrical Glow").AddComponent<Light>();
                light.gameObject.layer = enemyLayer;
                light.transform.SetParent(root.transform, false);
                light.type = LightType.Point;
                light.color = new Color(0.1f, 0.72f, 1f);
                light.range = 2.2f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DroneProjectilePrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Could not save drone projectile prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameplayEffectAssets BuildGameplayEffectAssets()
        {
            Texture2D softDisc = CreateOrUpdateSoftDiscTexture();
            Material glowMaterial = CreateOrUpdateParticleMaterial(
                GameplayGlowMaterialPath,
                softDisc,
                Color.white,
                true);
            Material smokeMaterial = CreateOrUpdateParticleMaterial(
                GameplaySmokeMaterialPath,
                softDisc,
                Color.white,
                false);

            return new GameplayEffectAssets
            {
                MeleeImpact = BuildLayeredGameplayEffect(
                    MeleeImpactEffectPath,
                    "PF_VFX_MeleeImpact",
                    glowMaterial,
                    smokeMaterial,
                    new Color(1f, 0.34f, 0.045f, 1f),
                    5,
                    18,
                    5,
                    0.18f,
                    0.85f,
                    false,
                    2.4f),
                ArmoredSlam = BuildLayeredGameplayEffect(
                    ArmoredSlamEffectPath,
                    "PF_VFX_ArmoredSlam",
                    glowMaterial,
                    smokeMaterial,
                    new Color(1f, 0.43f, 0.055f, 1f),
                    12,
                    36,
                    22,
                    0.52f,
                    1.55f,
                    true,
                    6.5f),
                DroneImpact = BuildLayeredGameplayEffect(
                    DroneImpactEffectPath,
                    "PF_VFX_DroneImpact",
                    glowMaterial,
                    smokeMaterial,
                    new Color(0.08f, 0.78f, 1f, 1f),
                    7,
                    22,
                    3,
                    0.16f,
                    0.75f,
                    false,
                    3.8f),
                PlayerJump = BuildLayeredGameplayEffect(
                    PlayerJumpEffectPath,
                    "PF_VFX_PlayerJump",
                    glowMaterial,
                    smokeMaterial,
                    new Color(1f, 0.54f, 0.12f, 1f),
                    3,
                    8,
                    10,
                    0.24f,
                    0.8f,
                    true,
                    0f),
                DoubleJump = BuildLayeredGameplayEffect(
                    DoubleJumpEffectPath,
                    "PF_VFX_DoubleJump",
                    glowMaterial,
                    smokeMaterial,
                    new Color(0.08f, 0.82f, 1f, 1f),
                    7,
                    20,
                    4,
                    0.22f,
                    1f,
                    true,
                    3.2f),
                PlayerDash = BuildDashGameplayEffect(glowMaterial, smokeMaterial),
                PlayerHit = BuildLayeredGameplayEffect(
                    PlayerHitEffectPath,
                    "PF_VFX_PlayerHit",
                    glowMaterial,
                    smokeMaterial,
                    new Color(1f, 0.1f, 0.025f, 1f),
                    5,
                    15,
                    3,
                    0.14f,
                    0.7f,
                    false,
                    2.8f),
                MachineBreak = BuildLayeredGameplayEffect(
                    MachineBreakEffectPath,
                    "PF_VFX_MachineBreak",
                    glowMaterial,
                    smokeMaterial,
                    new Color(1f, 0.3f, 0.035f, 1f),
                    14,
                    42,
                    18,
                    0.58f,
                    1.8f,
                    false,
                    7.5f),
                RepairLoop = BuildRepairLoopEffect(glowMaterial, smokeMaterial)
            };
        }

        private static GameObject BuildDashGameplayEffect(Material glowMaterial, Material smokeMaterial)
        {
            GameObject root = new GameObject("PF_VFX_PlayerDash");
            try
            {
                Color dashColor = new Color(0.06f, 0.86f, 1f, 1f);
                ParticleSystem core = CreateGameplayParticleSystem(
                    "Dash Core",
                    root.transform,
                    glowMaterial,
                    dashColor,
                    7,
                    new ParticleSystem.MinMaxCurve(0.1f, 0.24f),
                    new ParticleSystem.MinMaxCurve(0.35f, 1.6f),
                    new ParticleSystem.MinMaxCurve(0.22f, 0.5f),
                    0.3f,
                    0f,
                    false,
                    false);
                ParticleSystem streaks = CreateGameplayParticleSystem(
                    "Reverse Energy Streaks",
                    root.transform,
                    glowMaterial,
                    dashColor,
                    24,
                    new ParticleSystem.MinMaxCurve(0.16f, 0.38f),
                    new ParticleSystem.MinMaxCurve(4.5f, 8.5f),
                    new ParticleSystem.MinMaxCurve(0.025f, 0.065f),
                    0.32f,
                    0f,
                    false,
                    true);
                ConfigureDashCone(streaks, 11f, 0.32f, 0.42f);

                ParticleSystem wake = CreateGameplayParticleSystem(
                    "Ion Wake",
                    root.transform,
                    smokeMaterial,
                    new Color(0.08f, 0.68f, 0.82f, 0.34f),
                    10,
                    new ParticleSystem.MinMaxCurve(0.28f, 0.58f),
                    new ParticleSystem.MinMaxCurve(1.2f, 3.2f),
                    new ParticleSystem.MinMaxCurve(0.16f, 0.42f),
                    0.38f,
                    -0.03f,
                    false,
                    false);
                ConfigureDashCone(wake, 19f, 0.38f, 0.5f);

                Light burstLight = CreateGameplayEffectLight(root.transform, dashColor, 3.1f, 3.8f);
                GameplayEffect effect = root.AddComponent<GameplayEffect>();
                effect.Configure(new[] { core, streaks, wake }, burstLight, false, 0.7f, 0.11f);
                return PrefabUtility.SaveAsPrefabAsset(root, PlayerDashEffectPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureDashCone(
            ParticleSystem particleSystem,
            float angle,
            float radius,
            float length)
        {
            particleSystem.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.length = length;
            shape.radiusThickness = 1f;
        }

        private static GameObject BuildLayeredGameplayEffect(
            string path,
            string name,
            Material glowMaterial,
            Material smokeMaterial,
            Color color,
            short coreCount,
            short sparkCount,
            short dustCount,
            float radius,
            float lifetime,
            bool addShockwave,
            float lightIntensity)
        {
            GameObject root = new GameObject(name);
            try
            {
                List<ParticleSystem> layers = new List<ParticleSystem>();
                layers.Add(CreateGameplayParticleSystem(
                    "Impact Core",
                    root.transform,
                    glowMaterial,
                    color,
                    coreCount,
                    new ParticleSystem.MinMaxCurve(0.12f, 0.28f),
                    new ParticleSystem.MinMaxCurve(0.6f, 2.2f),
                    new ParticleSystem.MinMaxCurve(0.28f, 0.62f),
                    radius,
                    0f,
                    false,
                    false));
                layers.Add(CreateGameplayParticleSystem(
                    "Directional Sparks",
                    root.transform,
                    glowMaterial,
                    color,
                    sparkCount,
                    new ParticleSystem.MinMaxCurve(0.22f, 0.68f),
                    new ParticleSystem.MinMaxCurve(3.5f, addShockwave ? 10f : 7f),
                    new ParticleSystem.MinMaxCurve(0.035f, 0.085f),
                    radius,
                    addShockwave ? 1.25f : 0.8f,
                    false,
                    true));
                if (dustCount > 0)
                {
                    layers.Add(CreateGameplayParticleSystem(
                        "Dust Volume",
                        root.transform,
                        smokeMaterial,
                        new Color(0.3f, 0.33f, 0.34f, 0.58f),
                        dustCount,
                        new ParticleSystem.MinMaxCurve(0.55f, lifetime),
                        new ParticleSystem.MinMaxCurve(0.25f, 1.8f),
                        new ParticleSystem.MinMaxCurve(0.3f, addShockwave ? 1.1f : 0.72f),
                        radius * 1.2f,
                        -0.08f,
                        false,
                        false));
                }

                if (addShockwave)
                {
                    layers.Add(CreateGameplayParticleSystem(
                        "Expanding Shock Ring",
                        root.transform,
                        glowMaterial,
                        color,
                        30,
                        new ParticleSystem.MinMaxCurve(0.28f, 0.46f),
                        new ParticleSystem.MinMaxCurve(5.5f, 8f),
                        new ParticleSystem.MinMaxCurve(0.06f, 0.13f),
                        Mathf.Max(0.18f, radius),
                        0f,
                        true,
                        true));
                }

                Light burstLight = lightIntensity > 0f
                    ? CreateGameplayEffectLight(root.transform, color, lightIntensity, addShockwave ? 7f : 4f)
                    : null;
                GameplayEffect effect = root.AddComponent<GameplayEffect>();
                effect.Configure(layers.ToArray(), burstLight, false, lifetime, 0.16f);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildRepairLoopEffect(Material glowMaterial, Material smokeMaterial)
        {
            GameObject root = new GameObject("PF_VFX_RepairLoop");
            try
            {
                ParticleSystem sparks = CreateGameplayParticleSystem(
                    "Welding Sparks",
                    root.transform,
                    glowMaterial,
                    new Color(1f, 0.48f, 0.07f, 1f),
                    0,
                    new ParticleSystem.MinMaxCurve(0.18f, 0.48f),
                    new ParticleSystem.MinMaxCurve(2.5f, 5.8f),
                    new ParticleSystem.MinMaxCurve(0.025f, 0.065f),
                    0.22f,
                    0.65f,
                    false,
                    true,
                    true,
                    15f);
                ParticleSystem smoke = CreateGameplayParticleSystem(
                    "Repair Haze",
                    root.transform,
                    smokeMaterial,
                    new Color(0.26f, 0.3f, 0.31f, 0.36f),
                    0,
                    new ParticleSystem.MinMaxCurve(0.55f, 1.1f),
                    new ParticleSystem.MinMaxCurve(0.1f, 0.38f),
                    new ParticleSystem.MinMaxCurve(0.15f, 0.38f),
                    0.18f,
                    -0.04f,
                    false,
                    false,
                    true,
                    2.5f);
                Light workLight = CreateGameplayEffectLight(
                    root.transform,
                    new Color(1f, 0.38f, 0.04f),
                    1.6f,
                    2.4f);
                GameplayEffect effect = root.AddComponent<GameplayEffect>();
                effect.Configure(new[] { sparks, smoke }, workLight, true, 1f, 0.12f, 0.55f);
                return PrefabUtility.SaveAsPrefabAsset(root, RepairLoopEffectPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ParticleSystem CreateGameplayParticleSystem(
            string name,
            Transform parent,
            Material material,
            Color color,
            short burstCount,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float radius,
            float gravity,
            bool circleShape,
            bool stretch,
            bool looping = false,
            float rateOverTime = 0f)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = looping;
            main.playOnAwake = false;
            main.duration = looping ? 1f : 0.1f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(12, burstCount + Mathf.CeilToInt(rateOverTime * 3f));

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = looping ? rateOverTime : 0f;
            if (!looping && burstCount > 0)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });
            }

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = circleShape ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.radiusThickness = circleShape ? 0f : 1f;
            if (circleShape)
            {
                shape.rotation = new Vector3(90f, 0f, 0f);
            }

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = fade;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.65f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(1f, 0.15f)));

            ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = stretch
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            if (stretch)
            {
                renderer.lengthScale = 2.8f;
                renderer.velocityScale = 0.22f;
            }

            return particleSystem;
        }

        private static Light CreateGameplayEffectLight(
            Transform parent,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject("Burst Light");
            lightObject.transform.SetParent(parent, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static GameObject BuildDeathExplosionPrefab()
        {
            Texture2D softDisc = CreateOrUpdateSoftDiscTexture();
            Material glowMaterial = CreateOrUpdateParticleMaterial(
                DeathGlowMaterialPath,
                softDisc,
                new Color(1f, 0.36f, 0.04f, 1f),
                true);
            Material smokeMaterial = CreateOrUpdateParticleMaterial(
                DeathSmokeMaterialPath,
                softDisc,
                new Color(0.16f, 0.18f, 0.2f, 0.72f),
                false);

            GameObject root = new GameObject("PF_MechanicalDeathExplosion");
            try
            {
                ParticleSystem core = CreateDeathParticleSystem(
                    "Core Fire Burst",
                    root.transform,
                    glowMaterial,
                    18,
                    new ParticleSystem.MinMaxCurve(0.24f, 0.48f),
                    new ParticleSystem.MinMaxCurve(1.8f, 4.2f),
                    new ParticleSystem.MinMaxCurve(0.28f, 0.72f),
                    new Color(1f, 0.72f, 0.14f, 1f),
                    0.18f);

                ParticleSystem sparks = CreateDeathParticleSystem(
                    "Hot Metal Sparks",
                    root.transform,
                    glowMaterial,
                    30,
                    new ParticleSystem.MinMaxCurve(0.32f, 0.78f),
                    new ParticleSystem.MinMaxCurve(4f, 8f),
                    new ParticleSystem.MinMaxCurve(0.045f, 0.1f),
                    new Color(1f, 0.31f, 0.035f, 1f),
                    0.12f);
                ParticleSystem.MainModule sparkMain = sparks.main;
                sparkMain.gravityModifier = 1.1f;
                ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
                sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                sparkRenderer.lengthScale = 3.2f;
                sparkRenderer.velocityScale = 0.25f;

                ParticleSystem smoke = CreateDeathParticleSystem(
                    "Mechanical Smoke",
                    root.transform,
                    smokeMaterial,
                    11,
                    new ParticleSystem.MinMaxCurve(0.75f, 1.55f),
                    new ParticleSystem.MinMaxCurve(0.35f, 1.25f),
                    new ParticleSystem.MinMaxCurve(0.45f, 1.05f),
                    new Color(0.22f, 0.24f, 0.25f, 0.62f),
                    0.32f);
                ParticleSystem.VelocityOverLifetimeModule smokeVelocity = smoke.velocityOverLifetime;
                smokeVelocity.enabled = true;
                smokeVelocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                smokeVelocity.y = new ParticleSystem.MinMaxCurve(0.65f, 1.25f);
                smokeVelocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                GameObject lightObject = new GameObject("Explosion Light");
                lightObject.transform.SetParent(root.transform, false);
                Light burstLight = lightObject.AddComponent<Light>();
                burstLight.type = LightType.Point;
                burstLight.color = new Color(1f, 0.34f, 0.035f);
                burstLight.intensity = 7f;
                burstLight.range = 5.5f;
                burstLight.shadows = LightShadows.None;
                burstLight.enabled = false;

                DeathExplosionEffect effect = root.AddComponent<DeathExplosionEffect>();
                effect.Configure(new[] { core, sparks, smoke }, burstLight, 0.2f, 1.8f);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DeathExplosionPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Could not save the shared mechanical death explosion prefab.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ParticleSystem CreateDeathParticleSystem(
            string name,
            Transform parent,
            Material material,
            short burstCount,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            Color color,
            float shapeRadius)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = burstCount + 4;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.38f, 0.08f), 0.55f),
                    new GradientColorKey(new Color(0.18f, 0.2f, 0.22f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.72f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = fade;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            return particleSystem;
        }

        private static Texture2D CreateOrUpdateSoftDiscTexture()
        {
            const int size = 64;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DeathParticleTexturePath);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                {
                    name = "T_DeathExplosion_SoftDisc",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                AssetDatabase.CreateAsset(texture, DeathParticleTexturePath);
            }
            else if (texture.width != size || texture.height != size)
            {
                texture.Reinitialize(size, size, TextureFormat.RGBA32, false);
            }

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - normalized);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Material CreateOrUpdateParticleMaterial(
            string path,
            Texture2D texture,
            Color color,
            bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible shader was found for death particles.");
            }

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

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            SetMaterialFloat(material, "_Surface", 1f);
            SetMaterialFloat(material, "_ZWrite", 0f);
            SetMaterialFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetMaterialFloat(
                material,
                "_DstBlend",
                additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetMaterialColor(material, "_BaseColor", color);
            SetMaterialColor(material, "_Color", color);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetMaterialColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static float GetDeathExplosionScale(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Drone:
                    return 0.85f;
                case EnemyArchetype.Armored:
                    return 2.2f;
                default:
                    return 1f;
            }
        }

        private static Material CreateOrUpdateEmissionMaterial(string path, Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible shader was found for enemy effects.");
            }

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

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip RequireAnimationClip(string path)
        {
            AnimationClip clip = LoadAnimationClip(path);
            if (clip == null)
            {
                throw new InvalidOperationException("No animation clip could be loaded from: " + path);
            }

            return clip;
        }

        private static AnimationClip LoadAnimationClip(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                AnimationClip clip = assets[index] as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return null;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required asset is missing: " + path);
            }

            return asset;
        }

        private static int EnsureLayer(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                throw new InvalidOperationException("Could not load ProjectSettings/TagManager.asset.");
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int index = 8; index < layers.arraySize; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return index;
            }

            throw new InvalidOperationException("No free user layer is available for the Enemy layer.");
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

        private sealed class EnemyPrefabSpec
        {
            public EnemyPrefabSpec(
                string name,
                string outputPath,
                EnemyDefinition definition,
                GameObject visualPrefab,
                RuntimeAnimatorController animatorController,
                EnemyArchetype archetype,
                int enemyLayer,
                Material telegraphMaterial)
            {
                Name = name;
                OutputPath = outputPath;
                Definition = definition;
                VisualPrefab = visualPrefab;
                AnimatorController = animatorController;
                Archetype = archetype;
                EnemyLayer = enemyLayer;
                TelegraphMaterial = telegraphMaterial;
            }

            public string Name { get; }
            public string OutputPath { get; }
            public EnemyDefinition Definition { get; }
            public GameObject VisualPrefab { get; }
            public RuntimeAnimatorController AnimatorController { get; }
            public EnemyArchetype Archetype { get; }
            public int EnemyLayer { get; }
            public Material TelegraphMaterial { get; }
        }

        private sealed class GameplayEffectAssets
        {
            public GameObject MeleeImpact { get; set; }
            public GameObject ArmoredSlam { get; set; }
            public GameObject DroneImpact { get; set; }
            public GameObject PlayerJump { get; set; }
            public GameObject DoubleJump { get; set; }
            public GameObject PlayerDash { get; set; }
            public GameObject PlayerHit { get; set; }
            public GameObject MachineBreak { get; set; }
            public GameObject RepairLoop { get; set; }
        }
    }
}
