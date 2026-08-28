using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies.Editor;
using PlatformerUltra.Factory.Conveyors;
using PlatformerUltra.FactoryDefense;
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

                Transform pipeRack = pipeRackGrating.parent;
                AssertVector(pipeRack.localPosition, new Vector3(-0.881f, -0.49f, 0f));
                AssertPipeRackPost(pipeRack, new Vector3(-16.644001f, 2.21f, -0.95f));
                AssertPipeRackPost(pipeRack, new Vector3(-16.644001f, 0.95f, -0.95f));
                AssertPipeRackPost(pipeRack, new Vector3(-16.644001f, -0.27f, -0.95f));
                AssertPipeRackPost(pipeRack, new Vector3(-13.956f, 2.21f, -0.95f));
                AssertPipeRackPost(pipeRack, new Vector3(-13.956f, 0.95f, -0.95f));
                AssertPipeRackPost(pipeRack, new Vector3(-13.956f, -0.27f, -0.95f));

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
                AssertVector(sourceSocket.position, new Vector3(-11.7f, 0.219f, -8.25f));
                AssertVector(destinationSocket.position, new Vector3(-13.2f, 5.42f, -3.2f));
            });
        }

        [Test]
        public void MineToSmelterConveyor_RisesBeforeRunningForwardAndRouteMarkersAreRemoved()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Transform route = RequirePath(root, "06 Conveyor Network/Mine to Smelter Production Route");
                FactoryConveyorConnection connection = route.GetComponent<FactoryConveyorConnection>();
                Assert.That(connection, Is.Not.Null);
                Assert.That(connection.Conveyors, Has.Length.EqualTo(2));

                ConveyorBelt lift = connection.Conveyors[0];
                ConveyorBelt forwardRun = connection.Conveyors[1];
                AssertVector(lift.StartPosition, new Vector3(-13f, 0.72f, -9.95f));
                AssertVector(lift.EndPosition, new Vector3(-13f, 5.72f, -8f));
                AssertVector(forwardRun.StartPosition, lift.EndPosition);
                AssertVector(forwardRun.EndPosition, new Vector3(-12.02f, 5.72f, -2.05f));
                Assert.That(lift.EndPosition.y - lift.StartPosition.y, Is.EqualTo(5f).Within(0.0001f));
                Assert.That(Mathf.Abs(forwardRun.EndPosition.y - forwardRun.StartPosition.y), Is.LessThan(0.0001f));

                BoxCollider mezzanineDeck = RequirePath(
                    root,
                    "02 Ground Route - Normal Jump/West Smelter Mezzanine/Main Deck/Walkable Grating")
                    .GetComponent<BoxCollider>();
                Assert.That(mezzanineDeck, Is.Not.Null);
                Assert.That(forwardRun.StartPosition.y, Is.GreaterThan(mezzanineDeck.bounds.max.y + 0.4f));

                Transform[] routeMarkers = root.GetComponentsInChildren<Transform>(true)
                    .Where(transform => transform.name.StartsWith("Route Marker", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(routeMarkers, Is.Empty);
            });
        }

        [Test]
        public void FactoryMachines_UsePurpleIdentityMaterialAndFittedCompoundColliders()
        {
            Material machineMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Factory/Materials/M_Factory_MachinePurple.mat");
            Assert.That(machineMaterial, Is.Not.Null);

            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                string[] purpleRendererPaths =
                {
                    "05 Factory Machinery/Mine Extractor/Extractor Housing",
                    "05 Factory Machinery/Smelter/Furnace Housing",
                    "05 Factory Machinery/Main Generator/Generator Housing",
                    "05 Factory Machinery/Assembler/Machine Cabinet",
                    "05 Factory Machinery/Piston Crusher/Anvil Base"
                };
                foreach (string path in purpleRendererPaths)
                {
                    Renderer renderer = RequirePath(root, path).GetComponent<Renderer>();
                    Assert.That(renderer, Is.Not.Null, path);
                    Assert.That(renderer.sharedMaterial, Is.SameAs(machineMaterial), path);
                }

                Transform mine = RequirePath(root, "05 Factory Machinery/Mine Extractor");
                Transform smelter = RequirePath(root, "05 Factory Machinery/Smelter");
                Transform generator = RequirePath(root, "05 Factory Machinery/Main Generator");
                Transform assembler = RequirePath(root, "05 Factory Machinery/Assembler");
                Transform crusher = RequirePath(root, "05 Factory Machinery/Piston Crusher");

                AssertMachineColliderCount(mine, 5);
                AssertMachineColliderCount(smelter, 6);
                AssertMachineColliderCount(generator, 5);
                AssertMachineColliderCount(assembler, 3);
                AssertMachineColliderCount(crusher, 7);

                AssertBoxCollider(smelter, new Vector3(0f, 0.25f, 0f), new Vector3(5.4f, 0.5f, 4.2f));
                AssertBoxCollider(smelter, new Vector3(0f, 1.8f, 0.25f), new Vector3(2.6f, 3.1f, 2.5f));
                AssertCapsuleCollider(smelter, new Vector3(-1.85f, 1.45f, 0.7f), 0.47f, 2.6f, 1);
                AssertCapsuleCollider(smelter, new Vector3(1.85f, 1.45f, 0.7f), 0.47f, 2.6f, 1);
                AssertCapsuleCollider(smelter, new Vector3(1.7f, 0.9f, -1.05f), 0.42f, 1.8f, 0);
                AssertCapsuleCollider(smelter, new Vector3(0f, 4.55f, 0.65f), 0.38f, 3.8f, 1);

                AssertBoxCollider(assembler, new Vector3(0f, 0.25f, 0f), new Vector3(5.5f, 0.5f, 4.3f));
                AssertBoxCollider(assembler, new Vector3(0f, 1.8f, 0.7f), new Vector3(4.6f, 3.1f, 1.8f));
                AssertBoxCollider(assembler, new Vector3(0f, 0.72f, -1.55f), new Vector3(2.7f, 0.18f, 1.35f));

                AssertBoxCollider(crusher, new Vector3(0f, 0.3f, 0f), new Vector3(4.5f, 0.6f, 3.7f));
                AssertBoxCollider(crusher, new Vector3(0f, 4.3f, 0f), new Vector3(4.4f, 0.48f, 3.55f));
                BoxCollider movingPlateCollider = RequirePath(crusher, "Crushing Plate").GetComponent<BoxCollider>();
                Assert.That(movingPlateCollider, Is.Not.Null);
                AssertVector(movingPlateCollider.center, Vector3.zero);
                AssertVector(movingPlateCollider.size, Vector3.one);
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

                Transform mine = RequirePath(entranceRoot, MineEntranceName);
                AssertVector(mine.position, new Vector3(-19.92f, 1.55f, -12f));
                AssertVector(mine.eulerAngles, new Vector3(0f, 90f, 0f));
                AssertDoorLayout(mine, 1.8872f, 0.25f, 2.06f, 0.16f, 0.00195f,
                    new Vector3(0.28527f, 3.84020f, 0.37305f),
                    new Vector3(3.94992f, 0.30722f, 0.37305f),
                    new Vector3(3.51104f, 3.40132f, 0.26333f));

                Transform generator = RequirePath(entranceRoot, GeneratorEntranceName);
                AssertVector(generator.position, new Vector3(19.92f, 1.55f, -8f));
                AssertVector(generator.eulerAngles, new Vector3(0f, 270f, 0f));
                AssertDoorLayout(generator, 1.9328f, 0.38f, 2.23f, 0.29f, 0.00248f,
                    new Vector3(0.29216f, 3.93295f, 0.38206f),
                    new Vector3(4.04532f, 0.31464f, 0.38206f),
                    new Vector3(3.59584f, 3.48347f, 0.26969f));
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

            GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[0]);
            FactoryTurret turret = turretPrefab.GetComponent<FactoryTurret>();
            Assert.That(turret, Is.Not.Null);
            Assert.That(turret.Range, Is.EqualTo(15f).Within(0.0001f));
            Assert.That(turret.LaserTracerPrefab, Is.Not.Null);
            Assert.That(turret.LaserTracerPrefab.GetComponent<LineRenderer>(), Is.Not.Null);
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
                Assert.That(
                    new SerializedObject(playerFeedback).FindProperty("_jumpClip"),
                    Is.Null,
                    "Jump feedback must remain visual-only.");
                AssertSerializedReference(
                    playerFeedback,
                    "_playerHitClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sound-effects-v2_Person_hit-1.mp3"));
                AssertSerializedReference(
                    playerFeedback,
                    "_repairLoopClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hammer Loop_1.wav"));
                AssertSerializedReference(
                    playerFeedback,
                    "_dashClip",
                    AssetDatabase.LoadAssetAtPath<AudioClip>(
                        "Assets/Audio/sound-effects-v2_Person_performs_a_jump-2.mp3"));
                AssertSerializedReference(
                    playerFeedback,
                    "_dashEffectPrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(EnemyAssetFactory.PlayerDashEffectPath));
                AssertSerializedReference(playerFeedback, "_cameraShake", shake);

                TurretBuildSpot[] buildSpots = root.GetComponentsInChildren<TurretBuildSpot>(true);
                Assert.That(buildSpots, Has.Length.EqualTo(3));
                Assert.That(
                    buildSpots.All(spot => spot is IMaintenanceTimedInteractable),
                    Is.True,
                    "Turret construction must use the same maintenance loop as machine repairs.");

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

                Assert.That(root.Find("04 Upper Route and Recovery/Crane Transfer Plate"), Is.Null);

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

                Transform crusherLedge = RequirePath(root, "03 Middle Route - Double Jump/Crusher Service Ledge");
                AssertVector(crusherLedge.localPosition, new Vector3(-1.56f, 0f, 1.23f));
                AssertSupportCount(crusherLedge, 4);
                AssertSupportTier(crusherLedge, 3.54f, 8.402899f, new[] { 3.27999973f, 5.62f }, new[] { 2.03f, 3.97f });

                Transform generatorCatwalk = RequirePath(root, "03 Middle Route - Double Jump/Generator Belt Catwalk");
                AssertSupportCount(generatorCatwalk, 0);

                Transform southwestSmelterWing = RequirePath(root, "02 Ground Route - Normal Jump/West Smelter Mezzanine/Southwest Access Wing");
                AssertSupportCount(southwestSmelterWing, 2);
                AssertSupport(southwestSmelterWing, new Vector3(-17.72f, 2.375f, -4.62f), 4.75f);
                AssertSupport(southwestSmelterWing, new Vector3(-17.72f, 2.375f, -2.48f), 4.75f);
                Assert.That(FindSupport(southwestSmelterWing, new Vector3(-15.18f, 2.375f, -4.62f)), Is.Null);
                Assert.That(FindSupport(southwestSmelterWing, new Vector3(-15.18f, 2.375f, -2.48f)), Is.Null);

                int totalSupportCount = root.GetComponentsInChildren<Transform>(true)
                    .Count(transform => transform.name.StartsWith("Support Post", StringComparison.Ordinal));
                Assert.That(totalSupportCount, Is.EqualTo(116));
            });
        }

        [Test]
        public void PumpSkidTrimEdits_ArePersistedInAuthoritativeScene()
        {
            WithFactoryScene(scene =>
            {
                Transform route = RequirePath(GetFactoryRoot(scene), "02 Ground Route - Normal Jump");
                Transform housing = RequirePath(route, "Pump Skid Housing");
                Transform topPlate = RequirePath(route, "Pump Skid Top Plate");
                Transform orangeBand = RequirePath(route, "Pump Skid Orange Band");

                Assert.That(housing.gameObject.activeSelf, Is.True);
                Assert.That(topPlate.gameObject.activeSelf, Is.True);
                Assert.That(orangeBand.gameObject.activeSelf, Is.True);
                AssertVector(housing.localPosition, new Vector3(-16.28f, 0.17f, -3.35f));
                AssertVector(housing.localScale, new Vector3(2.7f, 1.72f, 2.5f));
                AssertVector(topPlate.localPosition, new Vector3(-16.28f, 1.074f, -3.35f));
                AssertVector(topPlate.localScale, new Vector3(2.88f, 0.2f, 2.68f));
                AssertVector(orangeBand.localPosition, new Vector3(-16.28f, 0.824f, -4.625f));
                AssertVector(orangeBand.localScale, new Vector3(2.7f, 0.22f, 0.12f));
            });
        }

        [Test]
        public void MovedGroundRouteStructures_UseAuthoritativeTransforms()
        {
            WithFactoryScene(scene =>
            {
                Transform route = RequirePath(GetFactoryRoot(scene), "02 Ground Route - Normal Jump");

                Assert.That(route.Find("Ore Belt Service Deck"), Is.Null);
                AssertVector(RequirePath(route, "Valve Housing Housing").localPosition, new Vector3(-16.31f, 1.99f, 1.35f));
                AssertVector(RequirePath(route, "Valve Housing Top Plate").localPosition, new Vector3(-16.31f, 4.03f, 1.35f));
                AssertVector(RequirePath(route, "Valve Housing Orange Band").localPosition, new Vector3(-16.31f, 3.78f, 0.228f));
                AssertVector(RequirePath(route, "Smelter Landing").localPosition, new Vector3(-0.93f, 0f, 0f));
            });
        }

        [Test]
        public void RemovedMapParts_StayRemovedWhileGeneratorFeedEndpointsRemain()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                Assert.That(root.Find("03 Middle Route - Double Jump/Pipe Rack Step 7.80"), Is.Null);
                Assert.That(root.Find("02 Ground Route - Normal Jump/Ore Belt Service Deck"), Is.Null);
                Assert.That(root.Find("02 Ground Route - Normal Jump/Inactive Intake Belt Housing"), Is.Null);
                Assert.That(root.Find("04 Upper Route and Recovery/Crane Transfer Plate"), Is.Null);
                Assert.That(root.Find("04 Upper Route and Recovery/Upper Material Pipe"), Is.Null);

                Transform generatorCatwalk = RequirePath(root, "03 Middle Route - Double Jump/Generator Belt Catwalk");
                Assert.That(
                    Enumerable.Range(0, generatorCatwalk.childCount)
                        .Select(generatorCatwalk.GetChild)
                        .Any(child => child.name.StartsWith("Support Post", StringComparison.Ordinal)),
                    Is.False);

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
        public void SmelterUnlocksSpawningAndSceneWiresPauseVictoryAtmosphereAndAmbience()
        {
            WithFactoryScene(scene =>
            {
                Transform root = GetFactoryRoot(scene);
                FactoryObjectiveTerminal smelter = RequirePath(
                    root,
                    "07 Objectives and Activation/Smelter Activation Terminal")
                    .GetComponent<FactoryObjectiveTerminal>();
                EnemySpawnManager spawnManager = RequirePath(
                    root,
                    "13 Enemy Systems/Enemy Spawn Manager")
                    .GetComponent<EnemySpawnManager>();
                Assert.That(smelter, Is.Not.Null);
                Assert.That(spawnManager, Is.Not.Null);
                AssertSerializedReference(spawnManager, "_spawnUnlockTerminal", smelter);

                MachineTargetRegistry machineRegistry = RequirePath(
                    root,
                    "13 Enemy Systems/Machine Target Registry")
                    .GetComponent<MachineTargetRegistry>();
                FactoryMachineHealth[] machines = root.GetComponentsInChildren<FactoryMachineHealth>(true);
                Assert.That(machineRegistry, Is.Not.Null);
                Assert.That(machines, Has.Length.EqualTo(4));
                foreach (FactoryMachineHealth machine in machines)
                {
                    AssertSerializedReference(machine, "_registry", machineRegistry);
                }

                Transform hud = RequirePath(root, "10 Player Rig/Factory HUD");
                Assert.That(hud.GetComponent<FactoryPauseController>(), Is.Not.Null);
                Assert.That(hud.GetComponent<FactoryVictoryController>(), Is.Not.Null);
                FactoryHudPresenter factoryHud = hud.GetComponent<FactoryHudPresenter>();
                Assert.That(factoryHud, Is.Not.Null);
                Transform portal = RequirePath(root, "05 Factory Machinery/Factory Exit Portal");
                Component portalGate = portal.GetComponent(
                    Type.GetType("PlatformerUltra.Factory.FactoryPortalGate, Assembly-CSharp", true));
                Assert.That(portalGate, Is.Not.Null);
                Assert.That(
                    new SerializedObject(portalGate).FindProperty("_requiredCoreCount").intValue,
                    Is.EqualTo(3));
                AssertSerializedReference(factoryHud, "_portalReceiverBehaviour", portalGate);
                Assert.That(
                    portal.GetComponentInChildren(
                        Type.GetType("PlatformerUltra.Factory.FactoryPortalCompletionTrigger, Assembly-CSharp"), true),
                    Is.Not.Null);

                Component volume = RequirePath(root, "09 Lighting/Factory Global Volume").GetComponent("Volume");
                Assert.That(volume, Is.Not.Null);
                AssertSerializedReference(
                    volume,
                    "sharedProfile",
                    AssetDatabase.LoadMainAssetAtPath("Assets/Game/Factory/Lighting/VP_FactoryAtmosphere.asset"));

                AssertAmbientLoop(root, "Mine Machinery Ambience", "Assets/Audio/Miner.wav", 0.11f, 15f);
                AssertAmbientLoop(root, "Smelter Fire Ambience", "Assets/Audio/IndustrialFireBUrning.mp3", 0.095f, 14f);
                AssertAmbientLoop(root, "Crusher Mechanism Ambience", "Assets/Audio/Crusher.mp3", 0.08f, 13f);
                AudioReverbZone reverb = RequirePath(root, "11 Audio/Factory Hall Reverb")
                    .GetComponent<AudioReverbZone>();
                Assert.That(reverb, Is.Not.Null);
                Assert.That(reverb.reverbPreset, Is.EqualTo(AudioReverbPreset.Hangar));

                Scene previousActive = SceneManager.GetActiveScene();
                Assert.That(SceneManager.SetActiveScene(scene), Is.True);
                try
                {
                    Assert.That(
                        RenderSettings.skybox,
                        Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>("Assets/Starfield Skybox/Skybox.mat")));
                }
                finally
                {
                    if (previousActive.IsValid() && previousActive.isLoaded)
                    {
                        SceneManager.SetActiveScene(previousActive);
                    }
                }

                int missingScriptCount = root.GetComponentsInChildren<Transform>(true)
                    .Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
                Assert.That(missingScriptCount, Is.Zero);
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

        private static void AssertDoorLayout(
            Transform root,
            float sideOffset,
            float sideY,
            float topY,
            float slabY,
            float slabZ,
            Vector3 sideScale,
            Vector3 topScale,
            Vector3 slabScale)
        {
            Transform left = RequirePath(root, "Door Frame Left");
            Transform right = RequirePath(root, "Door Frame Right");
            Transform top = RequirePath(root, "Door Frame Top");
            Transform slab = RequirePath(root, "Raised Door Slab");
            AssertVector(left.localPosition, new Vector3(-sideOffset, sideY, -0.02f));
            AssertVector(right.localPosition, new Vector3(sideOffset, sideY, -0.02f));
            AssertVector(left.localScale, sideScale);
            AssertVector(right.localScale, sideScale);
            AssertVector(top.localPosition, new Vector3(0f, topY, -0.02f));
            AssertVector(top.localScale, topScale);
            AssertVector(slab.localPosition, new Vector3(0f, slabY, slabZ));
            AssertVector(slab.localScale, slabScale);
            Assert.That(slab.GetComponent<Collider>(), Is.Null);
            EnemySpawnPoint spawnPoint = root.GetComponentInChildren<EnemySpawnPoint>(true);
            Assert.That(spawnPoint, Is.Not.Null);
            AssertVector(spawnPoint.transform.localPosition, new Vector3(0f, -1.5f, 2.25f));
        }

        private static void AssertAmbientLoop(
            Transform root,
            string name,
            string clipPath,
            float expectedVolume,
            float expectedMaximumDistance)
        {
            AudioSource source = RequirePath(root, "11 Audio/" + name).GetComponent<AudioSource>();
            Assert.That(source, Is.Not.Null);
            Assert.That(source.clip, Is.SameAs(AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath)));
            Assert.That(source.loop, Is.True);
            Assert.That(source.playOnAwake, Is.True);
            Assert.That(source.volume, Is.EqualTo(expectedVolume).Within(0.0001f));
            Assert.That(source.spatialBlend, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(source.maxDistance, Is.EqualTo(expectedMaximumDistance).Within(0.0001f));
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

        private static void AssertPipeRackPost(Transform parent, Vector3 localPosition)
        {
            Transform post = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .FirstOrDefault(child =>
                    child.name.StartsWith("Pipe Rack Post", StringComparison.Ordinal) &&
                    Vector3.Distance(child.localPosition, localPosition) <= 0.0001f);
            Assert.That(post, Is.Not.Null, parent.name + " is missing a pipe-rack support at " + localPosition.ToString("F3") + ".");
            AssertVector(post.localScale, new Vector3(0.22f, 1.3f, 2.1f));
            BoxCollider collider = post.GetComponent<BoxCollider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.isTrigger, Is.False);
        }

        private static void AssertMachineColliderCount(Transform machine, int expectedCount)
        {
            Collider[] colliders = machine.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders, Has.Length.EqualTo(expectedCount), machine.name);
            Assert.That(colliders.All(collider => collider.enabled && !collider.isTrigger), Is.True, machine.name);
        }

        private static void AssertBoxCollider(Transform target, Vector3 center, Vector3 size)
        {
            BoxCollider collider = target.GetComponents<BoxCollider>()
                .FirstOrDefault(candidate =>
                    Vector3.Distance(candidate.center, center) <= 0.0001f &&
                    Vector3.Distance(candidate.size, size) <= 0.0001f);
            Assert.That(collider, Is.Not.Null,
                target.name + " is missing a fitted box collider at " + center.ToString("F3") + ".");
        }

        private static void AssertCapsuleCollider(
            Transform target,
            Vector3 center,
            float radius,
            float height,
            int direction)
        {
            CapsuleCollider collider = target.GetComponents<CapsuleCollider>()
                .FirstOrDefault(candidate =>
                    Vector3.Distance(candidate.center, center) <= 0.0001f &&
                    Mathf.Abs(candidate.radius - radius) <= 0.0001f &&
                    Mathf.Abs(candidate.height - height) <= 0.0001f &&
                    candidate.direction == direction);
            Assert.That(collider, Is.Not.Null,
                target.name + " is missing a fitted capsule collider at " + center.ToString("F3") + ".");
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
