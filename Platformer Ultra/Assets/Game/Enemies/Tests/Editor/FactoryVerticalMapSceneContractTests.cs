using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies.Editor;
using PlatformerUltra.Factory.Conveyors;
using PlatformerUltra.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Enemies.Tests
{
    public sealed class FactoryVerticalMapSceneContractTests
    {
        private const string ScenePath = "Assets/Game/Scenes/FactoryVerticalMap.unity";
        private const string RootName = "Factory Vertical Hall";
        private const string MineEntranceName = "Future Enemy Entrance - Mine Service Door";
        private const string GeneratorEntranceName = "Future Enemy Entrance - Generator Service Door";
        private const string RetiredAssemblyEntranceName = "Future Enemy Entrance - Upper Assembly Hatch";
        private const string RetiredSmelterEntranceName = "Future Enemy Entrance - Smelter Maintenance Hatch";

        [Test]
        public void SmelterClearanceEdits_ArePersistedInAuthoritativeScene()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Transform oreHoist = RequirePath(root, "06 Conveyor Network/Enclosed Ore Hoist");
                AssertVector(oreHoist.localPosition, new Vector3(-1.31f, 0f, 0f));
                AssertVector(RequirePath(oreHoist, "Hoist Shaft").position, new Vector3(-18.91f, 2.8f, -2f));

                Transform pipeRackGrating = RequirePath(
                    root,
                    "02 Ground Route - Normal Jump/Pipe Rack Step 2.86/Pipe Rack Grating");
                AssertVector(pipeRackGrating.localPosition, new Vector3(-15.3f, 2.826f, -0.987f));
                Assert.That(pipeRackGrating.GetComponent<BoxCollider>(), Is.Not.Null);

                Transform smelterRamp = RequirePath(
                    root,
                    "12 Enemy Navigation/Smelter Enemy Bridge/Ground to Smelter Service Ramp");
                AssertVector(smelterRamp.position, new Vector3(-8.8084f, 2.495f, -7.6273f), 0.0002f);

                Transform sourceSocket = RequirePath(
                    root,
                    "06 Conveyor Network/Mine to Smelter Production Route/Source Socket - Mine → Smelter Conveyor");
                Transform destinationSocket = RequirePath(
                    root,
                    "06 Conveyor Network/Mine to Smelter Production Route/Destination Socket - Mine → Smelter Conveyor");
                AssertVector(sourceSocket.position, new Vector3(-11.7f, 0.78f, -8.25f));
                AssertVector(destinationSocket.position, new Vector3(-13.2f, 5.42f, -3.2f));
            });
        }

        [Test]
        public void EnemyEntrances_ContainOnlyTheTwoIntendedServiceDoors()
        {
            WithFactoryScene(scene =>
            {
                Transform entranceRoot = RequirePath(GetFactoryRoot(scene), "08 Enemy Entrances");
                string[] entranceNames = Enumerable.Range(0, entranceRoot.childCount)
                    .Select(index => entranceRoot.GetChild(index).name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

                Assert.That(entranceNames, Is.EqualTo(new[] { GeneratorEntranceName, MineEntranceName }));
                Assert.That(entranceNames, Does.Not.Contain(RetiredAssemblyEntranceName));
                Assert.That(entranceNames, Does.Not.Contain(RetiredSmelterEntranceName));
            });
        }

        [Test]
        public void EnemySpawnManager_ReferencesEveryRemainingEntranceWithoutNulls()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                EnemySpawnManager[] managers = root.GetComponentsInChildren<EnemySpawnManager>(true);
                EnemySpawnPoint[] sceneSpawnPoints = root.GetComponentsInChildren<EnemySpawnPoint>(true);

                Assert.That(managers, Has.Length.EqualTo(1));
                Assert.That(sceneSpawnPoints, Has.Length.EqualTo(2));
                Assert.That(managers[0].SpawnPoints.Count, Is.EqualTo(2));
                Assert.That(managers[0].SpawnPoints.All(point => point != null), Is.True);
                Assert.That(managers[0].SpawnPoints, Is.EquivalentTo(sceneSpawnPoints));
                Assert.That(
                    managers[0].SpawnPoints.Select(point => point.transform.parent.name),
                    Is.EquivalentTo(new[] { MineEntranceName, GeneratorEntranceName }));
            });
        }

        [Test]
        public void DeathPresentation_UsesOneThreeLayerEffectForPlayerAndEveryEnemyPrefab()
        {
            GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyAssetFactory.DeathExplosionPrefabPath);
            Assert.That(effectPrefab, Is.Not.Null);
            DeathExplosionEffect effect = effectPrefab.GetComponent<DeathExplosionEffect>();
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.ParticleLayerCount, Is.EqualTo(3));

            string[] enemyPrefabPaths =
            {
                EnemyAssetFactory.DronePrefabPath,
                EnemyAssetFactory.SaboteurPrefabPath,
                EnemyAssetFactory.ArmoredPrefabPath
            };
            for (int index = 0; index < enemyPrefabPaths.Length; index++)
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPaths[index]);
                Assert.That(enemyPrefab, Is.Not.Null, enemyPrefabPaths[index]);
                DeathExplosionEmitter emitter = enemyPrefab.GetComponent<DeathExplosionEmitter>();
                Assert.That(emitter, Is.Not.Null, enemyPrefabPaths[index]);
                Assert.That(emitter.EffectPrefab, Is.SameAs(effectPrefab), enemyPrefabPaths[index]);
            }

            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                PlayerHealth player = root.GetComponentInChildren<PlayerHealth>(true);
                Assert.That(player, Is.Not.Null);
                DeathExplosionEmitter emitter = player.GetComponent<DeathExplosionEmitter>();
                Assert.That(emitter, Is.Not.Null);
                Assert.That(emitter.EffectPrefab, Is.SameAs(effectPrefab));
                Assert.That(root.GetComponentInChildren<PlayerStatusPresenter>(true), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<FactoryGameOverController>(true), Is.Not.Null);
            });
        }

        [Test]
        public void TurretPrefabs_HaveNoBlinkingStatusOrServiceIndicatorObjects()
        {
            string[] prefabPaths =
            {
                "Assets/Game/FactoryDefense/Prefabs/PF_Factory_Turret.prefab",
                "Assets/Game/FactoryDefense/Prefabs/PF_Factory_TurretBuildSpot.prefab"
            };
            string[] removedNames = { "Factory Status Light", "Pulsing Service Indicator" };
            for (int prefabIndex = 0; prefabIndex < prefabPaths.Length; prefabIndex++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[prefabIndex]);
                Assert.That(prefab, Is.Not.Null, prefabPaths[prefabIndex]);
                string[] hierarchyNames = prefab.GetComponentsInChildren<Transform>(true)
                    .Select(transform => transform.name)
                    .ToArray();
                Assert.That(hierarchyNames, Does.Not.Contain(removedNames[0]));
                Assert.That(hierarchyNames, Does.Not.Contain(removedNames[1]));
            }
        }

        [Test]
        public void FactoryCombatFeedback_WiresAudioEffectsAndCameraShakeExplicitly()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Transform player = RequirePath(root, "10 Player Rig/Player");
                Transform camera = RequirePath(root, "10 Player Rig/Main Camera");
                CameraShakeController shake = camera.GetComponent<CameraShakeController>();
                Assert.That(shake, Is.Not.Null);

                PlayerFeedbackEffects playerFeedback = player.GetComponent<PlayerFeedbackEffects>();
                Assert.That(playerFeedback, Is.Not.Null);
                AssertSerializedReference(
                    playerFeedback,
                    "_jumpClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sound-effects-v2_Person_performs_a_jump-2.mp3"));
                AssertSerializedReference(
                    playerFeedback,
                    "_playerHitClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sound-effects-v2_Person_hit-1.mp3"));
                AssertSerializedReference(
                    playerFeedback,
                    "_repairLoopClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hammer Loop_1.wav"));
                AssertSerializedReference(playerFeedback, "_cameraShake", shake);

                string[] machinePaths =
                {
                    "05 Factory Machinery/Mine Extractor",
                    "05 Factory Machinery/Smelter",
                    "05 Factory Machinery/Main Generator",
                    "05 Factory Machinery/Assembler"
                };
                AudioClip rubbleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/freeeverythingxx-rubble-crash-275691.mp3");
                foreach (string path in machinePaths)
                {
                    MachineBreakPresentation presentation = RequirePath(root, path)
                        .GetComponent<MachineBreakPresentation>();
                    Assert.That(presentation, Is.Not.Null, path);
                    Assert.That(presentation.RubbleCrashClip, Is.SameAs(rubbleClip), path);
                    Assert.That(presentation.BreakEffectPrefab, Is.Not.Null, path);
                    AssertSerializedReference(presentation, "_cameraShake", shake);
                }

                EnemySpawnManager spawnManager = root.GetComponentInChildren<EnemySpawnManager>(true);
                Assert.That(spawnManager, Is.Not.Null);
                AssertSerializedReference(spawnManager, "_cameraShake", shake);
            });
        }

        [Test]
        public void UserAuthoredPlatformSupports_ArePersistedInAuthoritativeScene()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Transform feeder = RequirePath(root, "03 Middle Route - Double Jump/Assembler Feeder Conveyor Housing");
                AssertSupportCount(feeder, 6);
                AssertSupport(feeder, new Vector3(10.28f, 5.73f, 8.13f), 4.75f);
                AssertSupport(feeder, new Vector3(12.72f, 5.73f, 8.13f), 4.75f);
                AssertSupport(feeder, new Vector3(10.28f, 1.1f, 8.13f), 4.75f);
                AssertSupport(feeder, new Vector3(12.72f, 1.1f, 8.13f), 4.75f);
                Assert.That(FindSupport(feeder, new Vector3(10.28f, 10.375f, 9.87f)), Is.Null);
                Assert.That(FindSupport(feeder, new Vector3(12.72f, 10.375f, 9.87f)), Is.Null);

                Transform northeast = RequirePath(root, "03 Middle Route - Double Jump/Northeast Assembler Deck");
                AssertSupportCount(northeast, 10);
                AssertSupport(northeast, new Vector3(8.28f, 5.76f, 15.22f), 4.75f);
                AssertSupport(northeast, new Vector3(8.28f, 1.22f, 15.22f), 4.75f);
                AssertSupportTier(northeast, 5.73f, 4.75f, new[] { 8.28f, 16.72f }, new[] { 7.78f });
                AssertSupportTier(northeast, 1.1f, 4.75f, new[] { 8.28f, 16.72f }, new[] { 7.78f });

                Transform crane = RequirePath(root, "04 Upper Route and Recovery/Crane Transfer Plate");
                AssertSupportCount(crane, 12);
                AssertSupportTier(crane, 6.15f, 5.8f, new[] { -0.47f, 2.07f }, new[] { 11.88f, 13.72f });
                AssertSupportTier(crane, 0.58f, 5.8f, new[] { -0.47f, 2.07f }, new[] { 11.88f, 13.72f });

                Transform eastCore = RequirePath(root, "04 Upper Route and Recovery/East Core Balcony");
                AssertSupportCount(eastCore, 12);
                AssertSupportTier(eastCore, 6.14f, 6.05f, new[] { 10.68f, 15.72f }, new[] { 12.53f, 15.47f });
                AssertSupportTier(eastCore, 0.19f, 6.05f, new[] { 10.68f, 15.72f }, new[] { 12.53f, 15.47f });

                Transform north = RequirePath(root, "04 Upper Route and Recovery/North Perimeter Catwalk");
                AssertSupportCount(north, 10);
                AssertSupport(north, new Vector3(-10.72f, 6.14f, 11.28f), 6.05f);
                AssertSupport(north, new Vector3(-10.72f, 6.14f, 12.72f), 6.05f);
                AssertSupport(north, new Vector3(10.72f, 6.14f, 12.72f), 6.05f);
                AssertSupport(north, new Vector3(-10.72f, 0.19f, 11.28f), 6.05f);
                AssertSupport(north, new Vector3(-10.72f, 0.19f, 12.72f), 6.05f);
                AssertSupport(north, new Vector3(10.72f, 0.19f, 12.72f), 6.05f);

                Transform portal = RequirePath(root, "04 Upper Route and Recovery/Portal Deck");
                AssertSupportCount(portal, 12);
                AssertSupportTier(portal, 5.6f, 6.95f, new[] { -4.72f, 4.72f }, new[] { 16.68f, 20.12f });
                AssertSupportTier(portal, -1.14f, 6.95f, new[] { -4.72f, 4.72f }, new[] { 16.68f, 20.12f });

                Transform westRecovery = RequirePath(root, "04 Upper Route and Recovery/West Fall Recovery Balcony");
                AssertSupportCount(westRecovery, 8);
                AssertSupportTier(westRecovery, 1.55f, 4.75f, new[] { -14.72f, -7.28f }, new[] { 6.78f, 11.22f });

                Transform westGantry = RequirePath(root, "04 Upper Route and Recovery/West Gantry");
                AssertSupportCount(westGantry, 12);
                AssertSupportTier(westGantry, 6.14f, 6.05f, new[] { -9.22f, -0.78f }, new[] { 12.18f, 13.82f });
                AssertSupportTier(westGantry, 0.19f, 6.05f, new[] { -9.22f, -0.78f }, new[] { 12.18f, 13.82f });

                int totalSupportCount = root.GetComponentsInChildren<Transform>(true)
                    .Count(transform => transform.name.StartsWith("Support Post", StringComparison.Ordinal));
                Assert.That(totalSupportCount, Is.EqualTo(138));
            });
        }

        [Test]
        public void RemovedMapParts_StayRemovedWhileGeneratorFeedEndpointsRemain()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Assert.That(root.Find("03 Middle Route - Double Jump/Pipe Rack Step 7.80"), Is.Null);

                Transform generatorFeed = RequirePath(root, "06 Conveyor Network/Generator Feed Conveyor");
                ConveyorBelt belt = generatorFeed.GetComponent<ConveyorBelt>();
                Assert.That(belt, Is.Not.Null);
                Assert.That(belt.GeneratedGeometryEnabled, Is.False);
                Assert.That(belt.StartEndpoint, Is.Not.Null);
                Assert.That(belt.EndEndpoint, Is.Not.Null);
                Transform generatedRoot = RequirePath(generatorFeed, "Generated Conveyor");
                Assert.That(generatedRoot.childCount, Is.Zero);
            });
        }

        [Test]
        public void MovedBalconiesUpgradeAndTurretSpots_UseAuthoritativeTransforms()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                AssertVector(RequirePath(root, "04 Upper Route and Recovery/Central Fall Recovery Deck").localPosition, new Vector3(-2.69f, 0f, 4.56f));
                AssertVector(RequirePath(root, "04 Upper Route and Recovery/West Fall Recovery Balcony").localPosition, new Vector3(-3.04f, 0f, -0.82f));
                AssertVector(RequirePath(root, "07 Objectives and Activation/Double Jump Upgrade Station").position, new Vector3(-2.92f, 3.74f, 14.34f));
                AssertVector(RequirePath(root, "Turret Spots/TurretSpot").position, new Vector3(2.829f, 9.34f, -2.865f));
                AssertVector(RequirePath(root, "Turret Spots/TurretSpot (1)").position, new Vector3(-13.12f, 9.04f, 7.045f));
                AssertVector(RequirePath(root, "Turret Spots/TurretSpot (2)").position, new Vector3(6.165f, 15.54f, 12.73f));
            });
        }

        private static void WithFactoryScene(Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                assertion(scene);
            }
            finally
            {
                if (openedForTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static Transform GetFactoryRoot(Scene scene)
        {
            Transform root = scene.GetRootGameObjects()
                .Select(gameObject => gameObject.transform)
                .SingleOrDefault(candidate => candidate.name == RootName);
            Assert.That(root, Is.Not.Null, "The authoritative factory root is missing.");
            return root;
        }

        private static Transform RequirePath(Transform root, string path)
        {
            Transform result = root.Find(path);
            Assert.That(result, Is.Not.Null, "Missing required scene path: " + root.name + "/" + path);
            return result;
        }

        private static void AssertVector(Vector3 actual, Vector3 expected, float tolerance = 0.0001f)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThanOrEqualTo(tolerance),
                "Expected " + expected.ToString("F4") + " but found " + actual.ToString("F4") + ".");
        }

        private static void AssertSupportCount(Transform parent, int expectedCount)
        {
            int count = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Count(child => child.name.StartsWith("Support Post", StringComparison.Ordinal));
            Assert.That(count, Is.EqualTo(expectedCount), parent.name);
        }

        private static void AssertSupportTier(
            Transform parent,
            float y,
            float height,
            float[] xPositions,
            float[] zPositions)
        {
            foreach (float x in xPositions)
            {
                foreach (float z in zPositions)
                {
                    AssertSupport(parent, new Vector3(x, y, z), height);
                }
            }
        }

        private static void AssertSupport(Transform parent, Vector3 localPosition, float expectedHeight)
        {
            Transform support = FindSupport(parent, localPosition);
            Assert.That(support, Is.Not.Null, parent.name + " is missing a support at " + localPosition.ToString("F3") + ".");
            AssertVector(support.localScale, new Vector3(0.32f, expectedHeight, 0.32f));
        }

        private static Transform FindSupport(Transform parent, Vector3 localPosition)
        {
            return Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .FirstOrDefault(child =>
                    child.name.StartsWith("Support Post", StringComparison.Ordinal) &&
                    Vector3.Distance(child.localPosition, localPosition) <= 0.0001f);
        }

        private static void AssertSerializedReference(
            Component component,
            string propertyName,
            UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, component.GetType().Name + "." + propertyName);
            Assert.That(property.objectReferenceValue, Is.SameAs(expected), propertyName);
        }
    }
}
