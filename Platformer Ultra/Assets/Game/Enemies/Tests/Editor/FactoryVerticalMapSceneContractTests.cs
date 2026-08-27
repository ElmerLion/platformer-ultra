using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies.Editor;
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
    }
}
