using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class EnemyAssetFactoryTests
    {
        private const string DroneVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Veh_Drone_Attach_01.prefab";
        private const string SaboteurVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Characters/SM_Chr_RobotFemale_01.prefab";
        private const string ArmoredVisualPath = "Assets/Synty/PolygonSciFiSpace/Prefabs/Characters/SM_Chr_BR_BigAlien_02.prefab";
        private const string IdleAnimationPath = "Assets/Animations/Paladin J Nordstrom@Idle.fbx";
        private const string WalkingAnimationPath = "Assets/Animations/Paladin J Nordstrom@Walking.fbx";
        private const string RunningAnimationPath = "Assets/Animations/Paladin J Nordstrom@Standard Run.fbx";
        private const string ZombieAttackAnimationPath = "Assets/Animations/Paladin J Nordstrom@Zombie Attack.fbx";
        private const string ZombieDeathAnimationPath = "Assets/Animations/Paladin J Nordstrom@Zombie Death.fbx";
        private const string MutantSwipeAnimationPath = "Assets/Animations/Paladin J Nordstrom@Mutant Swiping.fbx";
        private const string MutantJumpAttackAnimationPath = "Assets/Animations/Paladin J Nordstrom@Mutant Jump Attack.fbx";
        private const string DyingAnimationPath = "Assets/Animations/Paladin J Nordstrom@Dying.fbx";

        [OneTimeSetUp]
        public void RebuildGeneratedAssetsBeforeValidation()
        {
            // Always rebuild so tests also cover recovery from a partially written or
            // stale generated asset set, not just the completely-missing case.
            EnemyAssetFactory.BuildAll();
        }

        [TestCase(EnemyAssetFactory.DronePrefabPath, DroneVisualPath, EnemyArchetype.Drone)]
        [TestCase(EnemyAssetFactory.SaboteurPrefabPath, SaboteurVisualPath, EnemyArchetype.Saboteur)]
        [TestCase(EnemyAssetFactory.ArmoredPrefabPath, ArmoredVisualPath, EnemyArchetype.Armored)]
        public void EnemyPrefab_IsCompleteAndKeepsItsNestedSyntySource(
            string prefabPath,
            string expectedVisualPath,
            EnemyArchetype archetype)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(prefab.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")));
            Assert.That(prefab.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Health>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FactionMember>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<FactionMember>().Faction, Is.EqualTo(Faction.Enemy));
            Assert.That(prefab.GetComponent<Targetable>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<EnemyHealth>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<EnemyBrain>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<EnemyAttackController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<EnemyAttackPresentation>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Target Point")?.GetComponent<TargetPoint>(), Is.Not.Null);

            GameObject visual = prefab.transform.Find("Visual")?.gameObject;
            Assert.That(visual, Is.Not.Null);
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(visual);
            Assert.That(source, Is.Not.Null, "The visual must remain a nested source-prefab instance.");
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo(expectedVisualPath));
            Assert.That(
                visual.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled),
                Is.True,
                "Visual-source colliders must not compete with the gameplay-root collider.");

            foreach (Transform node in prefab.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(node.gameObject.layer, Is.EqualTo(prefab.layer), node.name + " has a mismatched layer.");
                Assert.That(
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),
                    Is.Zero,
                    node.name + " has a missing script.");
            }

            if (archetype == EnemyArchetype.Drone)
            {
                Assert.That(prefab.GetComponent<DroneFlightMotor>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<NavMeshAgent>(), Is.Null);
                Assert.That(visual.GetComponentsInChildren<Animator>(true), Is.Empty);
                Assert.That(prefab.transform.Find("Muzzle/Electrical Shot Telegraph"), Is.Not.Null);
            }
            else
            {
                Assert.That(prefab.GetComponent<NavMeshEnemyMotor>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<NavMeshAgent>(), Is.Not.Null);
                Animator animator = visual.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(visual.GetComponent<EnemyAnimatorDriver>(), Is.Not.Null);
                Assert.That(visual.GetComponent<EnemyAnimationEventRelay>(), Is.Not.Null);
                AssertRequiredReference(prefab.GetComponent<EnemyHealth>(), "_animatorDriver");
            }

            if (archetype == EnemyArchetype.Armored)
            {
                Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one * 2f));
                CapsuleCollider capsule = prefab.GetComponent<CapsuleCollider>();
                Assert.That(capsule.height, Is.EqualTo(4.2f));
                Assert.That(capsule.radius, Is.EqualTo(1.05f));
                Assert.That(prefab.GetComponent<NavMeshAgent>().height, Is.EqualTo(4.2f));
                Assert.That(prefab.GetComponent<NavMeshAgent>().radius, Is.EqualTo(1.05f));
                Assert.That(prefab.transform.Find("Target Point").localPosition.y, Is.EqualTo(3.1f));
            }

            AssertRequiredReference(prefab.GetComponent<EnemyHealth>(), "_definition");
            AssertRequiredReference(prefab.GetComponent<EnemyHealth>(), "_health");
            AssertRequiredReference(prefab.GetComponent<EnemyHealth>(), "_targetable");
            AssertRequiredReference(prefab.GetComponent<EnemyHealth>(), "_brain");
            AssertRequiredReference(prefab.GetComponent<EnemyBrain>(), "_definition");
            AssertRequiredReference(prefab.GetComponent<EnemyBrain>(), "_health");
            AssertRequiredReference(prefab.GetComponent<EnemyBrain>(), "_attackController");
            AssertRequiredReference(prefab.GetComponent<EnemyBrain>(), "_motorBehaviour");
            AssertRequiredReference(prefab.GetComponent<EnemyAttackController>(), "_definition");
            AssertRequiredReference(prefab.GetComponent<EnemyAttackController>(), "_motorBehaviour");
            AssertRequiredReference(prefab.GetComponent<Targetable>(), "_targetPoint");
            AssertRequiredReference(prefab.GetComponent<Targetable>(), "_damageableBehaviour");
        }

        [Test]
        public void Definitions_UseRequiredTuningAndActualAttackClipLengths()
        {
            EnemyDefinition drone = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyAssetFactory.DroneDefinitionPath);
            EnemyDefinition saboteur = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyAssetFactory.SaboteurDefinitionPath);
            EnemyDefinition armored = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyAssetFactory.ArmoredDefinitionPath);
            AnimationClip zombie = LoadAnimationClip(ZombieAttackAnimationPath);
            AnimationClip zombieDeath = LoadAnimationClip(ZombieDeathAnimationPath);
            AnimationClip swipe = LoadAnimationClip(MutantSwipeAnimationPath);
            AnimationClip jump = LoadAnimationClip(MutantJumpAttackAnimationPath);
            AnimationClip dying = LoadAnimationClip(DyingAnimationPath);

            Assert.That(drone, Is.Not.Null);
            Assert.That(drone.MaximumHealth, Is.EqualTo(30));
            Assert.That(drone.MachineTravelSpeed, Is.EqualTo(3.8f));
            Assert.That(drone.PlayerDamage, Is.EqualTo(8));
            Assert.That(drone.MachineDamage, Is.EqualTo(6));
            Assert.That(drone.AttackCooldown, Is.EqualTo(1.3f));
            Assert.That(drone.TelegraphDuration, Is.EqualTo(0.45f));
            Assert.That(drone.ProjectilePrefab, Is.Not.Null);
            Assert.That(drone.SpawnPrefab, Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyAssetFactory.DronePrefabPath)));

            Assert.That(saboteur.MaximumHealth, Is.EqualTo(60));
            Assert.That(saboteur.MachineTravelSpeed, Is.EqualTo(2f));
            Assert.That(saboteur.PlayerChaseSpeed, Is.EqualTo(4f));
            Assert.That(saboteur.PlayerDamage, Is.EqualTo(12));
            Assert.That(saboteur.MachineDamage, Is.EqualTo(16));
            Assert.That(saboteur.AttackDuration, Is.EqualTo(zombie.length).Within(0.001f));
            Assert.That(saboteur.DeathRemovalDelay, Is.EqualTo(zombieDeath.length + 0.4f).Within(0.001f));

            Assert.That(armored.MaximumHealth, Is.EqualTo(180));
            Assert.That(armored.MachineTravelSpeed, Is.EqualTo(1.8f));
            Assert.That(armored.PlayerChaseSpeed, Is.EqualTo(1.8f));
            Assert.That(armored.PlayerDamage, Is.EqualTo(22));
            Assert.That(armored.MachineDamage, Is.EqualTo(28));
            Assert.That(armored.AttackDuration, Is.EqualTo(swipe.length).Within(0.001f));
            Assert.That(armored.SpecialDuration, Is.EqualTo(jump.length).Within(0.001f));
            Assert.That(armored.SpecialChance, Is.EqualTo(0.225f).Within(0.0001f));
            Assert.That(armored.SpecialCooldown, Is.EqualTo(7f));
            Assert.That(armored.MinimumLeapDistance, Is.EqualTo(3f));
            Assert.That(armored.MaximumLeapDistance, Is.EqualTo(7f));
            Assert.That(armored.MachineAttackRange, Is.EqualTo(3.4f));
            Assert.That(armored.PlayerAttackRange, Is.EqualTo(3.4f));
            Assert.That(armored.SpecialImpactRadius, Is.EqualTo(3f));
            Assert.That(armored.SpecialPlayerDamage, Is.EqualTo(35));
            Assert.That(armored.SpecialMachineDamage, Is.EqualTo(40));
            Assert.That(armored.DeathRemovalDelay, Is.EqualTo(dying.length + 0.4f).Within(0.001f));
        }

        [Test]
        public void AnimationImports_AreHumanoidInPlaceAndHaveReproducibleImpactEvents()
        {
            AssertAnimationImport(IdleAnimationPath, true, null, 0f);
            AssertAnimationImport(WalkingAnimationPath, true, null, 0f);
            AssertAnimationImport(RunningAnimationPath, true, null, 0f);
            AssertAnimationImport(
                ZombieAttackAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnAttackImpact),
                0.46f);
            AssertAnimationImport(ZombieDeathAnimationPath, false, null, 0f);
            AssertAnimationImport(
                MutantSwipeAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnAttackImpact),
                0.43f);
            AssertAnimationImport(
                MutantJumpAttackAnimationPath,
                false,
                nameof(EnemyAnimationEventRelay.OnSpecialImpact),
                0.60f);
            AssertAnimationImport(DyingAnimationPath, false, null, 0f);
        }

        [Test]
        public void AnimatorControllers_ContainRequiredStatesAndParameters()
        {
            AnimatorController saboteur = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyAssetFactory.SaboteurControllerPath);
            AnimatorController armored = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyAssetFactory.ArmoredControllerPath);

            AssertController(
                saboteur,
                new[] { "Idle", "Walk", "Run", "Zombie Attack", "Zombie Death" },
                includeSpecial: false);
            AssertController(
                armored,
                new[] { "Idle", "Walk", "Mutant Swipe", "Mutant Jump Attack", "Dying" },
                includeSpecial: true);

            Assert.That(FindState(saboteur, "Zombie Death").transitions, Is.Empty);
            Assert.That(FindState(armored, "Dying").transitions, Is.Empty);
        }

        [Test]
        public void DroneProjectile_IsVisibleAndConfigured()
        {
            GameObject projectile = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyAssetFactory.DroneProjectilePrefabPath);
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.GetComponent<EnemyProjectile>(), Is.Not.Null);
            Assert.That(projectile.GetComponent<TrailRenderer>(), Is.Not.Null);
            Renderer renderer = projectile.GetComponentInChildren<Renderer>(true);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            AssertRequiredReference(projectile.GetComponent<EnemyProjectile>(), "_impactEffectPrefab");
        }

        [Test]
        public void GameplayEffects_AreLayeredAndAssignedToEnemyPresentation()
        {
            string[] effectPaths =
            {
                EnemyAssetFactory.MeleeImpactEffectPath,
                EnemyAssetFactory.ArmoredSlamEffectPath,
                EnemyAssetFactory.DroneImpactEffectPath,
                EnemyAssetFactory.PlayerJumpEffectPath,
                EnemyAssetFactory.DoubleJumpEffectPath,
                EnemyAssetFactory.PlayerDashEffectPath,
                EnemyAssetFactory.PlayerHitEffectPath,
                EnemyAssetFactory.MachineBreakEffectPath,
                EnemyAssetFactory.RepairLoopEffectPath
            };

            foreach (string path in effectPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                GameplayEffect effect = prefab.GetComponent<GameplayEffect>();
                Assert.That(effect, Is.Not.Null, path);
                Assert.That(effect.ParticleLayerCount, Is.GreaterThanOrEqualTo(2), path);
            }

            GameObject armored = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyAssetFactory.ArmoredPrefabPath);
            EnemyAttackPresentation presentation = armored.GetComponent<EnemyAttackPresentation>();
            Assert.That(presentation.NormalImpactEffectPrefab, Is.Not.Null);
            Assert.That(presentation.SpecialImpactEffectPrefab, Is.Not.Null);
        }

        [Test]
        public void FactoryRerun_PreservesDefinitionControllerAndPrefabGuids()
        {
            string[] paths =
            {
                EnemyAssetFactory.DroneDefinitionPath,
                EnemyAssetFactory.SaboteurDefinitionPath,
                EnemyAssetFactory.ArmoredDefinitionPath,
                EnemyAssetFactory.SaboteurControllerPath,
                EnemyAssetFactory.ArmoredControllerPath,
                EnemyAssetFactory.DroneProjectilePrefabPath,
                EnemyAssetFactory.DronePrefabPath,
                EnemyAssetFactory.SaboteurPrefabPath,
                EnemyAssetFactory.ArmoredPrefabPath
            };
            Dictionary<string, string> before = paths.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);

            EnemyAssetFactory.BuildAll();

            foreach (string path in paths)
            {
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.Not.Empty, path);
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(before[path]), path);
            }
        }

        private static void AssertRequiredReference(Component component, string propertyName)
        {
            Assert.That(component, Is.Not.Null);
            SerializedProperty property = new SerializedObject(component).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, component.GetType().Name + "." + propertyName);
            Assert.That(
                property.objectReferenceValue,
                Is.Not.Null,
                component.GetType().Name + "." + propertyName + " is not assigned.");
        }

        private static void AssertAnimationImport(
            string path,
            bool loop,
            string impactFunction,
            float expectedImpactNormalizedTime)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.importAnimation, Is.True);

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            Assert.That(clips, Is.Not.Empty);
            foreach (ModelImporterClipAnimation clip in clips)
            {
                Assert.That(clip.loopTime, Is.EqualTo(loop));
                Assert.That(clip.loopPose, Is.EqualTo(loop));
                Assert.That(clip.lockRootRotation, Is.True);
                Assert.That(clip.lockRootHeightY, Is.True);
                Assert.That(clip.lockRootPositionXZ, Is.True);
                int managedEventCount = clip.events.Count(animationEvent =>
                    animationEvent.functionName == nameof(EnemyAnimationEventRelay.OnAttackImpact) ||
                    animationEvent.functionName == nameof(EnemyAnimationEventRelay.OnSpecialImpact));
                Assert.That(managedEventCount, Is.EqualTo(impactFunction == null ? 0 : 1));
                if (impactFunction != null)
                {
                    AnimationEvent impactEvent = clip.events.Single(animationEvent =>
                        animationEvent.functionName == impactFunction);
                    AnimationClip importedClip = LoadAnimationClip(path);
                    Assert.That(impactEvent.time, Is.GreaterThan(0f));
                    Assert.That(
                        impactEvent.time,
                        Is.EqualTo(expectedImpactNormalizedTime).Within(0.001f),
                        "ModelImporterClipAnimation event time must be normalized.");
                    AnimationEvent runtimeEvent = AnimationUtility.GetAnimationEvents(importedClip).Single(
                        animationEvent => animationEvent.functionName == impactFunction);
                    Assert.That(
                        runtimeEvent.time / importedClip.length,
                        Is.EqualTo(expectedImpactNormalizedTime).Within(0.01f));
                }
            }
        }

        private static void AssertController(
            AnimatorController controller,
            IReadOnlyCollection<string> expectedStates,
            bool includeSpecial)
        {
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.layers, Has.Length.EqualTo(1));
            string[] states = controller.layers[0].stateMachine.states
                .Select(child => child.state.name)
                .OrderBy(name => name)
                .ToArray();
            CollectionAssert.AreEquivalent(expectedStates, states);

            Dictionary<string, AnimatorControllerParameterType> parameters = controller.parameters
                .ToDictionary(parameter => parameter.name, parameter => parameter.type);
            Assert.That(parameters[EnemyAnimatorDriver.MoveSpeedParameter], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters[EnemyAnimatorDriver.LocomotionRateParameter], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters[EnemyAnimatorDriver.ChasingPlayerParameter], Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters[EnemyAnimatorDriver.AttackParameter], Is.EqualTo(AnimatorControllerParameterType.Trigger));
            Assert.That(parameters[EnemyAnimatorDriver.DeathParameter], Is.EqualTo(AnimatorControllerParameterType.Trigger));
            Assert.That(parameters.ContainsKey(EnemyAnimatorDriver.SpecialAttackParameter), Is.EqualTo(includeSpecial));
        }

        private static AnimatorState FindState(AnimatorController controller, string stateName)
        {
            return controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .Single(state => state.name == stateName);
        }

        private static AnimationClip LoadAnimationClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .First(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
        }
    }
}
