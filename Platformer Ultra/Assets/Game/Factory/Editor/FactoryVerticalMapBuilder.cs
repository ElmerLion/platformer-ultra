using System;
using System.Collections.Generic;
using System.Linq;
using PlatformerUltra.Audio;
using PlatformerUltra.Audio.Editor;
using PlatformerUltra.Combat;
using PlatformerUltra.Enemies;
using PlatformerUltra.Enemies.Editor;
using PlatformerUltra.Factory.Conveyors;
using PlatformerUltra.FactoryDefense;
using PlatformerUltra.Gameplay;
using PlatformerUltra.Gameplay.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PlatformerUltra.Factory.Editor
{
    public static class FactoryVerticalMapBuilder
    {
        private const string FactoryRoot = "Assets/Game/Factory";
        private const string PrefabFolder = FactoryRoot + "/Prefabs";
        private const string MaterialFolder = FactoryRoot + "/Materials";
        private const string SceneFolder = "Assets/Game/Scenes";
        private const string ScenePath = SceneFolder + "/FactoryVerticalMap.unity";
        private const string NavMeshFolder = SceneFolder + "/FactoryVerticalMap";
        private const string NavMeshDataPath = NavMeshFolder + "/NavMesh-EnemyService.asset";
        private const string LightingFolder = FactoryRoot + "/Lighting";
        private const string VolumeProfilePath = LightingFolder + "/VP_FactoryAtmosphere.asset";

        private const string MinePrefabPath = PrefabFolder + "/PF_Factory_Mine.prefab";
        private const string GeneratorPrefabPath = PrefabFolder + "/PF_Factory_Generator.prefab";
        private const string UpgradePrefabPath = PrefabFolder + "/PF_Factory_DoubleJumpStation.prefab";
        private const string CranePrefabPath = PrefabFolder + "/PF_Factory_OverheadCrane.prefab";
        private const string OreCargoPrefabPath = PrefabFolder + "/PF_Factory_OreCargo.prefab";
        private const string IngotCargoPrefabPath = PrefabFolder + "/PF_Factory_IngotCargo.prefab";
        private const string PortalComponentCargoPrefabPath = PrefabFolder + "/PF_Factory_PortalComponentCargo.prefab";

        private const string SmelterPrefabPath = PrefabFolder + "/PF_Factory_Smelter.prefab";
        private const string AssemblerPrefabPath = PrefabFolder + "/PF_Factory_Assembler.prefab";
        private const string CrusherPrefabPath = PrefabFolder + "/PF_Factory_Crusher.prefab";
        private const string PortalPrefabPath = PrefabFolder + "/PF_Factory_Portal.prefab";
        private const string PortalCorePrefabPath = PrefabFolder + "/PF_Factory_PortalCore.prefab";
        private const string ConveyorPrefabPath = FactoryRoot + "/Conveyors/Prefabs/PF_Conveyor_PointToPoint.prefab";
        private const string ConveyorTurnPrefabPath = FactoryRoot + "/Conveyors/Prefabs/PF_Conveyor_Turn.prefab";

        private const string PlayerPrefabPath = "Assets/Game/Gameplay/Prefabs/PF_Player_Prototype.prefab";
        private const string DroneEnemyPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Drone.prefab";
        private const string SaboteurEnemyPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Saboteur.prefab";
        private const string ArmoredEnemyPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Armored.prefab";
        private const string MovementSettingsPath = "Assets/Game/Gameplay/Data/DA_PlayerMovement_Prototype.asset";
        private const string MoveReferencePath = "Assets/Game/Input/IAR_Move.asset";
        private const string LookReferencePath = "Assets/Game/Input/IAR_Look.asset";
        private const string JumpReferencePath = "Assets/Game/Input/IAR_Jump.asset";
        private const string SprintReferencePath = "Assets/Game/Input/IAR_Sprint.asset";
        private const string DashReferencePath = "Assets/Game/Input/IAR_Dash.asset";
        private const string InteractReferencePath = "Assets/Game/Input/IAR_Interact.asset";
        private const string PauseReferencePath = "Assets/Game/Input/IAR_Pause.asset";
        private const string InputActionsPath = "Assets/Game/Input/IA_Gameplay.asset";
        private const string PanelSettingsPath = "Assets/Game/UI/PS_PrototypeHUD.asset";
        private const string HudLayoutPath = "Assets/Game/UI/FactoryMapHUD.uxml";
        private const string HudStylePath = "Assets/Game/UI/PrototypeHUD.uss";

        private const string MinerAudioPath = "Assets/Audio/Miner.wav";
        private const string SmelterAmbienceAudioPath = "Assets/Audio/IndustrialFireBUrning.mp3";
        private const string CrusherAmbienceAudioPath = "Assets/Audio/Crusher.mp3";
        private const string RubbleCrashAudioPath = "Assets/Audio/freeeverythingxx-rubble-crash-275691.mp3";
        private const string PlayerHitAudioPath = "Assets/Audio/sound-effects-v2_Person_hit-1.mp3";
        private const string RepairHammerAudioPath = "Assets/Audio/Hammer Loop_1.wav";
        private const string MusicTrackOnePath = "Assets/Audio/Music/LOOP_Casual Puzzle Solving 1 (live).wav";
        private const string MusicTrackTwoPath = "Assets/Audio/Music/LOOP_Casual Puzzle Solving 2 (live).wav";
        private const string MusicTrackThreePath = "Assets/Audio/Music/LOOP_Casual Puzzle Solving 4.wav";
        private const string SkyboxMaterialPath = "Assets/Starfield Skybox/Skybox.mat";

        private const string FrameMaterialPath = FactoryRoot + "/Conveyors/Materials/M_Conveyor_Frame.mat";
        private const string DarkMaterialPath = FactoryRoot + "/Conveyors/Materials/M_Conveyor_Belt.mat";
        private const string SteelMaterialPath = FactoryRoot + "/Conveyors/Materials/M_Conveyor_Accent.mat";
        private const string EnergyMaterialPath = MaterialFolder + "/M_Factory_EmissiveCyan.mat";
        private const string FurnaceMaterialPath = MaterialFolder + "/M_Factory_EmissiveOrange.mat";
        private const string ActiveMaterialPath = MaterialFolder + "/M_Factory_IndicatorGreen.mat";
        private const string BrokenMarkerMaterialPath = MaterialFolder + "/M_Factory_BrokenMarker.mat";
        private const string MachineBodyMaterialPath = MaterialFolder + "/M_Factory_MachinePurple.mat";
        private const string EnergyParticleMaterialPath = MaterialFolder + "/M_Factory_EnergyParticle.mat";
        private const string FloorMaterialPath = MaterialFolder + "/M_Factory_MapFloor.mat";
        private const string DeckMaterialPath = MaterialFolder + "/M_Factory_MapDeck.mat";
        private const string WallMaterialPath = MaterialFolder + "/M_Factory_MapWall.mat";
        private const string HazardMaterialPath = MaterialFolder + "/M_Factory_MapHazard.mat";
        private const string OreMaterialPath = MaterialFolder + "/M_Factory_MapOre.mat";

        [MenuItem("Tools/Factory/Build Vertical Factory Map")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(LightingFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(NavMeshFolder);

            EnsurePauseInputAction();
            FactoryMachineAssetFactory.BuildAll();
            PrototypeAssetFactory.BuildPlayerCharacterOnly();
            EnemyAssetFactory.BuildAll();
            MapMaterials materials = BuildMapMaterials();
            FactoryDefenseAssetFactory.BuildAll();
            BuildUpgradeStationPrefab(materials);
            BuildCranePrefab(materials);
            BuildScene(materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddSceneToBuildSettings();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Vertical factory map built at " + ScenePath + ".");
        }

        [MenuItem("Tools/Factory/Rebuild Vertical Factory Scene Only")]
        public static void RebuildSceneOnly()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EnsureFolder(SceneFolder);
            EnsureFolder(NavMeshFolder);
            EnsurePauseInputAction();

            BuildScene(LoadMapMaterials());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddSceneToBuildSettings();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Vertical factory scene and enemy NavMesh rebuilt at " + ScenePath + ".");
        }

        private static void EnsurePauseInputAction()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new InvalidOperationException(
                    "Missing gameplay input actions at " + InputActionsPath +
                    ". Run Tools/Platformer Ultra/Build Prototype Assets and Conveyor Test Scene first.");
            }

            InputActionMap gameplay = inputActions.FindActionMap("Gameplay", true);
            InputAction pause = gameplay.FindAction("Pause");
            if (pause == null)
            {
                pause = gameplay.AddAction("Pause", InputActionType.Button);
                pause.AddBinding("<Keyboard>/escape");
                pause.AddBinding("<Gamepad>/start");
                EditorUtility.SetDirty(inputActions);
            }

            InputActionReference pauseReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(
                PauseReferencePath);
            if (pauseReference != null && pauseReference.action == pause)
            {
                return;
            }

            if (pauseReference != null)
            {
                AssetDatabase.DeleteAsset(PauseReferencePath);
            }

            pauseReference = InputActionReference.Create(pause);
            pauseReference.name = "IAR_Pause";
            AssetDatabase.CreateAsset(pauseReference, PauseReferencePath);
        }

        private static MapMaterials BuildMapMaterials()
        {
            return new MapMaterials
            {
                Frame = RequireMaterial(FrameMaterialPath, new Color(0.88f, 0.31f, 0.055f), 0.5f, 0.4f, Color.black),
                Dark = RequireMaterial(DarkMaterialPath, new Color(0.045f, 0.055f, 0.062f), 0.2f, 0.3f, Color.black),
                Steel = RequireMaterial(SteelMaterialPath, new Color(0.42f, 0.52f, 0.58f), 0.75f, 0.62f, Color.black),
                Machine = RequireMaterial(MachineBodyMaterialPath, new Color(0.38f, 0.12f, 0.58f), 0.42f, 0.5f, new Color(0.08f, 0.01f, 0.14f)),
                Energy = RequireMaterial(EnergyMaterialPath, new Color(0.05f, 0.72f, 0.95f), 0.1f, 0.75f, new Color(0.1f, 2.2f, 4.2f)),
                Furnace = RequireMaterial(FurnaceMaterialPath, new Color(1f, 0.25f, 0.025f), 0.1f, 0.5f, new Color(4f, 0.35f, 0.02f)),
                Active = RequireMaterial(ActiveMaterialPath, new Color(0.08f, 0.88f, 0.4f), 0.1f, 0.6f, new Color(0.05f, 1.7f, 0.32f)),
                Broken = RequireMaterial(BrokenMarkerMaterialPath, new Color(1f, 0.035f, 0.02f), 0.05f, 0.68f, new Color(5.5f, 0.03f, 0.01f)),
                EnergyParticle = RequireAsset<Material>(EnergyParticleMaterialPath),
                Floor = CreateOrUpdateMaterial(FloorMaterialPath, new Color(0.075f, 0.085f, 0.09f), 0.72f, 0.35f, Color.black),
                Deck = CreateOrUpdateMaterial(DeckMaterialPath, new Color(0.13f, 0.16f, 0.175f), 0.7f, 0.46f, Color.black),
                Wall = CreateOrUpdateMaterial(WallMaterialPath, new Color(0.055f, 0.065f, 0.075f), 0.45f, 0.28f, Color.black),
                Hazard = CreateOrUpdateMaterial(HazardMaterialPath, new Color(0.98f, 0.57f, 0.035f), 0.2f, 0.35f, new Color(0.28f, 0.045f, 0f)),
                Ore = CreateOrUpdateMaterial(OreMaterialPath, new Color(0.12f, 0.24f, 0.29f), 0.75f, 0.3f, new Color(0.01f, 0.12f, 0.16f))
            };
        }

        private static MapMaterials LoadMapMaterials()
        {
            return new MapMaterials
            {
                Frame = RequireAsset<Material>(FrameMaterialPath),
                Dark = RequireAsset<Material>(DarkMaterialPath),
                Steel = RequireAsset<Material>(SteelMaterialPath),
                Machine = RequireAsset<Material>(MachineBodyMaterialPath),
                Energy = RequireAsset<Material>(EnergyMaterialPath),
                Furnace = RequireAsset<Material>(FurnaceMaterialPath),
                Active = RequireAsset<Material>(ActiveMaterialPath),
                Broken = RequireAsset<Material>(BrokenMarkerMaterialPath),
                EnergyParticle = RequireAsset<Material>(EnergyParticleMaterialPath),
                Floor = RequireAsset<Material>(FloorMaterialPath),
                Deck = RequireAsset<Material>(DeckMaterialPath),
                Wall = RequireAsset<Material>(WallMaterialPath),
                Hazard = RequireAsset<Material>(HazardMaterialPath),
                Ore = RequireAsset<Material>(OreMaterialPath)
            };
        }

        private static void BuildUpgradeStationPrefab(MapMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_DoubleJumpStation");
            try
            {
                CreateBox("Station Base", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(3f, 0.4f, 2.8f), materials.Frame, true);
                CreateBox("Left Pylon", root.transform, new Vector3(-1.05f, 1.45f, 0.35f), new Vector3(0.38f, 2.5f, 0.5f), materials.Dark, true);
                CreateBox("Right Pylon", root.transform, new Vector3(1.05f, 1.45f, 0.35f), new Vector3(0.38f, 2.5f, 0.5f), materials.Dark, true);
                CreateBox("Pylon Brace", root.transform, new Vector3(0f, 2.55f, 0.35f), new Vector3(2.45f, 0.3f, 0.55f), materials.Frame, false);
                CreateCylinder("Energy Halo A", root.transform, new Vector3(0f, 1.45f, 0.2f), new Vector3(0.95f, 0.06f, 0.95f), Quaternion.Euler(90f, 0f, 0f), materials.Energy, false);
                CreateCylinder("Energy Halo B", root.transform, new Vector3(0f, 1.45f, 0.03f), new Vector3(0.62f, 0.05f, 0.62f), Quaternion.Euler(90f, 0f, 0f), materials.Dark, false);
                CreatePrimitive(PrimitiveType.Sphere, "Upgrade Energy Core", root.transform, new Vector3(0f, 1.45f, -0.03f), Vector3.one * 0.42f, Quaternion.identity, materials.Energy, false);

                GameObject console = CreateBox("Upgrade Console", root.transform, new Vector3(0f, 0.78f, -0.95f), new Vector3(1.5f, 1.15f, 0.72f), materials.Dark, true, Quaternion.Euler(-12f, 0f, 0f));
                Renderer indicator = CreateBox("Upgrade Indicator", console.transform, new Vector3(0f, 0.35f, -0.52f), new Vector3(0.8f, 0.14f, 0.08f), materials.Energy, false).GetComponent<Renderer>();

                DoubleJumpUpgradeStation station = root.AddComponent<DoubleJumpUpgradeStation>();
                station.Configure(indicator);
                root.AddComponent<InteractionTarget>().Configure(station);
                SavePrefab(root, UpgradePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildCranePrefab(MapMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_OverheadCrane");
            try
            {
                CreateBox("Gantry Beam", root.transform, new Vector3(0f, 6f, 0f), new Vector3(12f, 0.48f, 0.55f), materials.Frame, true);
                CreateBox("Gantry Top Rail", root.transform, new Vector3(0f, 6.35f, 0f), new Vector3(11.5f, 0.18f, 0.34f), materials.Steel, false);
                CreateBox("Trolley", root.transform, new Vector3(0f, 5.55f, 0f), new Vector3(1.8f, 0.72f, 1.2f), materials.Dark, true);
                CreateCylinder("Trolley Beacon", root.transform, new Vector3(0f, 5.96f, 0f), new Vector3(0.3f, 0.12f, 0.3f), Quaternion.identity, materials.Hazard, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    CreatePipeBetween("Hoist Cable " + side, root.transform, new Vector3(side * 0.48f, 5.2f, 0f), new Vector3(side * 0.48f, 1.05f, 0f), 0.07f, materials.Steel, false);
                }

                CreateCylinder("Magnet Housing", root.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.1f, 0.34f, 1.1f), Quaternion.identity, materials.Dark, true);
                CreateCylinder("Magnet Energy Ring", root.transform, new Vector3(0f, 0.62f, 0f), new Vector3(0.86f, 0.09f, 0.86f), Quaternion.identity, materials.Energy, false);
                CreateBox("Suspended Cargo Deck", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(3.2f, 0.5f, 3f), materials.Deck, true);
                CreateBox("Cargo Deck Frame", root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(3.5f, 0.2f, 3.3f), materials.Frame, true);
                AddDeckCornerBrackets(root.transform, Vector3.zero, new Vector2(3.2f, 3f), 0.5f, materials);

                SavePrefab(root, CranePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScene(MapMaterials materials)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FactoryVerticalMap";

            ConfigureRenderSettings();

            GameObject levelRoot = new GameObject("Factory Vertical Hall");
            GameObject architecture = CreateGroup("01 Architecture", levelRoot.transform);
            GameObject groundRoute = CreateGroup("02 Ground Route - Normal Jump", levelRoot.transform);
            GameObject middleRoute = CreateGroup("03 Middle Route - Double Jump", levelRoot.transform);
            GameObject upperRoute = CreateGroup("04 Upper Route and Recovery", levelRoot.transform);
            GameObject machinery = CreateGroup("05 Factory Machinery", levelRoot.transform);
            GameObject conveyors = CreateGroup("06 Conveyor Network", levelRoot.transform);
            GameObject objectives = CreateGroup("07 Objectives and Activation", levelRoot.transform);
            GameObject futureEntrances = CreateGroup("08 Enemy Entrances", levelRoot.transform);
            GameObject lighting = CreateGroup("09 Lighting", levelRoot.transform);
            GameObject playerRig = CreateGroup("10 Player Rig", levelRoot.transform);
            GameObject audio = CreateGroup("11 Audio", levelRoot.transform);
            GameObject enemyNavigation = CreateGroup("12 Enemy Navigation", levelRoot.transform);
            GameObject enemySystems = CreateGroup("13 Enemy Systems", levelRoot.transform);
            GameObject turretSpots = CreateGroup("Turret Spots", levelRoot.transform);

            MachineTargetRegistry machineRegistry = CreateGroup(
                "Machine Target Registry",
                enemySystems.transform).AddComponent<MachineTargetRegistry>();
            EnemyRuntimeRegistry enemyRegistry = CreateGroup(
                "Enemy Runtime Registry",
                enemySystems.transform).AddComponent<EnemyRuntimeRegistry>();

            BuildArchitecture(architecture.transform, materials);
            BuildGroundRoute(groundRoute.transform, materials);
            BuildMiddleRoute(middleRoute.transform, materials);
            BuildUpperRoute(upperRoute.transform, materials);
            EnemyAccessRouteSet enemyAccessRoutes = BuildEnemyAccessRoutes(enemyNavigation.transform, materials);
            EnemyEntranceSet entrances = BuildFutureEntrances(futureEntrances.transform, materials);

            GameObject mine = PlacePrefab(MinePrefabPath, new Vector3(-13f, 0f, -11f), Quaternion.Euler(0f, 180f, 0f), machinery.transform, "Mine Extractor");
            GameObject smelter = PlacePrefab(SmelterPrefabPath, new Vector3(-12.02f, 5.2f, -0.6f), Quaternion.identity, machinery.transform, "Smelter");
            GameObject upgradeStation = PlacePrefab(UpgradePrefabPath, new Vector3(-2.92f, 3.74f, 14.34f), Quaternion.Euler(0f, 180f, 0f), objectives.transform, "Double Jump Upgrade Station");
            GameObject crusher = PlacePrefab(CrusherPrefabPath, new Vector3(2.8f, 4.8f, -2.8f), Quaternion.identity, machinery.transform, "Piston Crusher");
            GameObject generator = PlacePrefab(GeneratorPrefabPath, new Vector3(13f, 8f, 0.5f), Quaternion.Euler(0f, -90f, 0f), machinery.transform, "Main Generator");
            GameObject assembler = PlacePrefab(AssemblerPrefabPath, new Vector3(12.5f, 13.2f, 11f), Quaternion.Euler(0f, 90f, 0f), machinery.transform, "Assembler");
            GameObject crane = PlacePrefab(CranePrefabPath, new Vector3(-0.04f, 15.45f, 0.53f), Quaternion.identity, machinery.transform, "Overhead Gantry Crane");
            GameObject portal = PlacePrefab(PortalPrefabPath, new Vector3(0f, 16.4f, 19.2f), Quaternion.Euler(0f, 180f, 0f), machinery.transform, "Factory Exit Portal");

            ProductionConveyorRoute mineToSmelterRoute = CreateProductionConveyorRoute(
                "Mine to Smelter Production Route",
                conveyors.transform,
                new[]
                {
                    new Vector3(-13f, 0.72f, -9.95f),
                    new Vector3(-13f, 5.72f, -8f),
                    new Vector3(-12.02f, 5.72f, -2.05f)
                });
            ProductionConveyorRoute smelterToAssemblerRoute = CreateProductionConveyorRoute(
                "Smelter to Assembler Production Route",
                conveyors.transform,
                new[]
                {
                    new Vector3(-9.42f, 5.72f, 0.2f),
                    new Vector3(-7.7f, 5.72f, 0.2f),
                    new Vector3(-7f, 5.34f, 9.4f),
                    new Vector3(-7f, 13.5f, 10.5f),
                    new Vector3(10.45f, 13.72f, 12.2f)
                });
            ConveyorBelt generatorBelt = CreateConveyor(
                "Generator Feed Conveyor",
                conveyors.transform,
                new Vector3(8.7f, 8.12f, 1.8f),
                new Vector3(11.6f, 8.12f, 0.8f),
                ConveyorOperatingState.Offline);
            generatorBelt.SetGeneratedGeometryEnabled(false);
            ProductionConveyorRoute assemblerToPortalRoute = CreateProductionConveyorRoute(
                "Assembler to Portal Production Route",
                conveyors.transform,
                new[]
                {
                    new Vector3(10.45f, 14.04f, 10.6f),
                    new Vector3(8.4f, 14.04f, 10.6f),
                    new Vector3(7.5f, 16.2f, 15f),
                    new Vector3(2f, 17.05f, 18.25f)
                });

            CreateFreightLift(middleRoute.transform, materials);
            CreateProductionFlowDressing(conveyors.transform, materials);
            CreateOverheadCraneRails(architecture.transform, materials);

            Light mineLight = CreatePointLight("Mine Work Light", lighting.transform, new Vector3(-13f, 4.8f, -10f), new Color(0.95f, 0.43f, 0.12f), 8f, 6f);
            Light smelterLight = CreatePointLight("Smelter Work Light", lighting.transform, new Vector3(-13f, 10.2f, -1f), new Color(1f, 0.28f, 0.06f), 10f, 7f);
            Light generatorLight = CreatePointLight("Generator Work Light", lighting.transform, new Vector3(13f, 12.8f, 0.5f), new Color(0.08f, 0.78f, 1f), 10f, 7f);
            Light assemblerLight = CreatePointLight("Assembler Work Light", lighting.transform, new Vector3(12.5f, 18f, 11f), new Color(0.12f, 0.72f, 1f), 10f, 7f);
            CreateLighting(lighting.transform);
            CreatePostProcessing(lighting.transform);

            GameObject minePowered = FindDescendant(mine, "Drill Assembly");
            GameObject smelterPowered = FindDescendant(smelter, "Smoke Plume");
            GameObject generatorPowered = FindDescendant(generator, "Energy Assembly");
            GameObject assemblerPowered = FindDescendant(assembler, "Status Light 2");

            FactoryObjectiveTerminal mineTerminal = CreateObjectiveTerminal(
                objectives.transform,
                "Mine Activation Terminal",
                "Mine",
                new Vector3(-9.8f, 0f, -10.2f),
                Quaternion.Euler(0f, -90f, 0f),
                null,
                CompactObjects(minePowered),
                new[] { mineLight },
                Array.Empty<ConveyorBelt>(),
                materials);
            FactoryObjectiveTerminal smelterTerminal = CreateObjectiveTerminal(
                objectives.transform,
                "Smelter Activation Terminal",
                "Smelter",
                new Vector3(-9.3f, 5.2f, -3.6f),
                Quaternion.Euler(0f, 90f, 0f),
                mineTerminal,
                CompactObjects(smelterPowered),
                new[] { smelterLight },
                Array.Empty<ConveyorBelt>(),
                materials);
            FactoryObjectiveTerminal generatorTerminal = CreateObjectiveTerminal(
                objectives.transform,
                "Generator Activation Terminal",
                "Generator",
                new Vector3(9.1f, 8f, -0.5f),
                Quaternion.Euler(0f, -90f, 0f),
                smelterTerminal,
                CompactObjects(generatorPowered),
                new[] { generatorLight },
                new[] { generatorBelt },
                materials);
            FactoryObjectiveTerminal assemblerTerminal = CreateObjectiveTerminal(
                objectives.transform,
                "Assembler Activation Terminal",
                "Assembler",
                new Vector3(15.55f, 13.2f, 13.55f),
                Quaternion.identity,
                generatorTerminal,
                CompactObjects(assemblerPowered),
                new[] { assemblerLight },
                Array.Empty<ConveyorBelt>(),
                materials);

            BindMachineToTerminal(mine, mineTerminal, machineRegistry);
            BindMachineToTerminal(smelter, smelterTerminal, machineRegistry);
            BindMachineToTerminal(generator, generatorTerminal, machineRegistry);
            BindMachineToTerminal(assembler, assemblerTerminal, machineRegistry);

            FactoryConveyorConnection mineToSmelter = ConfigureProductionConveyorRoute(
                mineToSmelterRoute,
                "Mine → Smelter Conveyor",
                mineTerminal,
                smelterTerminal,
                materials,
                new Vector3(-11.7f, 0.219f, -8.25f),
                new Vector3(-13.2f, 5.42f, -3.2f));
            FactoryConveyorConnection smelterToAssembler = ConfigureProductionConveyorRoute(
                smelterToAssemblerRoute,
                "Smelter → Assembler Conveyor",
                smelterTerminal,
                assemblerTerminal,
                materials,
                new Vector3(-8.92f, 5.42f, -0.487f),
                new Vector3(9.25f, 13.58f, 12.2f));
            FactoryConveyorConnection assemblerToPortal = ConfigureProductionConveyorRoute(
                assemblerToPortalRoute,
                "Assembler → Portal Conveyor",
                assemblerTerminal,
                null,
                materials,
                new Vector3(9.55f, 13.62f, 9.6f),
                new Vector3(3.15f, 16.36f, 17.47f));

            FactoryGantryCraneMover craneMover = crane.AddComponent<FactoryGantryCraneMover>();
            craneMover.Configure(
                crane.transform,
                generatorTerminal,
                new Vector3(-0.04f, 15.45f, -10.5f),
                new Vector3(-0.04f, 15.45f, 9.5f),
                1.8f,
                0.75f);

            FactoryPortalGate portalGate = BuildPortalObjectives(objectives.transform, portal, materials);
            BuildProductionLine(
                conveyors.transform,
                mineTerminal,
                smelterTerminal,
                generatorTerminal,
                assemblerTerminal,
                mineToSmelter,
                smelterToAssembler,
                assemblerToPortal,
                portalGate);
            PlayerRigSet player = BuildPlayerRig(playerRig.transform);
            FactoryHudPresenter factoryHud = player.Hud.AddComponent<FactoryHudPresenter>();
            factoryHud.Configure(
                player.Hud.GetComponent<UIDocument>(),
                AssetDatabase.LoadAssetAtPath<StyleSheet>(HudStylePath),
                player.Hud.GetComponent<InteractionPromptPresenter>(),
                new[] { mineTerminal, smelterTerminal, generatorTerminal, assemblerTerminal },
                upgradeStation.GetComponent<DoubleJumpUpgradeStation>(),
                portalGate);
            ConfigureMachineBreakPresentation(mine, player.CameraShake);
            ConfigureMachineBreakPresentation(smelter, player.CameraShake);
            ConfigureMachineBreakPresentation(generator, player.CameraShake);
            ConfigureMachineBreakPresentation(assembler, player.CameraShake);
            enemyAccessRoutes.Configure(
                smelterTerminal,
                generatorTerminal,
                assemblerTerminal);
            EnemySpawnManager spawnManager = BuildEnemySpawnManager(
                enemySystems.transform,
                entrances,
                player.Targetable,
                machineRegistry,
                enemyRegistry,
                smelterTerminal,
                assemblerTerminal,
                player.CameraShake);
            BuildPortalCompletionFlow(portalGate, portal, player, spawnManager, objectives.transform);
            FactorySceneEntryController sceneEntry = player.Hud.AddComponent<FactorySceneEntryController>();
            sceneEntry.Configure(
                player.PlayerController,
                player.PlayerInteractor,
                player.OrbitCamera,
                player.Targetable,
                player.StatusPresenter,
                factoryHud,
                player.Hud.GetComponent<FactoryPauseController>(),
                spawnManager);
            BuildTurretSpots(turretSpots.transform, machineRegistry, enemyRegistry);
            BuildAudio(audio);
            BuildAndPersistEnemyNavMesh(enemyNavigation);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void BuildTurretSpots(
            Transform parent,
            MachineTargetRegistry machineRegistry,
            EnemyRuntimeRegistry enemyRegistry)
        {
            Vector3[] positions =
            {
                new Vector3(2.829f, 9.34f, -2.865f),
                new Vector3(-13.12f, 9.04f, 7.045f),
                new Vector3(6.165f, 15.54f, 12.73f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject spotObject = PlacePrefab(
                    FactoryDefenseAssetFactory.BuildSpotPrefabPath,
                    positions[index],
                    Quaternion.identity,
                    parent,
                    "TurretSpot" + (index == 0 ? string.Empty : " (" + index + ")"));
                TurretBuildSpot spot = spotObject.GetComponent<TurretBuildSpot>();
                if (spot == null)
                {
                    throw new InvalidOperationException("Factory turret build-spot prefab is missing TurretBuildSpot.");
                }

                spot.AssignRegistries(enemyRegistry, machineRegistry);
            }
        }

        private static void BuildAudio(GameObject audioRoot)
        {
            AudioMixer mixer = RequireAsset<AudioMixer>(GameAudioAssetFactory.MixerPath);
            AudioMixerGroup musicGroup = mixer.FindMatchingGroups(GameAudioAssetFactory.MusicGroupName).First();
            AudioMixerGroup sfxGroup = mixer.FindMatchingGroups(GameAudioAssetFactory.SfxGroupName).First();
            ContinuousMusicPlayer musicPlayer = audioRoot.AddComponent<ContinuousMusicPlayer>();
            musicPlayer.Configure(
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<AudioClip>(MusicTrackOnePath),
                    AssetDatabase.LoadAssetAtPath<AudioClip>(MusicTrackTwoPath),
                    AssetDatabase.LoadAssetAtPath<AudioClip>(MusicTrackThreePath)
                },
                0.22f,
                5f,
                musicGroup);

            CreateAmbientLoop(
                "Mine Machinery Ambience",
                audioRoot.transform,
                RequireAsset<AudioClip>(MinerAudioPath),
                new Vector3(-13f, 1.4f, -11f),
                0.11f,
                3f,
                15f,
                sfxGroup);
            CreateAmbientLoop(
                "Smelter Fire Ambience",
                audioRoot.transform,
                RequireAsset<AudioClip>(SmelterAmbienceAudioPath),
                new Vector3(-12.02f, 6.4f, -0.6f),
                0.095f,
                2.5f,
                14f,
                sfxGroup);
            CreateAmbientLoop(
                "Crusher Mechanism Ambience",
                audioRoot.transform,
                RequireAsset<AudioClip>(CrusherAmbienceAudioPath),
                new Vector3(2.8f, 6.2f, -2.8f),
                0.08f,
                2.5f,
                13f,
                sfxGroup);

            AudioReverbZone reverbZone = CreateGroup(
                "Factory Hall Reverb",
                audioRoot.transform).AddComponent<AudioReverbZone>();
            reverbZone.transform.position = new Vector3(0f, 10f, 0f);
            reverbZone.reverbPreset = AudioReverbPreset.Hangar;
            reverbZone.minDistance = 30f;
            reverbZone.maxDistance = 45f;
        }

        private static AudioSource CreateAmbientLoop(
            string name,
            Transform parent,
            AudioClip clip,
            Vector3 position,
            float volume,
            float minimumDistance,
            float maximumDistance,
            AudioMixerGroup outputGroup)
        {
            GameObject sourceObject = CreateGroup(name, parent);
            sourceObject.transform.position = position;
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = 0.9f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = Mathf.Max(0.1f, minimumDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maximumDistance);
            source.reverbZoneMix = 0.32f;
            source.priority = 180;
            source.outputAudioMixerGroup = outputGroup;
            return source;
        }

        private static void BuildArchitecture(Transform parent, MapMaterials materials)
        {
            CreateBox("Factory Floor", parent, new Vector3(0f, -0.5f, 0f), new Vector3(42f, 1f, 42f), materials.Floor, true);
            CreateBox("North Wall", parent, new Vector3(0f, 12f, 20.5f), new Vector3(42f, 24f, 1f), materials.Wall, true);
            CreateBox("South Wall", parent, new Vector3(0f, 12f, -20.5f), new Vector3(42f, 24f, 1f), materials.Wall, true);
            CreateSideWallWithLowerDoorOpening("West Wall", parent, -20.5f, -12f, materials);
            CreateSideWallWithLowerDoorOpening("East Wall", parent, 20.5f, -8f, materials);

            float[] columnZ = { -16f, -8f, 0f, 8f, 16f };
            foreach (float z in columnZ)
            {
                CreateBox("West Structural Column", parent, new Vector3(-18.5f, 11.8f, z), new Vector3(0.75f, 23.6f, 0.75f), materials.Frame, true);
                float eastColumnZ = Mathf.Approximately(z, -8f) ? -5.25f : z;
                CreateBox("East Structural Column", parent, new Vector3(18.5f, 11.8f, eastColumnZ), new Vector3(0.75f, 23.6f, 0.75f), materials.Frame, true);
                CreateBox("Ceiling Cross Truss", parent, new Vector3(0f, 22.2f, z), new Vector3(37f, 0.38f, 0.5f), materials.Steel, true);
                CreateBox("Ceiling Truss Orange Strip", parent, new Vector3(0f, 21.92f, z), new Vector3(37f, 0.12f, 0.62f), materials.Frame, false);
            }

            for (int index = -2; index <= 2; index++)
            {
                float x = index * 7.2f;
                CreateBox("Longitudinal Roof Beam " + (index + 3), parent, new Vector3(x, 22.55f, 0f), new Vector3(0.45f, 0.45f, 39f), materials.Dark, true);
            }

            CreateBox("Spawn Safety Stripe Left", parent, new Vector3(-3.2f, 0.02f, -17f), new Vector3(0.22f, 0.04f, 5f), materials.Hazard, false);
            CreateBox("Spawn Safety Stripe Right", parent, new Vector3(3.2f, 0.02f, -17f), new Vector3(0.22f, 0.04f, 5f), materials.Hazard, false);
            CreateBox("Central Sight Shaft Floor Marking", parent, new Vector3(0f, 0.015f, 2f), new Vector3(7.5f, 0.03f, 24f), materials.Dark, false);
        }

        private static void CreateSideWallWithLowerDoorOpening(
            string name,
            Transform parent,
            float x,
            float doorwayZ,
            MapMaterials materials)
        {
            const float wallMinimumZ = -21f;
            const float wallMaximumZ = 21f;
            const float doorwayWidth = 3.8f;
            const float doorwayHeight = 3.8f;
            const float wallHeight = 24f;

            float openingMinimumZ = doorwayZ - doorwayWidth * 0.5f;
            float openingMaximumZ = doorwayZ + doorwayWidth * 0.5f;
            float southLength = openingMinimumZ - wallMinimumZ;
            float northLength = wallMaximumZ - openingMaximumZ;

            CreateBox(
                name + " South Segment",
                parent,
                new Vector3(x, wallHeight * 0.5f, wallMinimumZ + southLength * 0.5f),
                new Vector3(1f, wallHeight, southLength),
                materials.Wall,
                true);
            CreateBox(
                name + " North Segment",
                parent,
                new Vector3(x, wallHeight * 0.5f, openingMaximumZ + northLength * 0.5f),
                new Vector3(1f, wallHeight, northLength),
                materials.Wall,
                true);

            float headerHeight = wallHeight - doorwayHeight;
            CreateBox(
                name + " Door Header",
                parent,
                new Vector3(x, doorwayHeight + headerHeight * 0.5f, doorwayZ),
                new Vector3(1f, headerHeight, doorwayWidth),
                materials.Wall,
                true);
        }

        private static void BuildGroundRoute(Transform parent, MapMaterials materials)
        {
            CreateIndustrialDeck("Spawn Checkpoint Apron", parent, new Vector3(0f, -17f), new Vector2(7f, 4.5f), 0.2f, 0f, materials, false, false);
            CreateIndustrialDeck("Mine Apron", parent, new Vector3(-13f, -10.1f), new Vector2(8f, 6.8f), 0.2f, 0f, materials, true, false);

            CreateMachineHousingPlatform(
                "Pump Skid",
                parent,
                new Vector3(-16.28f, -3.35f),
                new Vector2(2.7f, 2.5f),
                1.03f,
                materials,
                -0.69f,
                0.144f);
            CreatePipeRackPlatform(
                parent,
                new Vector3(-15.3f, -0.95f),
                new Vector2(3.2f, 2.1f),
                2.86f,
                materials,
                new Vector3(-15.3f, 2.826f, -0.987f),
                new Vector3(-0.881f, -0.49f, 0f));
            CreateMachineHousingPlatform(
                "Valve Housing",
                parent,
                new Vector3(-16.31f, 1.35f),
                new Vector2(2.8f, 2.2f),
                3.98f,
                materials,
                trimYOffset: 0.15f);
            GameObject smelterLanding = CreateIndustrialDeck("Smelter Landing", parent, new Vector3(-15.5f, 3.85f), new Vector2(3.2f, 2.7f), 5.16f, 0f, materials, true, false);
            smelterLanding.transform.localPosition = new Vector3(-0.93f, 0f, 0f);

            CreateWestSmelterMezzanine(parent, materials);
            AddSafetyRail(parent, new Vector3(-13f, 6.0f, -4.75f), 8f, true, materials);

            CreateIndustrialDeck("Router Catwalk", parent, new Vector3(-7.2f, 1.4f), new Vector2(3.8f, 2.4f), 5.2f, 0f, materials, false, false);
            HoverDeckBuildData doubleJumpStation = CreateHoverIndustrialDeck(
                "Double Jump Station Balcony",
                parent,
                new Vector3(-5.5f, 5.1f),
                new Vector2(3.4f, 3.4f),
                5.2f,
                materials,
                0.08f);
            AddSafetyRail(
                doubleJumpStation.VisualRoot,
                new Vector3(-5.5f, 6f, 6.65f),
                3f,
                true,
                materials);
        }

        private static void CreateWestSmelterMezzanine(Transform parent, MapMaterials materials)
        {
            GameObject mezzanine = CreateGroup("West Smelter Mezzanine", parent);

            // Keep the smelter deck intact while leaving the west-side step route open from
            // the pipe rack, across the valve housing, and onto the smelter landing.
            GameObject mainDeck = CreateIndustrialDeck(
                "Main Deck",
                mezzanine.transform,
                new Vector3(-11.45f, -0.5f),
                new Vector2(6.9f, 8.8f),
                5.2f,
                0f,
                materials,
                true,
                false);
            RemoveSupportPostAt(mainDeck.transform, new Vector3(-14.62f, 2.375f, -4.62f));
            GameObject southwestAccessWing = CreateIndustrialDeck(
                "Southwest Access Wing",
                mezzanine.transform,
                new Vector3(-16.45f, -3.55f),
                new Vector2(3.1f, 2.7f),
                5.2f,
                0f,
                materials,
                true,
                false);
            RemoveSupportPostAt(southwestAccessWing.transform, new Vector3(-15.18f, 2.375f, -4.62f));
            RemoveSupportPostAt(southwestAccessWing.transform, new Vector3(-15.18f, 2.375f, -2.48f));
            RemoveSupportPostAt(southwestAccessWing.transform, new Vector3(-17.72f, 2.375f, -2.48f));
        }

        private static void BuildMiddleRoute(Transform parent, MapMaterials materials)
        {
            CreateHoverIndustrialDeck(
                "Double Jump Gate - Freight Lift Roof",
                parent,
                new Vector3(-1.7f, 5.1f),
                new Vector2(2.6f, 3f),
                7.55f,
                materials,
                0.41f);
            HoverDeckBuildData crusherServiceLedge = CreateHoverIndustrialDeck(
                "Crusher Service Ledge",
                parent,
                new Vector3(4.45f, 3f),
                new Vector2(2.9f, 2.5f),
                8f,
                materials,
                0.74f);
            crusherServiceLedge.Root.transform.localPosition = new Vector3(-1.56f, 0f, 1.23f);
            CreateIndustrialDeck("Generator Belt Catwalk", parent, new Vector3(7.7f, 1.8f), new Vector2(3.3f, 2.4f), 8f, 4.8f, materials, false, false);
            GameObject crusherMachineDeck = CreateIndustrialDeck("Crusher Machine Deck", parent, new Vector3(2.8f, -2.8f), new Vector2(7.2f, 6.3f), 4.8f, 0f, materials, false, false);
            CreateSteppedCentralLoadColumn(crusherMachineDeck.transform, new Vector2(2.8f, -2.8f), new Vector2(7.2f, 6.3f), 4.8f, materials);
            AddSafetyRail(parent, new Vector3(2.8f, 5.6f, -5.8f), 6.4f, true, materials);

            GameObject eastGeneratorOverlook = CreateIndustrialDeck("East Generator Overlook", parent, new Vector3(13f, 0.5f), new Vector2(10f, 9f), 8f, 0f, materials, false, false);
            CreateTwinGantrySupports(eastGeneratorOverlook.transform, new Vector2(13f, 0.5f), new Vector2(10f, 9f), 8f, materials);
            AddSafetyRail(parent, new Vector3(17.8f, 8.8f, 0.5f), 8f, false, materials);
            AddSafetyRail(parent, new Vector3(13f, 8.8f, -3.8f), 8.8f, true, materials);

            CreateIndustrialDeck("Assembler Feeder Conveyor Housing", parent, new Vector3(11.5f, 9f), new Vector2(3f, 2.3f), 13.2f, 8f, materials, false, false);

            GameObject northeastDeck = CreateIndustrialDeck("Northeast Assembler Deck", parent, new Vector3(12.5f, 11.5f), new Vector2(9f, 8f), 13.2f, 8f, materials, true, false);
            AddSupportPost("Support Post (1)", northeastDeck.transform, new Vector3(8.28f, 5.76f, 15.22f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (2)", northeastDeck.transform, new Vector3(8.28f, 1.22f, 15.22f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (3)", northeastDeck.transform, new Vector3(8.28f, 5.73f, 7.78f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (4)", northeastDeck.transform, new Vector3(16.72f, 5.73f, 7.78f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (5)", northeastDeck.transform, new Vector3(8.28f, 1.1f, 7.78f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (6)", northeastDeck.transform, new Vector3(16.72f, 1.1f, 7.78f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (7)", northeastDeck.transform, new Vector3(16.72f, 5.75f, 15.22f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSupportPost("Support Post (8)", northeastDeck.transform, new Vector3(16.72f, 2.03f, 15.22f), new Vector3(0.32f, 4.75f, 0.32f), materials);
            AddSafetyRail(parent, new Vector3(16.8f, 14f, 11.5f), 7.5f, false, materials);
            AddSafetyRail(parent, new Vector3(12.5f, 14f, 15.3f), 8.5f, true, materials);
        }

        private static void BuildUpperRoute(Transform parent, MapMaterials materials)
        {
            GameObject westGantry = CreateIndustrialDeck("West Gantry", parent, new Vector3(-5f, 13f), new Vector2(9f, 2.2f), 15.5f, 9f, materials, false, false);
            westGantry.transform.position = new Vector3(-7.89f, 0f, 0f);
            GameObject northCatwalk = CreateIndustrialDeck("North Perimeter Catwalk", parent, new Vector3(0f, 12f), new Vector2(22f, 2f), 15.5f, 9f, materials, true, false);
            RemoveSupportPostAt(northCatwalk.transform, new Vector3(10.72f, 12.025f, 11.28f));
            RemoveSupportPostAt(northCatwalk.transform, new Vector3(10.72f, 12.025f, 12.72f));
            AddSupportPost("Support Post (5)", northCatwalk.transform, new Vector3(-10.72f, 6.14f, 11.28f), new Vector3(0.32f, 6.05f, 0.32f), materials);
            AddSupportPost("Support Post (6)", northCatwalk.transform, new Vector3(-10.72f, 6.14f, 12.72f), new Vector3(0.32f, 6.05f, 0.32f), materials);
            AddSupportPost("Support Post (9)", northCatwalk.transform, new Vector3(-10.72f, 0.19f, 11.28f), new Vector3(0.32f, 6.05f, 0.32f), materials);
            AddSupportPost("Support Post (10)", northCatwalk.transform, new Vector3(-10.72f, 0.19f, 12.72f), new Vector3(0.32f, 6.05f, 0.32f), materials);
            northCatwalk.transform.position = new Vector3(-7.89f, 0f, 0f);
            GameObject eastCoreBalcony = CreateIndustrialDeck("East Core Balcony", parent, new Vector3(13.2f, 14f), new Vector2(5.6f, 3.5f), 15.5f, 9f, materials, true, false);
            RemoveSupportPostAt(eastCoreBalcony.transform, new Vector3(10.68f, 12.025f, 12.53f));
            AddSupportTier(eastCoreBalcony.transform, 6.14f, 6.05f, 8, new[] { 10.68f }, new[] { 15.47f }, materials);
            AddSupportTier(eastCoreBalcony.transform, 6.14f, 6.05f, 9, new[] { 15.72f }, new[] { 12.53f, 15.47f }, materials);
            AddSupportTier(eastCoreBalcony.transform, 0.19f, 6.05f, 11, new[] { 10.68f }, new[] { 15.47f }, materials);
            AddSupportTier(eastCoreBalcony.transform, 0.19f, 6.05f, 12, new[] { 15.72f }, new[] { 12.53f, 15.47f }, materials);
            eastCoreBalcony.transform.position = new Vector3(-7.89f, 0f, -1.25f);
            GameObject portalDeck = CreateIndustrialDeck("Portal Deck", parent, new Vector3(0f, 18.4f), new Vector2(10f, 4f), 16.4f, 9f, materials, true, false);
            AddSupportTier(portalDeck.transform, 5.6f, 6.95f, 1, new[] { -4.72f, 4.72f }, new[] { 16.68f, 20.12f }, materials);
            AddSupportTier(portalDeck.transform, -1.14f, 6.95f, 5, new[] { -4.72f, 4.72f }, new[] { 16.68f, 20.12f }, materials);

            AddSafetyRail(parent, new Vector3(5.31f, 16.3f, 15.6f), 5f, true, materials);
            AddSafetyRail(parent, new Vector3(-4.8f, 17.2f, 18.4f), 3.5f, false, materials);
            AddSafetyRail(parent, new Vector3(4.8f, 17.2f, 18.4f), 3.5f, false, materials);

            GameObject centralFallRecovery = CreateIndustrialDeck("Central Fall Recovery Deck", parent, new Vector3(0f, 9f), new Vector2(10f, 6f), 3.8f, 0f, materials, false, false);
            centralFallRecovery.transform.position = new Vector3(-2.69f, 0f, 4.56f);
            CreateSteppedCentralLoadColumn(centralFallRecovery.transform, new Vector2(0f, 9f), new Vector2(10f, 6f), 3.8f, materials);
            GameObject westFallRecovery = CreateIndustrialDeck("West Fall Recovery Balcony", parent, new Vector3(-11f, 9f), new Vector2(8f, 5f), 9f, 3.8f, materials, false, false);
            westFallRecovery.transform.position = new Vector3(-3.04f, 0f, -0.82f);
            CreateTwinCantileverSupports(westFallRecovery.transform, new Vector2(-11f, 9f), new Vector2(8f, 5f), 9f, materials);
            GameObject eastFallRecovery = CreateIndustrialDeck("East Fall Recovery Balcony", parent, new Vector3(11f, 9f), new Vector2(8f, 5f), 9f, 3.8f, materials, false, false);
            eastFallRecovery.transform.position = new Vector3(0f, -4.04f, 0f);
            CreateSteppedCentralLoadColumn(eastFallRecovery.transform, new Vector2(11f, 9f), new Vector2(8f, 5f), 9f, materials);
            AddSafetyRail(parent, new Vector3(-13.70f, 9.80f, 10.38f), 7f, true, materials);
        }

        private static EnemyAccessRouteSet BuildEnemyAccessRoutes(Transform parent, MapMaterials materials)
        {
            EnemyAccessRouteBuildData smelterRoute = CreateEnemyAccessRoute(
                "Smelter Enemy Access Route",
                parent);
            CreateEnemyLandingPad(
                "Ground Ladder Landing",
                smelterRoute.Root,
                new Vector3(-11.45f, 0.08f, -5.75f),
                new Vector3(3.4f, 0.24f, 1.5f),
                materials);
            smelterRoute.Add(CreateDeployableEnemyLadder(
                "Ground to Smelter Ladder",
                smelterRoute.Root,
                new Vector3(-11.45f, 0.2f, -5.75f),
                new Vector3(-11.45f, 5.2f, -4.55f),
                Vector3.forward,
                materials));

            EnemyAccessRouteBuildData generatorRoute = CreateEnemyAccessRoute(
                "Generator Enemy Access Route",
                parent);
            CreateEnemyLandingPad(
                "Router Ladder Lower Landing",
                generatorRoute.Root,
                new Vector3(-3.5f, 5.08f, 5.1f),
                new Vector3(1.6f, 0.24f, 3f),
                materials);
            CreateEnemyLandingPad(
                "Router Ladder Upper Landing",
                generatorRoute.Root,
                new Vector3(-2.6f, 7.43f, 5.1f),
                new Vector3(1.2f, 0.24f, 3f),
                materials);
            generatorRoute.Add(CreateDeployableEnemyLadder(
                "Smelter to Generator Ladder",
                generatorRoute.Root,
                new Vector3(-3.45f, 5.2f, 5.1f),
                new Vector3(-2.65f, 7.55f, 5.1f),
                Vector3.right,
                materials));
            generatorRoute.Add(CreateEnemyJumpLink(
                "Smelter Deck to Router Link",
                generatorRoute.Root,
                new Vector3(-9.667f, 5.718f, -1f),
                new Vector3(-8.4f, 5.234f, 2.17f),
                0f,
                1f));
            generatorRoute.Add(CreateEnemyJumpLink(
                "Router to Double Jump Station Link",
                generatorRoute.Root,
                new Vector3(-5.83f, 5.22f, 2.17f),
                new Vector3(-5.5f, 5.22f, 3.83f),
                0f,
                0.65f));
            generatorRoute.Add(CreateEnemyJumpLink(
                "Freight Roof to Crusher Ledge Link",
                generatorRoute.Root,
                new Vector3(-0.83f, 7.55f, 4.8f),
                new Vector3(2f, 8.05f, 4.8f),
                0f,
                1.1f));
            generatorRoute.Add(CreateEnemyJumpLink(
                "Crusher Ledge to Generator Catwalk Link",
                generatorRoute.Root,
                new Vector3(3.83f, 8.05f, 3.5f),
                new Vector3(6.5f, 8.05f, 2.5f),
                0f,
                0.8f));
            generatorRoute.Add(CreateEnemyJumpLink(
                "Generator Deck to Crusher Turret Link",
                generatorRoute.Root,
                new Vector3(8.5f, 8.05f, -2.85f),
                new Vector3(4.5f, 9.39f, -2.85f),
                0f,
                1.2f));
            CreateEnemyLandingPad(
                "West Turret Ladder Lower Landing",
                generatorRoute.Root,
                new Vector3(-13.12f, 5.08f, 4.8f),
                new Vector3(3.2f, 0.24f, 1.6f),
                materials);
            CreateEnemyLandingPad(
                "West Turret Ladder Upper Landing",
                generatorRoute.Root,
                new Vector3(-13.12f, 8.88f, 5.55f),
                new Vector3(3.2f, 0.24f, 1.6f),
                materials);
            generatorRoute.Add(CreateDeployableEnemyLadder(
                "Smelter to West Turret Ladder",
                generatorRoute.Root,
                new Vector3(-13.12f, 5.2f, 4.65f),
                new Vector3(-13.12f, 9f, 5.95f),
                Vector3.forward,
                materials));

            EnemyAccessRouteBuildData assemblerRoute = CreateEnemyAccessRoute(
                "Assembler Enemy Access Route",
                parent);
            CreateEnemyLandingPad(
                "Freight Shaft Ladder Lower Landing",
                assemblerRoute.Root,
                new Vector3(13.2f, 7.88f, 5.7f),
                new Vector3(3f, 0.24f, 1.4f),
                materials);
            CreateEnemyLandingPad(
                "Freight Shaft Ladder Upper Landing",
                assemblerRoute.Root,
                new Vector3(13.2f, 13.08f, 6.85f),
                new Vector3(3f, 0.24f, 1.4f),
                materials);
            assemblerRoute.Add(CreateDeployableEnemyLadder(
                "Generator to Assembler Ladder",
                assemblerRoute.Root,
                new Vector3(13.2f, 8f, 5.8f),
                new Vector3(13.2f, 13.2f, 6.75f),
                Vector3.forward,
                materials));
            assemblerRoute.Add(CreateEnemyJumpLink(
                "Assembler Deck to East Turret Link",
                assemblerRoute.Root,
                new Vector3(8.83f, 13.22f, 13f),
                new Vector3(7.67f, 15.55f, 12.73f),
                0f,
                1.2f));

            return new EnemyAccessRouteSet(smelterRoute, generatorRoute, assemblerRoute);
        }

        private static EnemyAccessRouteBuildData CreateEnemyAccessRoute(string name, Transform parent)
        {
            GameObject root = CreateGroup(name, parent);
            return new EnemyAccessRouteBuildData(
                root.transform,
                root.AddComponent<EnemyAccessRoute>());
        }

        private static EnemyLadderBuildData CreateDeployableEnemyLadder(
            string name,
            Transform parent,
            Vector3 bottomPoint,
            Vector3 topPoint,
            Vector3 facingDirection,
            MapMaterials materials)
        {
            const float outerWidth = 2.9f;
            const float linkWidth = 2.6f;
            const float rungSpacing = 0.42f;
            const float railOffset = 1.36f;

            Vector3 ladderDelta = topPoint - bottomPoint;
            float ladderLength = ladderDelta.magnitude;
            if (ladderLength <= 0.1f)
            {
                throw new InvalidOperationException(name + " requires separated endpoints.");
            }

            Vector3 localUp = ladderDelta.normalized;
            Vector3 requestedForward = Vector3.ProjectOnPlane(facingDirection, localUp).normalized;
            if (requestedForward.sqrMagnitude <= 0.001f)
            {
                requestedForward = Vector3.ProjectOnPlane(Vector3.forward, localUp).normalized;
            }

            GameObject ladderRoot = CreateGroup(name, parent);
            ladderRoot.transform.SetPositionAndRotation(
                bottomPoint,
                Quaternion.LookRotation(requestedForward, localUp));

            NavMeshLink link = ladderRoot.AddComponent<NavMeshLink>();
            link.agentTypeID = 0;
            link.startPoint = Vector3.zero;
            link.endPoint = Vector3.up * ladderLength;
            link.width = linkWidth;
            link.bidirectional = true;
            link.costModifier = -1f;

            EnemyTraversalLink traversal = ladderRoot.AddComponent<EnemyTraversalLink>();
            traversal.Configure(link, EnemyTraversalKind.Ladder, Vector3.forward, 0f, 4.5f, 0.18f, 0.18f);

            CreateBox(
                "Upper Deployment Housing",
                ladderRoot.transform,
                new Vector3(0f, ladderLength + 0.12f, 0f),
                new Vector3(outerWidth, 0.28f, 0.38f),
                materials.Dark,
                false);
            CreateBox(
                "Left Hostile Indicator",
                ladderRoot.transform,
                new Vector3(-1.16f, ladderLength + 0.13f, -0.21f),
                new Vector3(0.22f, 0.14f, 0.08f),
                materials.Broken,
                false);
            CreateBox(
                "Right Hostile Indicator",
                ladderRoot.transform,
                new Vector3(1.16f, ladderLength + 0.13f, -0.21f),
                new Vector3(0.22f, 0.14f, 0.08f),
                materials.Broken,
                false);

            List<Transform> parts = new List<Transform>();
            List<Vector3> retractedPositions = new List<Vector3>();
            AddDeployingLadderPart(
                CreateBox(
                    "Left Rail",
                    ladderRoot.transform,
                    new Vector3(-railOffset, ladderLength * 0.5f, 0f),
                    new Vector3(0.17f, ladderLength, 0.17f),
                    materials.Steel,
                    false).transform,
                ladderLength,
                parts,
                retractedPositions);
            AddDeployingLadderPart(
                CreateBox(
                    "Right Rail",
                    ladderRoot.transform,
                    new Vector3(railOffset, ladderLength * 0.5f, 0f),
                    new Vector3(0.17f, ladderLength, 0.17f),
                    materials.Steel,
                    false).transform,
                ladderLength,
                parts,
                retractedPositions);
            AddDeployingLadderPart(
                CreateBox(
                    "Left Hazard Trim",
                    ladderRoot.transform,
                    new Vector3(-1.46f, ladderLength * 0.5f, 0.02f),
                    new Vector3(0.08f, ladderLength, 0.21f),
                    materials.Hazard,
                    false).transform,
                ladderLength,
                parts,
                retractedPositions);
            AddDeployingLadderPart(
                CreateBox(
                    "Right Hazard Trim",
                    ladderRoot.transform,
                    new Vector3(1.46f, ladderLength * 0.5f, 0.02f),
                    new Vector3(0.08f, ladderLength, 0.21f),
                    materials.Hazard,
                    false).transform,
                ladderLength,
                parts,
                retractedPositions);

            int rungCount = Mathf.Max(2, Mathf.FloorToInt(ladderLength / rungSpacing));
            for (int index = 0; index <= rungCount; index++)
            {
                float height = Mathf.Min(ladderLength, index * ladderLength / rungCount);
                AddDeployingLadderPart(
                    CreateBox(
                        "Rung " + (index + 1),
                        ladderRoot.transform,
                        new Vector3(0f, height, 0f),
                        new Vector3(linkWidth, 0.1f, 0.14f),
                        materials.Frame,
                        false).transform,
                    ladderLength,
                    parts,
                    retractedPositions);
            }

            return new EnemyLadderBuildData(
                traversal,
                parts.ToArray(),
                retractedPositions.ToArray());
        }

        private static void AddDeployingLadderPart(
            Transform part,
            float retractedHeight,
            ICollection<Transform> parts,
            ICollection<Vector3> retractedPositions)
        {
            parts.Add(part);
            Vector3 retractedPosition = part.localPosition;
            retractedPosition.y = retractedHeight;
            retractedPositions.Add(retractedPosition);
        }

        private static EnemyTraversalLink CreateEnemyJumpLink(
            string name,
            Transform parent,
            Vector3 startPoint,
            Vector3 endPoint,
            float width,
            float arcHeight)
        {
            GameObject linkObject = CreateGroup(name, parent);
            NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
            link.agentTypeID = 0;
            link.startPoint = startPoint;
            link.endPoint = endPoint;
            link.width = Mathf.Max(0f, width);
            link.bidirectional = true;
            link.costModifier = -1f;

            EnemyTraversalLink traversal = linkObject.AddComponent<EnemyTraversalLink>();
            Vector3 facing = Vector3.ProjectOnPlane(endPoint - startPoint, Vector3.up).normalized;
            traversal.Configure(link, EnemyTraversalKind.Jump, facing, arcHeight, 4.5f, 0.18f, 0.18f);
            return traversal;
        }

        private static void CreateEnemyLandingPad(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            MapMaterials materials)
        {
            GameObject root = CreateGroup(name, parent);
            CreateBox("Steel Landing", root.transform, position, scale, materials.Deck, true);
            CreateBox(
                "Hazard Perimeter",
                root.transform,
                position + Vector3.down * (scale.y * 0.55f),
                new Vector3(scale.x + 0.14f, 0.08f, scale.z + 0.14f),
                materials.Hazard,
                false);
        }

        private static EnemyEntranceSet BuildFutureEntrances(Transform parent, MapMaterials materials)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                throw new InvalidOperationException(
                    "The Enemy layer was not created. Build enemy assets before rebuilding the map.");
            }

            GameObject mineDoor = CreateMaintenanceDoor(
                "Future Enemy Entrance - Mine Service Door",
                parent,
                new Vector3(-19.92f, 1.55f, -12f),
                Quaternion.Euler(0f, 90f, 0f),
                materials,
                true,
                new MaintenanceDoorLayout(1.0972f, 0.25f, 2.06f, 0.16f, 0.00195f));
            GameObject generatorDoor = CreateMaintenanceDoor(
                "Future Enemy Entrance - Generator Service Door",
                parent,
                new Vector3(19.92f, 1.55f, -8f),
                Quaternion.Euler(0f, -90f, 0f),
                materials,
                true,
                new MaintenanceDoorLayout(1.1237f, 0.38f, 2.23f, 0.29f, 0.00248f));
            EnemySpawnPoint mineSpawnPoint = CreateEnemySpawnPoint(
                mineDoor.transform,
                "Mine Door Enemy Spawn Point",
                new[]
                {
                    new EnemySpawnWeight(EnemyArchetype.Saboteur, 0.55f),
                    new EnemySpawnWeight(EnemyArchetype.Drone, 0.2f),
                    new EnemySpawnWeight(EnemyArchetype.Armored, 0.25f)
                },
                enemyLayer);
            EnemySpawnPoint generatorSpawnPoint = CreateEnemySpawnPoint(
                generatorDoor.transform,
                "Generator Door Enemy Spawn Point",
                new[]
                {
                    new EnemySpawnWeight(EnemyArchetype.Drone, 0.4f),
                    new EnemySpawnWeight(EnemyArchetype.Saboteur, 0.35f),
                    new EnemySpawnWeight(EnemyArchetype.Armored, 0.25f)
                },
                enemyLayer);

            return new EnemyEntranceSet(mineSpawnPoint, generatorSpawnPoint);
        }

        private static EnemySpawnPoint CreateEnemySpawnPoint(
            Transform door,
            string name,
            EnemySpawnWeight[] weights,
            int enemyLayer)
        {
            GameObject spawnObject = CreateGroup(name, door);
            spawnObject.transform.localPosition = new Vector3(0f, -1.5f, 2.25f);
            spawnObject.transform.localRotation = Quaternion.identity;
            EnemySpawnPoint spawnPoint = spawnObject.AddComponent<EnemySpawnPoint>();
            spawnPoint.Configure(weights, 1.3f, 3.4f, 6f, 1 << enemyLayer);
            return spawnPoint;
        }

        private static void CreateProductionFlowDressing(Transform parent, MapMaterials materials)
        {
            GameObject oreHoist = CreateGroup("Enclosed Ore Hoist", parent);
            // Preserve the authored Smelter-clearance offset from the authoritative scene.
            oreHoist.transform.localPosition = new Vector3(-1.31f, 0f, 0f);
            CreateBox("Hoist Shaft", oreHoist.transform, new Vector3(-17.6f, 2.8f, -2f), new Vector3(1.8f, 5.6f, 1.8f), materials.Dark, true);
            CreateBox("Hoist Orange Spine", oreHoist.transform, new Vector3(-18.52f, 2.8f, -2f), new Vector3(0.18f, 5.8f, 2f), materials.Frame, false);
            for (int index = 0; index < 5; index++)
            {
                CreateBox("Hoist Window " + (index + 1), oreHoist.transform, new Vector3(-16.66f, 0.8f + index, -2f), new Vector3(0.08f, 0.55f, 0.8f), index % 2 == 0 ? materials.Energy : materials.Steel, false);
            }

            GameObject componentShaft = CreateGroup("Open Component Conveyor Shaft", parent);
            foreach (float xOffset in new[] { -1.05f, 1.05f })
            {
                foreach (float zOffset in new[] { -1.05f, 1.05f })
                {
                    CreateBox(
                        "Component Shaft Post",
                        componentShaft.transform,
                        new Vector3(-7f + xOffset, 9.4f, 10.5f + zOffset),
                        new Vector3(0.18f, 8.2f, 0.18f),
                        materials.Dark,
                        true);
                }
            }

            CreateBox("Component Shaft Back Panel", componentShaft.transform, new Vector3(-7f, 9.4f, 11.55f), new Vector3(2.2f, 8.2f, 0.14f), materials.Dark, true);
            CreateBox("Component Shaft Cyan Spine", componentShaft.transform, new Vector3(-5.85f, 9.4f, 10.5f), new Vector3(0.16f, 7.7f, 1.8f), materials.Energy, false);
            CreatePipeBetween("Generator Feed Pipe", parent, new Vector3(13f, 8.7f, -3f), new Vector3(13f, 12f, -3f), 0.3f, materials.Steel, false);
        }

        private static void CreateOverheadCraneRails(Transform parent, MapMaterials materials)
        {
            CreateBox("Overhead Crane Rail West", parent, new Vector3(-6f, 21.78f, -1.47f), new Vector3(0.48f, 0.42f, 27f), materials.Steel, true);
            CreateBox("Overhead Crane Rail East", parent, new Vector3(6f, 21.78f, -1.47f), new Vector3(0.48f, 0.42f, 27f), materials.Steel, true);
            CreateBox("Overhead Rail Orange West", parent, new Vector3(-6f, 21.57f, -1.47f), new Vector3(0.62f, 0.12f, 27f), materials.Frame, false);
            CreateBox("Overhead Rail Orange East", parent, new Vector3(6f, 21.58f, -1.47f), new Vector3(0.62f, 0.12f, 27f), materials.Frame, false);
        }

        private static void CreateFreightLift(Transform parent, MapMaterials materials)
        {
            GameObject shaft = CreateGroup("Powered Freight Lift Shortcut", parent);
            shaft.transform.position = new Vector3(16.5f, 8.05f, 6.25f);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    CreateBox(
                        "Lift Shaft Post",
                        shaft.transform,
                        new Vector3(x * 1.55f, 1.7f, z * 1.55f),
                        new Vector3(0.22f, 7.2f, 0.22f),
                        materials.Frame,
                        true);
                }
            }

            GameObject platform = CreateGroup("Moving Lift Platform", shaft.transform);
            CreateBox("Lift Deck", platform.transform, new Vector3(0f, -0.16f, 0f), new Vector3(2.9f, 0.32f, 2.9f), materials.Deck, true);
            CreateBox("Lift Frame", platform.transform, new Vector3(0f, -0.34f, 0f), new Vector3(3.2f, 0.18f, 3.2f), materials.Frame, true);
            Rigidbody body = platform.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            BoxCollider trigger = CreateGroup("Passenger Carry Trigger", platform.transform).AddComponent<BoxCollider>();
            trigger.center = new Vector3(0f, 0.55f, 0f);
            trigger.size = new Vector3(2.8f, 1.1f, 2.8f);
            trigger.isTrigger = true;
            FactoryMovingPlatform movingPlatform = platform.AddComponent<FactoryMovingPlatform>();
            movingPlatform.Configure(Vector3.zero, new Vector3(0f, 5.05f, 0f), 3.2f, 0.65f, true);

            CreateBox("Lower Lift Stop", shaft.transform, new Vector3(0f, -0.18f, -1.75f), new Vector3(3.5f, 0.36f, 0.5f), materials.Steel, true);
            CreateBox("Upper Lift Stop", shaft.transform, new Vector3(0f, 2.87f, -1.75f), new Vector3(3.5f, 0.36f, 0.5f), materials.Steel, true);
        }

        private static FactoryObjectiveTerminal CreateObjectiveTerminal(
            Transform parent,
            string objectName,
            string stationName,
            Vector3 position,
            Quaternion rotation,
            FactoryObjectiveTerminal prerequisite,
            GameObject[] poweredObjects,
            Light[] workLights,
            ConveyorBelt[] belts,
            MapMaterials materials)
        {
            GameObject root = CreateGroup(objectName, parent);
            root.transform.SetPositionAndRotation(position, rotation);
            CreateBox("Terminal Pedestal", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.25f, 1.3f, 0.85f), materials.Dark, true, Quaternion.Euler(-8f, 0f, 0f));
            CreateBox("Terminal Frame", root.transform, new Vector3(0f, 0.75f, -0.47f), new Vector3(1.45f, 1.05f, 0.14f), materials.Frame, false, Quaternion.Euler(-8f, 0f, 0f));
            Renderer indicator = CreateBox("Terminal Status Indicator", root.transform, new Vector3(0f, 1.05f, -0.57f), new Vector3(0.72f, 0.18f, 0.08f), materials.Active, false, Quaternion.Euler(-8f, 0f, 0f)).GetComponent<Renderer>();
            CreateBox("Terminal Screen", root.transform, new Vector3(0f, 0.73f, -0.575f), new Vector3(0.86f, 0.38f, 0.08f), materials.Energy, false, Quaternion.Euler(-8f, 0f, 0f));

            FactoryObjectiveTerminal terminal = root.AddComponent<FactoryObjectiveTerminal>();
            terminal.Configure(stationName, prerequisite, indicator, poweredObjects, workLights, belts, false);
            root.AddComponent<InteractionTarget>().Configure(terminal);
            return terminal;
        }

        private static FactoryPortalGate BuildPortalObjectives(Transform parent, GameObject portal, MapMaterials materials)
        {
            GameObject bridge = CreateGroup("Portal Access Bridge - Core Gated", parent);
            CreateIndustrialDeck("Retractable Bridge Segment", bridge.transform, new Vector3(0f, 13.75f), new Vector2(4f, 1.5f), 15.5f, 9f, materials, true, false);
            CreateBox("Bridge Direction Arrow", bridge.transform, new Vector3(0f, 15.69f, 13.75f), new Vector3(0.8f, 0.03f, 0.9f), materials.Energy, false, Quaternion.Euler(0f, 45f, 0f));

            GameObject coreRack = CreateGroup("Portal Core Capacitor Bank", parent);
            CreateBox("Core Bank Backplate", coreRack.transform, new Vector3(-2.55f, 18.42f, 18.55f), new Vector3(0.92f, 3.22f, 0.28f), materials.Dark, false);
            CreateBox("Core Bank Left Brace", coreRack.transform, new Vector3(-3.06f, 18.42f, 18.42f), new Vector3(0.14f, 3.48f, 0.22f), materials.Frame, false);
            CreateBox("Core Bank Right Brace", coreRack.transform, new Vector3(-2.04f, 18.42f, 18.42f), new Vector3(0.14f, 3.48f, 0.22f), materials.Frame, false);

            Renderer[] sockets = new Renderer[3];
            GameObject[] installedCores = new GameObject[3];
            for (int index = 0; index < sockets.Length; index++)
            {
                Vector3 socketPosition = new Vector3(-2.55f, 17.48f + index * 0.94f, 18.31f);
                CreateCylinder("Portal Core Socket " + (index + 1), coreRack.transform, socketPosition, new Vector3(0.47f, 0.11f, 0.47f), Quaternion.Euler(90f, 0f, 0f), materials.Frame, false);
                sockets[index] = CreateCylinder("Portal Socket Indicator " + (index + 1), coreRack.transform, socketPosition + new Vector3(0f, 0f, -0.13f), new Vector3(0.3f, 0.04f, 0.3f), Quaternion.Euler(90f, 0f, 0f), materials.Energy, false).GetComponent<Renderer>();

                GameObject installedCore = PlacePrefab(
                    PortalCorePrefabPath,
                    socketPosition + new Vector3(0f, 0f, -0.28f),
                    Quaternion.Euler(90f, 0f, 0f),
                    coreRack.transform,
                    "Installed Portal Core " + (index + 1));
                installedCore.transform.localScale = Vector3.one * 0.3f;
                foreach (Collider collider in installedCore.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }

                installedCore.SetActive(false);
                installedCores[index] = installedCore;
            }

            FactoryPortalVisual portalVisual = portal != null ? portal.GetComponent<FactoryPortalVisual>() : null;
            FactoryPortalGate gate = (portal != null ? portal : CreateGroup("Portal Gate Logic", parent)).AddComponent<FactoryPortalGate>();
            gate.Configure(portalVisual, bridge, sockets, installedCores, 1);

            gate.ResetGate();
            return gate;
        }

        private static void BuildPortalCompletionFlow(
            FactoryPortalGate portalGate,
            GameObject portal,
            PlayerRigSet player,
            EnemySpawnManager spawnManager,
            Transform parent)
        {
            if (portalGate == null || portal == null || player == null)
            {
                throw new InvalidOperationException("The portal victory flow requires a portal gate and player rig.");
            }

            GameObject destination = CreateGroup("Victory Portal Destination", portal.transform);
            destination.transform.localPosition = new Vector3(0f, 3.15f, 0.1f);
            destination.transform.localRotation = Quaternion.identity;

            GameObject cameraAnchor = CreateGroup("Victory Camera Anchor", parent);
            cameraAnchor.transform.position = new Vector3(7.2f, 21.8f, 13f);
            cameraAnchor.transform.rotation = Quaternion.LookRotation(
                destination.transform.position - cameraAnchor.transform.position,
                Vector3.up);

            FactoryVictoryController victoryController = player.Hud.AddComponent<FactoryVictoryController>();
            victoryController.Configure(
                player.Player.transform,
                player.CharacterController,
                player.PlayerHealth,
                player.PlayerController,
                player.PlayerInteractor,
                player.OrbitCamera,
                player.CameraTransform,
                destination.transform,
                cameraAnchor.transform,
                player.StatusPresenter,
                spawnManager,
                player.Player.GetComponentsInChildren<Renderer>(true),
                2.6f);

            GameObject triggerObject = CreateGroup("Portal Completion Trigger", portal.transform);
            triggerObject.transform.localPosition = new Vector3(0f, 1.8f, 0.8f);
            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.center = Vector3.zero;
            triggerCollider.size = new Vector3(3.4f, 3.4f, 1.6f);
            triggerCollider.isTrigger = true;
            FactoryPortalCompletionTrigger completionTrigger =
                triggerObject.AddComponent<FactoryPortalCompletionTrigger>();
            completionTrigger.Configure(
                portalGate,
                player.Player.transform,
                victoryController,
                triggerCollider);

            FactoryPauseController pauseController = player.Hud.AddComponent<FactoryPauseController>();
            pauseController.Configure(
                player.PauseAction,
                player.StatusPresenter,
                player.PlayerController,
                player.PlayerInteractor,
                player.OrbitCamera,
                player.GameOverController,
                victoryController,
                player.Hud.GetComponent<FactorySettingsPresenter>());
        }

        private static void CreatePortalCorePickup(
            Transform parent,
            FactoryPortalGate gate,
            int socketIndex,
            Vector3 position,
            string coreName,
            MapMaterials materials)
        {
            GameObject pedestal = CreateGroup(coreName + " Pedestal", parent);
            pedestal.transform.position = new Vector3(position.x, position.y - 1.15f, position.z);
            CreateCylinder("Pedestal Base", pedestal.transform, Vector3.zero, new Vector3(0.8f, 0.18f, 0.8f), Quaternion.identity, materials.Frame, true);
            Renderer indicator = CreateCylinder("Pedestal Indicator", pedestal.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.08f, 0.55f), Quaternion.identity, materials.Energy, false).GetComponent<Renderer>();

            GameObject core = PlacePrefab(PortalCorePrefabPath, position, Quaternion.identity, parent, coreName);
            FactoryPortalCorePickup pickup = core.AddComponent<FactoryPortalCorePickup>();
            pickup.Configure(gate, socketIndex, core, indicator, coreName);
            core.AddComponent<InteractionTarget>().Configure(pickup);
        }

        private static PlayerRigSet BuildPlayerRig(Transform parent)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new InvalidOperationException("Missing player prefab at " + PlayerPrefabPath);
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, parent);
            player.name = "Player";
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.22f, -17f), Quaternion.identity);
            SetLayerRecursively(player, 2);
            Targetable playerTarget = EnsurePlayerCombat(player);

            InputActionReference move = AssetDatabase.LoadAssetAtPath<InputActionReference>(MoveReferencePath);
            InputActionReference look = AssetDatabase.LoadAssetAtPath<InputActionReference>(LookReferencePath);
            InputActionReference jump = AssetDatabase.LoadAssetAtPath<InputActionReference>(JumpReferencePath);
            InputActionReference sprint = AssetDatabase.LoadAssetAtPath<InputActionReference>(SprintReferencePath);
            InputActionReference dash = AssetDatabase.LoadAssetAtPath<InputActionReference>(DashReferencePath);
            InputActionReference interact = AssetDatabase.LoadAssetAtPath<InputActionReference>(InteractReferencePath);
            InputActionReference pause = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseReferencePath);
            PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3.1f, -20f), Quaternion.identity);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 140f;
            UniversalAdditionalCameraData cameraData =
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraObject.AddComponent<AudioListener>();
            CameraShakeController cameraShake = cameraObject.AddComponent<CameraShakeController>();
            ThirdPersonOrbitCamera orbitCamera = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbitCamera.Configure(player.transform, look, ~(1 << 2), cameraShake);

            GameObject hud = new GameObject("Factory HUD");
            hud.transform.SetParent(parent, false);
            UIDocument document = hud.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudLayoutPath);
            StyleSheet hudStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudStylePath);
            InteractionPromptPresenter prompt = hud.AddComponent<InteractionPromptPresenter>();
            prompt.Configure(document, hudStyle);
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            PlayerStatusPresenter playerStatus = hud.AddComponent<PlayerStatusPresenter>();
            playerStatus.Configure(document, hudStyle, playerHealth);
            AudioMixer mixer = RequireAsset<AudioMixer>(GameAudioAssetFactory.MixerPath);
            AudioMixerGroup sfxGroup = mixer.FindMatchingGroups(GameAudioAssetFactory.SfxGroupName).First();
            AudioSettingsController audioSettings = hud.AddComponent<AudioSettingsController>();
            audioSettings.Configure(mixer);
            FactorySettingsPresenter settingsPresenter = hud.AddComponent<FactorySettingsPresenter>();
            settingsPresenter.Configure(document, audioSettings);

            CharacterController characterController = player.GetComponent<CharacterController>();
            ConveyorPassenger passenger = player.GetComponent<ConveyorPassenger>();
            passenger.Configure(null, characterController, false);
            ThirdPersonPlayerController playerController = player.GetComponent<ThirdPersonPlayerController>();
            playerController.Configure(
                characterController,
                cameraObject.transform,
                passenger,
                settings,
                move,
                jump,
                sprint,
                dash);
            PlayerInteractor playerInteractor = player.GetComponent<PlayerInteractor>();
            playerInteractor.Configure(cameraObject.transform, interact, prompt, ~(1 << 2));
            AudioSource feedbackSource = player.AddComponent<AudioSource>();
            AudioSource repairSource = player.AddComponent<AudioSource>();
            feedbackSource.outputAudioMixerGroup = sfxGroup;
            repairSource.outputAudioMixerGroup = sfxGroup;
            PlayerFeedbackEffects feedbackEffects = GetOrAddComponent<PlayerFeedbackEffects>(player);
            feedbackEffects.Configure(
                playerController,
                playerHealth,
                playerInteractor,
                cameraShake,
                feedbackSource,
                repairSource,
                RequireAsset<AudioClip>(PlayerHitAudioPath),
                RequireAsset<AudioClip>(RepairHammerAudioPath),
                RequireAsset<GameObject>(EnemyAssetFactory.PlayerJumpEffectPath),
                RequireAsset<GameObject>(EnemyAssetFactory.DoubleJumpEffectPath),
                RequireAsset<GameObject>(EnemyAssetFactory.PlayerDashEffectPath),
                RequireAsset<GameObject>(EnemyAssetFactory.PlayerHitEffectPath),
                RequireAsset<GameObject>(EnemyAssetFactory.RepairLoopEffectPath),
                playerTarget.TargetPoint);
            AudioSource movementSource = player.AddComponent<AudioSource>();
            movementSource.outputAudioMixerGroup = sfxGroup;
            PlayerMovementAudio movementAudio = player.AddComponent<PlayerMovementAudio>();
            movementAudio.Configure(
                playerController,
                player.GetComponentInChildren<ProceduralPlayerAnimator>(true),
                movementSource,
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerFootstepPaths),
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerJumpPaths),
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerDoubleJumpPaths),
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerDashPaths),
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerLightLandingPaths),
                GameAudioAssetFactory.LoadClips(GameAudioAssetFactory.PlayerHeavyLandingPaths));
            FactoryGameOverController gameOver = hud.AddComponent<FactoryGameOverController>();
            gameOver.Configure(
                playerHealth,
                playerStatus,
                playerController,
                playerInteractor,
                orbitCamera,
                player.GetComponentsInChildren<Renderer>(true));
            return new PlayerRigSet(
                player,
                hud,
                playerTarget,
                playerHealth,
                characterController,
                playerController,
                playerInteractor,
                orbitCamera,
                cameraObject.transform,
                playerStatus,
                gameOver,
                pause,
                cameraShake);
        }

        private static void ConfigureMachineBreakPresentation(
            GameObject machineObject,
            CameraShakeController cameraShake)
        {
            if (machineObject == null)
            {
                return;
            }

            FactoryMachineHealth machineHealth = machineObject.GetComponent<FactoryMachineHealth>();
            if (machineHealth == null)
            {
                return;
            }

            AudioSource audioSource = machineObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = GameAudioAssetFactory.GetGroup(GameAudioAssetFactory.SfxGroupName);
            MachineBreakPresentation presentation = GetOrAddComponent<MachineBreakPresentation>(machineObject);
            presentation.Configure(
                machineHealth,
                audioSource,
                RequireAsset<AudioClip>(RubbleCrashAudioPath),
                RequireAsset<GameObject>(EnemyAssetFactory.MachineBreakEffectPath),
                cameraShake);
        }

        private static Targetable EnsurePlayerCombat(GameObject player)
        {
            Health health = GetOrAddComponent<Health>(player);
            FactionMember factionMember = GetOrAddComponent<FactionMember>(player);
            Targetable targetable = GetOrAddComponent<Targetable>(player);
            PlayerHealth playerHealth = GetOrAddComponent<PlayerHealth>(player);

            TargetPoint targetPoint = player.GetComponentInChildren<TargetPoint>(true);
            if (targetPoint == null)
            {
                GameObject targetPointObject = CreateGroup("Target Point", player.transform);
                targetPointObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                targetPoint = targetPointObject.AddComponent<TargetPoint>();
                SetLayerRecursively(targetPointObject, player.layer);
            }

            targetable.Configure(factionMember, targetPoint, playerHealth, true);
            playerHealth.Configure(health, factionMember, targetable, 100, 0.35f);
            GameObject deathExplosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyAssetFactory.DeathExplosionPrefabPath);
            if (deathExplosionPrefab == null)
            {
                throw new InvalidOperationException(
                    "Missing shared death explosion prefab at " + EnemyAssetFactory.DeathExplosionPrefabPath + ".");
            }

            DeathExplosionEmitter deathExplosion = GetOrAddComponent<DeathExplosionEmitter>(player);
            deathExplosion.Configure(deathExplosionPrefab, targetPoint.transform, 1.1f);
            playerHealth.ConfigureDeathExplosion(deathExplosion);
            return targetable;
        }

        private static void BindMachineToTerminal(
            GameObject machineObject,
            FactoryObjectiveTerminal terminal,
            MachineTargetRegistry registry)
        {
            if (machineObject == null)
            {
                throw new InvalidOperationException("Cannot bind a missing factory machine to its terminal.");
            }

            if (terminal == null)
            {
                throw new InvalidOperationException(machineObject.name + " is missing its objective terminal.");
            }

            FactoryMachineHealth machineHealth = machineObject.GetComponent<FactoryMachineHealth>();
            if (machineHealth == null)
            {
                throw new InvalidOperationException(
                    machineObject.name + " is missing FactoryMachineHealth. Rebuild machine assets first.");
            }

            Transform targetPoint = machineHealth.Targetable != null
                ? machineHealth.Targetable.TargetPoint
                : null;
            if (targetPoint != null && targetPoint != machineObject.transform)
            {
                Vector3 localPosition = machineObject.transform.InverseTransformPoint(targetPoint.position);
                // Keep the attack point closer to the service deck than to any baked
                // walkable machine roof. This makes direct NavMesh queries and the
                // runtime resolver agree on the reachable attack position.
                localPosition.y = Mathf.Min(localPosition.y, 0.9f);
                targetPoint.position = machineObject.transform.TransformPoint(localPosition);
            }

            machineHealth.BindTerminal(terminal);
            machineHealth.AssignRegistry(registry);
            PrefabUtility.RecordPrefabInstancePropertyModifications(machineHealth);
        }

        private static EnemySpawnManager BuildEnemySpawnManager(
            Transform parent,
            EnemyEntranceSet entrances,
            Targetable player,
            MachineTargetRegistry machineRegistry,
            EnemyRuntimeRegistry enemyRegistry,
            FactoryObjectiveTerminal spawnUnlockTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            CameraShakeController cameraShake)
        {
            GameObject dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DroneEnemyPrefabPath);
            GameObject saboteurPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaboteurEnemyPrefabPath);
            GameObject armoredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoredEnemyPrefabPath);
            if (dronePrefab == null || saboteurPrefab == null || armoredPrefab == null)
            {
                throw new InvalidOperationException(
                    "One or more generated enemy prefabs are missing. Run Tools/Factory/Build Enemy Assets.");
            }

            if (player == null || entrances.MineDoor == null || entrances.GeneratorDoor == null)
            {
                throw new InvalidOperationException("The factory enemy spawner is missing a player or lower-door spawn point.");
            }

            EnemySpawnManager spawnManager = CreateGroup(
                "Enemy Spawn Manager",
                parent).AddComponent<EnemySpawnManager>();
            spawnManager.Configure(
                dronePrefab,
                saboteurPrefab,
                armoredPrefab,
                new[] { entrances.MineDoor, entrances.GeneratorDoor },
                player,
                machineRegistry,
                enemyRegistry,
                spawnUnlockTerminal,
                assemblerTerminal,
                6f,
                12f,
                18f,
                6,
                60f,
                2,
                3,
                0.8f,
                cameraShake);
            return spawnManager;
        }

        private static void BuildAndPersistEnemyNavMesh(GameObject navigationRoot)
        {
            NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << 0;
            surface.BuildNavMesh();

            NavMeshData generatedData = surface.navMeshData;
            if (generatedData == null)
            {
                throw new InvalidOperationException("Unity did not generate NavMesh data for FactoryVerticalMap.");
            }

            surface.RemoveData();
            NavMeshData persistedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath);
            if (persistedData == null)
            {
                generatedData.name = "NavMesh-EnemyService";
                AssetDatabase.CreateAsset(generatedData, NavMeshDataPath);
                persistedData = generatedData;
            }
            else
            {
                EditorUtility.CopySerialized(generatedData, persistedData);
                persistedData.name = "NavMesh-EnemyService";
                EditorUtility.SetDirty(persistedData);
                UnityEngine.Object.DestroyImmediate(generatedData);
            }

            surface.navMeshData = persistedData;
            surface.AddData();
            persistedData.name = "NavMesh-EnemyService";
            EditorUtility.SetDirty(persistedData);
            NavMeshLink[] links = navigationRoot.GetComponentsInChildren<NavMeshLink>(true);
            foreach (NavMeshLink link in links)
            {
                link.UpdateLink();
            }

            EditorUtility.SetDirty(surface);
        }

        private static ProductionConveyorRoute CreateProductionConveyorRoute(
            string name,
            Transform parent,
            Vector3[] routePoints)
        {
            if (routePoints == null || routePoints.Length < 2)
            {
                throw new InvalidOperationException(name + " requires at least two route points.");
            }

            GameObject root = CreateGroup(name, parent);
            ConveyorBelt[] belts = new ConveyorBelt[routePoints.Length - 1];
            for (int index = 0; index < belts.Length; index++)
            {
                belts[index] = CreateConveyor(
                    name + " Segment " + (index + 1),
                    root.transform,
                    routePoints[index],
                    routePoints[index + 1],
                    ConveyorOperatingState.Offline);
                belts[index].gameObject.SetActive(false);
            }

            for (int index = 1; index < routePoints.Length - 1; index++)
            {
                Vector3 incoming = (routePoints[index] - routePoints[index - 1]).normalized;
                Vector3 outgoing = (routePoints[index + 1] - routePoints[index]).normalized;
                if (Vector3.Dot(incoming, outgoing) > 0.998f)
                {
                    continue;
                }

                GameObject turnObject = PlacePrefab(
                    ConveyorTurnPrefabPath,
                    routePoints[index],
                    Quaternion.identity,
                    root.transform,
                    name + " Turn " + index);
                PrefabUtility.UnpackPrefabInstance(
                    turnObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                ConveyorTurnModule turn = turnObject.GetComponent<ConveyorTurnModule>();
                if (turn == null)
                {
                    throw new InvalidOperationException("Missing ConveyorTurnModule on " + ConveyorTurnPrefabPath + ".");
                }

                turn.Configure(routePoints[index - 1], routePoints[index], routePoints[index + 1]);
                turnObject.transform.SetParent(belts[index - 1].transform, true);
            }

            return new ProductionConveyorRoute(root, belts, routePoints);
        }

        private static FactoryConveyorConnection ConfigureProductionConveyorRoute(
            ProductionConveyorRoute route,
            string connectionName,
            FactoryObjectiveTerminal sourceTerminal,
            FactoryObjectiveTerminal destinationTerminal,
            MapMaterials materials,
            Vector3 sourceSocketPosition,
            Vector3 destinationSocketPosition)
        {
            GameObject sourcePoint = CreateConveyorConnectionPoint(
                "Source Socket - " + connectionName,
                route.Root.transform,
                sourceSocketPosition,
                materials,
                out Renderer sourceIndicator,
                out _);
            GameObject destinationPoint = CreateConveyorConnectionPoint(
                "Destination Socket - " + connectionName,
                route.Root.transform,
                destinationSocketPosition,
                materials,
                out Renderer destinationIndicator,
                out GameObject destinationMarker);

            FactoryConveyorConnection connection = route.Root.AddComponent<FactoryConveyorConnection>();
            connection.Configure(
                connectionName,
                sourceTerminal,
                destinationTerminal,
                route.Belts,
                sourceIndicator,
                destinationIndicator,
                destinationMarker);

            ConveyorConnectionPoint sourceInteraction = sourcePoint.AddComponent<ConveyorConnectionPoint>();
            sourceInteraction.Configure(connection, true);
            sourcePoint.AddComponent<InteractionTarget>().Configure(sourceInteraction);

            ConveyorConnectionPoint destinationInteraction = destinationPoint.AddComponent<ConveyorConnectionPoint>();
            destinationInteraction.Configure(connection, false);
            destinationPoint.AddComponent<InteractionTarget>().Configure(destinationInteraction);
            return connection;
        }

        private static GameObject CreateConveyorConnectionPoint(
            string name,
            Transform parent,
            Vector3 position,
            MapMaterials materials,
            out Renderer indicator,
            out GameObject destinationMarker)
        {
            GameObject root = CreateGroup(name, parent);
            root.transform.position = position;
            CreateCylinder(
                "Socket Base",
                root.transform,
                new Vector3(0f, 0.14f, 0f),
                new Vector3(0.72f, 0.14f, 0.72f),
                Quaternion.identity,
                materials.Frame,
                true);
            indicator = CreateCylinder(
                "Socket Indicator",
                root.transform,
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.48f, 0.08f, 0.48f),
                Quaternion.identity,
                materials.Energy,
                false).GetComponent<Renderer>();
            CreateBox(
                "Socket Interaction Post",
                root.transform,
                new Vector3(0f, 0.76f, 0f),
                new Vector3(0.32f, 0.78f, 0.32f),
                materials.Dark,
                true);

            destinationMarker = CreateGroup("Connection Destination Arrow", root.transform);
            destinationMarker.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            CreateBox("Arrow Stem", destinationMarker.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.18f, 0.82f, 0.18f), materials.Energy, false);
            CreateBox("Arrow Head Left", destinationMarker.transform, new Vector3(-0.19f, 0f, 0f), new Vector3(0.18f, 0.58f, 0.18f), materials.Energy, false, Quaternion.Euler(0f, 0f, -42f));
            CreateBox("Arrow Head Right", destinationMarker.transform, new Vector3(0.19f, 0f, 0f), new Vector3(0.18f, 0.58f, 0.18f), materials.Energy, false, Quaternion.Euler(0f, 0f, 42f));
            CreateBox("Arrow Head Front", destinationMarker.transform, new Vector3(0f, 0f, -0.19f), new Vector3(0.18f, 0.58f, 0.18f), materials.Energy, false, Quaternion.Euler(42f, 0f, 0f));
            CreateBox("Arrow Head Back", destinationMarker.transform, new Vector3(0f, 0f, 0.19f), new Vector3(0.18f, 0.58f, 0.18f), materials.Energy, false, Quaternion.Euler(-42f, 0f, 0f));
            destinationMarker.AddComponent<ConveyorDestinationMarker>();
            destinationMarker.SetActive(false);
            return root;
        }

        private static void BuildProductionLine(
            Transform parent,
            FactoryObjectiveTerminal mineTerminal,
            FactoryObjectiveTerminal smelterTerminal,
            FactoryObjectiveTerminal generatorTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            FactoryConveyorConnection mineToSmelter,
            FactoryConveyorConnection smelterToAssembler,
            FactoryConveyorConnection assemblerToPortal,
            FactoryPortalGate portalGate)
        {
            GameObject root = CreateGroup("Factory Production Line", parent);
            Transform cargoRoot = CreateGroup("Active Production Cargo", root.transform).transform;
            FactoryProductionLine productionLine = root.AddComponent<FactoryProductionLine>();
            productionLine.Configure(
                mineTerminal,
                smelterTerminal,
                assemblerTerminal,
                mineToSmelter,
                smelterToAssembler,
                assemblerToPortal,
                AssetDatabase.LoadAssetAtPath<GameObject>(OreCargoPrefabPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(IngotCargoPrefabPath),
                AssetDatabase.LoadAssetAtPath<GameObject>(PortalComponentCargoPrefabPath),
                cargoRoot,
                portalGate,
                4f,
                3f,
                4f);
            productionLine.BindPresentation(
                mineTerminal != null && mineTerminal.MachineHealth != null
                    ? mineTerminal.MachineHealth.GetComponent<FactoryMachinePresentation>()
                    : null,
                smelterTerminal != null && smelterTerminal.MachineHealth != null
                    ? smelterTerminal.MachineHealth.GetComponent<FactoryMachinePresentation>()
                    : null,
                generatorTerminal != null && generatorTerminal.MachineHealth != null
                    ? generatorTerminal.MachineHealth.GetComponent<FactoryMachinePresentation>()
                    : null,
                assemblerTerminal != null && assemblerTerminal.MachineHealth != null
                    ? assemblerTerminal.MachineHealth.GetComponent<FactoryMachinePresentation>()
                    : null);
        }

        private static ConveyorBelt CreateConveyor(
            string name,
            Transform parent,
            Vector3 startPosition,
            Vector3 endPosition,
            ConveyorOperatingState state)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            GameObject root = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)
                : CreateGroup(name, parent);
            root.name = name;

            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            ConveyorEndpoint[] oldEndpoints = root.GetComponentsInChildren<ConveyorEndpoint>(true);
            foreach (ConveyorEndpoint endpoint in oldEndpoints)
            {
                UnityEngine.Object.DestroyImmediate(endpoint.gameObject);
            }

            ConveyorBelt belt = root.GetComponent<ConveyorBelt>();
            if (belt == null)
            {
                belt = root.AddComponent<ConveyorBelt>();
            }

            ConveyorEndpoint start = CreateEndpoint("Start Endpoint", root.transform, startPosition, ConveyorEndpointKind.Output);
            ConveyorEndpoint end = CreateEndpoint("End Endpoint", root.transform, endPosition, ConveyorEndpointKind.Input);
            belt.SetEndpoints(start, end);
            belt.SetSpeed(2.5f);
            belt.SetOperatingState(state);
            belt.RebuildNow();
            return belt;
        }

        private static ConveyorEndpoint CreateEndpoint(string name, Transform parent, Vector3 worldPosition, ConveyorEndpointKind kind)
        {
            GameObject endpointObject = CreateGroup(name, parent);
            endpointObject.transform.position = worldPosition;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind, 0.25f);
            return endpoint;
        }

        private static void CreateLighting(Transform parent)
        {
            Light sun = new GameObject("Factory Key Light").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.color = new Color(0.66f, 0.78f, 0.92f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            CreatePointLight("Spawn Warm Light", parent, new Vector3(0f, 6f, -16f), new Color(1f, 0.42f, 0.12f), 9f, 6f);
            CreatePointLight("Central Shaft Light", parent, new Vector3(0f, 11f, 4f), new Color(0.08f, 0.55f, 0.85f), 13f, 8f);
            CreatePointLight("Portal Beacon Light", parent, new Vector3(0f, 20f, 18f), new Color(0.08f, 0.8f, 1f), 13f, 9f);
            CreatePointLight("Upper West Work Light", parent, new Vector3(-11f, 18f, 12f), new Color(0.95f, 0.42f, 0.12f), 9f, 6f);
            CreatePointLight("Upper East Work Light", parent, new Vector3(12f, 18f, 13f), new Color(0.1f, 0.68f, 0.95f), 9f, 6f);
        }

        private static void CreatePostProcessing(Transform parent)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "VP_FactoryAtmosphere";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            Bloom bloom = GetOrCreateVolumeOverride<Bloom>(profile);

            bloom.active = true;
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.32f);
            bloom.scatter.Override(0.55f);
            bloom.tint.Override(new Color(0.82f, 0.93f, 1f));

            ColorAdjustments colorAdjustments = GetOrCreateVolumeOverride<ColorAdjustments>(profile);

            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(0.05f);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(-5f);
            colorAdjustments.colorFilter.Override(new Color(0.94f, 0.98f, 1f));

            Vignette vignette = GetOrCreateVolumeOverride<Vignette>(profile);

            vignette.active = true;
            vignette.intensity.Override(0.15f);
            vignette.smoothness.Override(0.32f);

            Tonemapping tonemapping = GetOrCreateVolumeOverride<Tonemapping>(profile);

            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(colorAdjustments);
            EditorUtility.SetDirty(vignette);
            EditorUtility.SetDirty(tonemapping);

            Volume volume = CreateGroup("Factory Global Volume", parent).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private static T GetOrCreateVolumeOverride<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            profile.components.RemoveAll(component => component == null);
            if (profile.TryGet(out T existing) && existing != null)
            {
                return existing;
            }

            T created = ScriptableObject.CreateInstance<T>();
            created.name = typeof(T).Name;
            created.active = true;
            profile.components.Add(created);
            AssetDatabase.AddObjectToAsset(created, profile);
            EditorUtility.SetDirty(created);
            EditorUtility.SetDirty(profile);
            return created;
        }

        private static Light CreatePointLight(string name, Transform parent, Vector3 position, Color color, float range, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            return light;
        }

        private static void ConfigureRenderSettings()
        {
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.16f, 0.2f);
            RenderSettings.ambientEquatorColor = new Color(0.055f, 0.07f, 0.085f);
            RenderSettings.ambientGroundColor = new Color(0.018f, 0.022f, 0.028f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.04f, 0.052f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 72f;
        }

        private static void CreateCrateStackPlatform(Transform parent, Vector2 center, float topY, MapMaterials materials)
        {
            GameObject root = CreateGroup("Cargo Crate Step", parent);
            CreateBox("Lower Cargo Crate", root.transform, new Vector3(center.x - 0.45f, 0.4f, center.y), new Vector3(1.55f, 0.8f, 2.3f), materials.Dark, true);
            CreateBox("Upper Cargo Crate", root.transform, new Vector3(center.x + 0.55f, 0.45f, center.y + 0.15f), new Vector3(1.2f, 0.9f, 2f), materials.Deck, true);
            CreateBox("Crate Top Plate", root.transform, new Vector3(center.x, topY - 0.08f, center.y), new Vector3(2.55f, 0.16f, 2.35f), materials.Steel, true);
            AddDeckCornerBrackets(root.transform, new Vector3(center.x, topY - 0.45f, center.y), new Vector2(2.55f, 2.35f), 0.9f, materials);
        }

        private static void CreatePipeRackPlatform(
            Transform parent,
            Vector2 center,
            Vector2 size,
            float topY,
            MapMaterials materials,
            Vector3? gratingPositionOverride = null,
            Vector3? rootLocalPosition = null)
        {
            GameObject root = CreateGroup("Pipe Rack Step " + topY.ToString("0.00"), parent);
            root.transform.localPosition = rootLocalPosition ?? Vector3.zero;
            float supportBottom = Mathf.Max(0f, topY - 1.3f);
            float supportHeight = Mathf.Max(0.4f, topY - supportBottom);
            float upperSupportY = supportBottom + supportHeight * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = center.x + side * (size.x * 0.42f);
                string middleSuffix = side < 0 ? " (1)" : " (2)";
                string lowerSuffix = side < 0 ? " (3)" : " (4)";
                CreateBox("Pipe Rack Post", root.transform, new Vector3(x, upperSupportY, center.y), new Vector3(0.22f, supportHeight, size.y), materials.Frame, true);
                CreateBox("Pipe Rack Post" + middleSuffix, root.transform, new Vector3(x, upperSupportY - 1.26f, center.y), new Vector3(0.22f, supportHeight, size.y), materials.Frame, true);
                CreateBox("Pipe Rack Post" + lowerSuffix, root.transform, new Vector3(x, upperSupportY - 2.48f, center.y), new Vector3(0.22f, supportHeight, size.y), materials.Frame, true);
            }

            for (int index = -1; index <= 1; index++)
            {
                CreateCylinder(
                    "Service Pipe " + (index + 2),
                    root.transform,
                    new Vector3(center.x + index * 0.62f, topY - 0.28f, center.y),
                    new Vector3(0.28f, size.y * 0.48f, 0.28f),
                    Quaternion.Euler(90f, 0f, 0f),
                    index == 0 ? materials.Energy : materials.Steel,
                    false);
            }

            Vector3 gratingPosition = gratingPositionOverride ?? new Vector3(center.x, topY - 0.1f, center.y);
            CreateBox("Pipe Rack Grating", root.transform, gratingPosition, new Vector3(size.x, 0.2f, size.y), materials.Deck, true);
            CreateBox("Pipe Rack Orange Edge", root.transform, new Vector3(center.x, topY - 0.22f, center.y - size.y * 0.5f), new Vector3(size.x, 0.16f, 0.16f), materials.Frame, false);
        }

        private static void CreateCableTrayPlatform(Transform parent, Vector2 center, Vector2 size, float topY, MapMaterials materials)
        {
            CreateIndustrialDeck("Cable Tray Landing", parent, center, size, topY, 8f, materials, true, false);
            for (int index = -2; index <= 2; index++)
            {
                CreateCylinder(
                    "Power Cable " + (index + 3),
                    parent,
                    new Vector3(center.x + index * 0.38f, topY + 0.11f, center.y),
                    new Vector3(0.08f, size.y * 0.45f, 0.08f),
                    Quaternion.Euler(90f, 0f, 0f),
                    index == 0 ? materials.Energy : materials.Dark,
                    false);
            }
        }

        private static void CreateMachineHousingPlatform(
            string name,
            Transform parent,
            Vector2 center,
            Vector2 size,
            float topY,
            MapMaterials materials,
            float bottomY = 0f,
            float trimYOffset = 0f)
        {
            float height = Mathf.Max(0.35f, topY - bottomY);
            CreateBox(name + " Housing", parent, new Vector3(center.x, bottomY + height * 0.5f, center.y), new Vector3(size.x, height, size.y), materials.Dark, true);
            CreateBox(name + " Top Plate", parent, new Vector3(center.x, topY - 0.1f + trimYOffset, center.y), new Vector3(size.x + 0.18f, 0.2f, size.y + 0.18f), materials.Steel, true);
            CreateBox(name + " Orange Band", parent, new Vector3(center.x, topY - 0.35f + trimYOffset, center.y - size.y * 0.51f), new Vector3(size.x, 0.22f, 0.12f), materials.Frame, false);
        }

        private static GameObject CreateIndustrialDeck(
            string name,
            Transform parent,
            Vector2 center,
            Vector2 size,
            float topY,
            float supportBottom,
            MapMaterials materials,
            bool addSupports,
            bool addFullRails)
        {
            GameObject root = CreateGroup(name, parent);
            CreateBox("Walkable Grating", root.transform, new Vector3(center.x, topY - 0.16f, center.y), new Vector3(size.x, 0.32f, size.y), materials.Deck, true);
            CreateBox("Orange Underframe", root.transform, new Vector3(center.x, topY - 0.38f, center.y), new Vector3(size.x + 0.18f, 0.16f, size.y + 0.18f), materials.Frame, true);
            CreateBox("Steel Center Spine", root.transform, new Vector3(center.x, topY - 0.48f, center.y), new Vector3(Mathf.Max(0.25f, size.x - 0.6f), 0.16f, 0.28f), materials.Steel, false);

            if (addSupports && topY - supportBottom > 0.6f)
            {
                float height = topY - supportBottom - 0.45f;
                float y = supportBottom + height * 0.5f;
                float xOffset = Mathf.Max(0.25f, size.x * 0.5f - 0.28f);
                float zOffset = Mathf.Max(0.25f, size.y * 0.5f - 0.28f);
                foreach (float xSign in new[] { -1f, 1f })
                {
                    foreach (float zSign in new[] { -1f, 1f })
                    {
                        CreateBox(
                            "Support Post",
                            root.transform,
                            new Vector3(center.x + xSign * xOffset, y, center.y + zSign * zOffset),
                            new Vector3(0.32f, height, 0.32f),
                            materials.Frame,
                            true);
                    }
                }
            }

            if (addFullRails)
            {
                AddSafetyRail(root.transform, new Vector3(center.x, topY + 0.8f, center.y - size.y * 0.5f), size.x, true, materials);
                AddSafetyRail(root.transform, new Vector3(center.x, topY + 0.8f, center.y + size.y * 0.5f), size.x, true, materials);
            }

            return root;
        }

        private static void CreateSteppedCentralLoadColumn(
            Transform deck,
            Vector2 center,
            Vector2 deckSize,
            float topY,
            MapMaterials materials)
        {
            Transform structure = CreateGroup("Central Load Column", deck).transform;
            float floorY = GetLocalFactoryFloorY(deck, center);
            float supportTopY = topY - 0.45f;
            float availableHeight = Mathf.Max(0.6f, supportTopY - floorY);
            float columnWidth = Mathf.Clamp(Mathf.Min(deckSize.x, deckSize.y) * 0.28f, 1.4f, 2f);
            float footingHeight = Mathf.Min(0.34f, availableHeight * 0.16f);
            float footingWidth = columnWidth * 1.65f;
            float coreBottomY = floorY + footingHeight * 0.7f;
            float coreTopY = supportTopY - 0.22f;
            float coreHeight = Mathf.Max(0.25f, coreTopY - coreBottomY);

            CreateBox(
                "Footing",
                structure,
                new Vector3(center.x, floorY + footingHeight * 0.5f, center.y),
                new Vector3(footingWidth, footingHeight, footingWidth),
                materials.Frame,
                true);
            CreateBox(
                "Load Core",
                structure,
                new Vector3(center.x, coreBottomY + coreHeight * 0.5f, center.y),
                new Vector3(columnWidth * 0.58f, coreHeight, columnWidth * 0.58f),
                materials.Dark,
                true);
            CreateBox(
                "Lower Collar",
                structure,
                new Vector3(center.x, floorY + Mathf.Min(0.62f, availableHeight * 0.24f), center.y),
                new Vector3(columnWidth * 1.22f, 0.24f, columnWidth * 1.22f),
                materials.Frame,
                true);
            CreateBox(
                "Upper Collar",
                structure,
                new Vector3(center.x, supportTopY - 0.31f, center.y),
                new Vector3(columnWidth * 1.16f, 0.22f, columnWidth * 1.16f),
                materials.Frame,
                true);
            CreateBox(
                "Crosshead X",
                structure,
                new Vector3(center.x, supportTopY - 0.10f, center.y),
                new Vector3(deckSize.x * 0.72f, 0.2f, columnWidth * 0.42f),
                materials.Steel,
                true);
            CreateBox(
                "Crosshead Z",
                structure,
                new Vector3(center.x, supportTopY - 0.12f, center.y),
                new Vector3(columnWidth * 0.42f, 0.16f, deckSize.y * 0.68f),
                materials.Steel,
                true);

            float braceTopY = supportTopY - 0.22f;
            float braceBottomY = Mathf.Max(floorY + 0.65f, supportTopY - Mathf.Min(1.2f, availableHeight * 0.38f));
            float xReach = deckSize.x * 0.29f;
            float zReach = deckSize.y * 0.27f;
            CreateBoxBetween("Knee Brace East", structure, new Vector3(center.x + columnWidth * 0.28f, braceBottomY, center.y), new Vector3(center.x + xReach, braceTopY, center.y), 0.13f, materials.Frame, false);
            CreateBoxBetween("Knee Brace West", structure, new Vector3(center.x - columnWidth * 0.28f, braceBottomY, center.y), new Vector3(center.x - xReach, braceTopY, center.y), 0.13f, materials.Frame, false);
            CreateBoxBetween("Knee Brace North", structure, new Vector3(center.x, braceBottomY, center.y + columnWidth * 0.28f), new Vector3(center.x, braceTopY, center.y + zReach), 0.13f, materials.Frame, false);
            CreateBoxBetween("Knee Brace South", structure, new Vector3(center.x, braceBottomY, center.y - columnWidth * 0.28f), new Vector3(center.x, braceTopY, center.y - zReach), 0.13f, materials.Frame, false);
        }

        private static void CreateTwinGantrySupports(
            Transform deck,
            Vector2 center,
            Vector2 deckSize,
            float topY,
            MapMaterials materials)
        {
            Transform structure = CreateGroup("Twin Gantry Supports", deck).transform;
            float floorY = GetLocalFactoryFloorY(deck, center);
            float supportTopY = topY - 0.45f;
            float pylonOffset = deckSize.x * 0.28f;
            float pylonWidth = Mathf.Clamp(Mathf.Min(deckSize.x, deckSize.y) * 0.1f, 0.72f, 1.05f);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = center.x + side * pylonOffset;
                Transform pylon = CreateGroup(side < 0 ? "West Pylon" : "East Pylon", structure).transform;
                CreatePrimaryPylon(pylon, x, center.y, floorY, supportTopY, pylonWidth, materials);
                CreateBox(
                    "Deck Saddle",
                    pylon,
                    new Vector3(x, supportTopY - 0.1f, center.y),
                    new Vector3(pylonWidth * 1.65f, 0.2f, deckSize.y * 0.64f),
                    materials.Steel,
                    true);
            }

            CreateBox(
                "Connecting Header",
                structure,
                new Vector3(center.x, supportTopY - 0.30f, center.y),
                new Vector3(pylonOffset * 2f + pylonWidth, 0.24f, pylonWidth * 0.62f),
                materials.Frame,
                true);
            float braceY = Mathf.Max(floorY + 0.8f, supportTopY - 1.35f);
            CreateBoxBetween("Upper Brace West", structure, new Vector3(center.x - pylonOffset, braceY, center.y), new Vector3(center.x - pylonWidth * 0.5f, supportTopY - 0.43f, center.y), 0.14f, materials.Frame, false);
            CreateBoxBetween("Upper Brace East", structure, new Vector3(center.x + pylonOffset, braceY, center.y), new Vector3(center.x + pylonWidth * 0.5f, supportTopY - 0.43f, center.y), 0.14f, materials.Frame, false);
        }

        private static void CreateTwinCantileverSupports(
            Transform deck,
            Vector2 center,
            Vector2 deckSize,
            float topY,
            MapMaterials materials)
        {
            Transform structure = CreateGroup("Twin Cantilever Supports", deck).transform;
            float floorY = GetLocalFactoryFloorY(deck, center);
            float supportTopY = topY - 0.45f;
            float towerX = center.x - deckSize.x * 0.36f;
            float towerOffsetZ = deckSize.y * 0.27f;
            float armEndX = center.x + deckSize.x * 0.28f;
            float towerWidth = Mathf.Clamp(Mathf.Min(deckSize.x, deckSize.y) * 0.16f, 0.7f, 0.95f);

            for (int side = -1; side <= 1; side += 2)
            {
                float z = center.y + side * towerOffsetZ;
                Transform tower = CreateGroup(side < 0 ? "South Tower" : "North Tower", structure).transform;
                CreatePrimaryPylon(tower, towerX, z, floorY, supportTopY, towerWidth, materials);
                CreateBoxBetween(
                    "Cantilever Arm",
                    tower,
                    new Vector3(towerX, supportTopY - 0.12f, z),
                    new Vector3(armEndX, supportTopY - 0.12f, z),
                    0.28f,
                    materials.Steel,
                    true);
                float kneeBottomY = Mathf.Max(floorY + 0.75f, supportTopY - 2.05f);
                CreateBoxBetween(
                    "Triangular Knee Brace",
                    tower,
                    new Vector3(towerX + towerWidth * 0.36f, kneeBottomY, z),
                    new Vector3(armEndX - deckSize.x * 0.08f, supportTopY - 0.30f, z),
                    0.18f,
                    materials.Frame,
                    false);
            }
        }

        private static void CreatePrimaryPylon(
            Transform parent,
            float x,
            float z,
            float floorY,
            float supportTopY,
            float width,
            MapMaterials materials)
        {
            const float footingHeight = 0.32f;
            float coreBottomY = floorY + footingHeight * 0.7f;
            float coreTopY = supportTopY - 0.22f;
            float coreHeight = Mathf.Max(0.25f, coreTopY - coreBottomY);
            CreateBox("Footing", parent, new Vector3(x, floorY + footingHeight * 0.5f, z), new Vector3(width * 1.9f, footingHeight, width * 1.9f), materials.Frame, true);
            CreateBox("Load Core", parent, new Vector3(x, coreBottomY + coreHeight * 0.5f, z), new Vector3(width, coreHeight, width), materials.Dark, true);
            CreateBox("Upper Collar", parent, new Vector3(x, supportTopY - 0.28f, z), new Vector3(width * 1.42f, 0.24f, width * 1.42f), materials.Frame, true);
        }

        private static float GetLocalFactoryFloorY(Transform deck, Vector2 localCenter)
        {
            Vector3 worldProbe = deck.TransformPoint(new Vector3(localCenter.x, 0f, localCenter.y));
            worldProbe.y = 0f;
            return deck.InverseTransformPoint(worldProbe).y;
        }

        private static HoverDeckBuildData CreateHoverIndustrialDeck(
            string name,
            Transform parent,
            Vector2 center,
            Vector2 size,
            float topY,
            MapMaterials materials,
            float phaseOffset)
        {
            GameObject root = CreateGroup(name, parent);
            Transform visualRoot = CreateGroup("Visual Body", root.transform).transform;

            CreateBox(
                "Walkable Grating",
                visualRoot,
                new Vector3(center.x, topY - 0.16f, center.y),
                new Vector3(size.x, 0.32f, size.y),
                materials.Deck,
                false);
            CreateBox(
                "Orange Underframe",
                visualRoot,
                new Vector3(center.x, topY - 0.38f, center.y),
                new Vector3(size.x + 0.18f, 0.16f, size.y + 0.18f),
                materials.Frame,
                false);
            CreateBox(
                "Steel Center Spine",
                visualRoot,
                new Vector3(center.x, topY - 0.48f, center.y),
                new Vector3(Mathf.Max(0.25f, size.x - 0.6f), 0.16f, 0.28f),
                materials.Steel,
                false);

            CreateFixedBoxCollider(
                "Walkable Collision",
                root.transform,
                new Vector3(center.x, topY - 0.16f, center.y),
                new Vector3(size.x, 0.32f, size.y));
            CreateFixedBoxCollider(
                "Underframe Collision",
                root.transform,
                new Vector3(center.x, topY - 0.38f, center.y),
                new Vector3(size.x + 0.18f, 0.16f, size.y + 0.18f));

            Transform hoverAssembly = CreateGroup("Hover Assembly", visualRoot).transform;
            float emitterRadius = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.23f, 0.54f, 0.72f);
            CreateCylinder(
                "Central Repulsor Housing",
                hoverAssembly,
                new Vector3(center.x, topY - 0.72f, center.y),
                new Vector3(emitterRadius + 0.16f, 0.14f, emitterRadius + 0.16f),
                Quaternion.identity,
                materials.Dark,
                false);
            CreateCylinder(
                "Steel Lift Coil",
                hoverAssembly,
                new Vector3(center.x, topY - 0.91f, center.y),
                new Vector3(emitterRadius + 0.08f, 0.065f, emitterRadius + 0.08f),
                Quaternion.identity,
                materials.Steel,
                false);
            CreateCylinder(
                "Cyan Lift Ring",
                hoverAssembly,
                new Vector3(center.x, topY - 0.995f, center.y),
                new Vector3(emitterRadius, 0.035f, emitterRadius),
                Quaternion.identity,
                materials.Energy,
                false);
            CreateCylinder(
                "Lift Ring Center Mask",
                hoverAssembly,
                new Vector3(center.x, topY - 1.04f, center.y),
                new Vector3(emitterRadius * 0.56f, 0.04f, emitterRadius * 0.56f),
                Quaternion.identity,
                materials.Dark,
                false);

            Transform ringSpinner = CreateGroup("Repulsor Ring Spinner", hoverAssembly).transform;
            ringSpinner.localPosition = new Vector3(center.x, topY - 1.09f, center.y);
            for (int index = 0; index < 3; index++)
            {
                float angle = index * 120f;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 markerPosition = new Vector3(
                    Mathf.Cos(radians) * emitterRadius * 0.76f,
                    0f,
                    Mathf.Sin(radians) * emitterRadius * 0.76f);
                CreateBox(
                    "Energy Segment " + (index + 1),
                    ringSpinner,
                    markerPosition,
                    new Vector3(emitterRadius * 0.34f, 0.055f, 0.1f),
                    materials.Energy,
                    false,
                    Quaternion.Euler(0f, -angle, 0f));
            }

            bool stabilizersAlongX = size.x >= size.y;
            float stabilizerOffset = (stabilizersAlongX ? size.x : size.y) * 0.29f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 stabilizerPosition = new Vector3(
                    center.x + (stabilizersAlongX ? side * stabilizerOffset : 0f),
                    topY - 0.67f,
                    center.y + (stabilizersAlongX ? 0f : side * stabilizerOffset));
                CreateCylinder(
                    side < 0 ? "Stabilizer Housing A" : "Stabilizer Housing B",
                    hoverAssembly,
                    stabilizerPosition,
                    new Vector3(0.24f, 0.11f, 0.24f),
                    Quaternion.identity,
                    materials.Dark,
                    false);
                CreateCylinder(
                    side < 0 ? "Stabilizer Glow A" : "Stabilizer Glow B",
                    hoverAssembly,
                    stabilizerPosition + Vector3.down * 0.14f,
                    new Vector3(0.15f, 0.03f, 0.15f),
                    Quaternion.identity,
                    materials.Energy,
                    false);
            }

            ParticleSystem hoverParticles = CreateHoverParticles(
                hoverAssembly,
                new Vector3(center.x, topY - 1.12f, center.y),
                emitterRadius * 0.55f,
                materials.EnergyParticle);
            Light hoverLight = CreateHoverLight(
                hoverAssembly,
                new Vector3(center.x, topY - 1.08f, center.y));

            BoxCollider landingTrigger = root.AddComponent<BoxCollider>();
            landingTrigger.center = new Vector3(center.x, topY + 0.44f, center.y);
            landingTrigger.size = new Vector3(
                Mathf.Max(0.4f, size.x - 0.16f),
                0.92f,
                Mathf.Max(0.4f, size.y - 0.16f));
            landingTrigger.isTrigger = true;

            HoverPlatformPresentation presentation = root.AddComponent<HoverPlatformPresentation>();
            presentation.Configure(
                visualRoot,
                landingTrigger,
                ringSpinner,
                hoverLight,
                hoverParticles,
                phaseOffset);

            return new HoverDeckBuildData(root, visualRoot);
        }

        private static void CreateFixedBoxCollider(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size)
        {
            GameObject collision = CreateGroup(name, parent);
            collision.transform.localPosition = localPosition;
            BoxCollider collider = collision.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static ParticleSystem CreateHoverParticles(
            Transform parent,
            Vector3 localPosition,
            float radius,
            Material material)
        {
            GameObject effect = CreateGroup("Hover Energy Plume", parent);
            effect.transform.localPosition = localPosition;
            effect.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.08f, 0.72f, 1f, 0.62f),
                new Color(0.34f, 0.92f, 1f, 0.28f));
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = radius;
            shape.length = 0.12f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.4f, 1f),
                    new Keyframe(1f, 0f)));

            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.8f;
            renderer.velocityScale = 0.45f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static Light CreateHoverLight(Transform parent, Vector3 localPosition)
        {
            GameObject lightObject = CreateGroup("Hover Underglow", parent);
            lightObject.transform.localPosition = localPosition;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.08f, 0.76f, 1f);
            light.intensity = 2.1f;
            light.range = 2.4f;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void SetDirectChildSupportPostGeometry(Transform parent, float y, float height)
        {
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                Transform child = parent.GetChild(childIndex);
                if (!child.name.StartsWith("Support Post", StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 position = child.localPosition;
                position.y = y;
                child.localPosition = position;

                Vector3 scale = child.localScale;
                scale.y = height;
                child.localScale = scale;
            }
        }

        private static void AddSupportTier(
            Transform parent,
            float y,
            float height,
            int firstSuffix,
            float[] xPositions,
            float[] zPositions,
            MapMaterials materials)
        {
            int suffix = firstSuffix;
            foreach (float x in xPositions)
            {
                foreach (float z in zPositions)
                {
                    AddSupportPost(
                        "Support Post (" + suffix + ")",
                        parent,
                        new Vector3(x, y, z),
                        new Vector3(0.32f, height, 0.32f),
                        materials);
                    suffix++;
                }
            }
        }

        private static void AddSupportPost(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            MapMaterials materials)
        {
            CreateBox(name, parent, localPosition, localScale, materials.Frame, true);
        }

        private static void RemoveSupportPostAt(Transform parent, Vector3 localPosition)
        {
            const float positionTolerance = 0.0001f;
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (!child.name.StartsWith("Support Post", StringComparison.Ordinal) ||
                    (child.localPosition - localPosition).sqrMagnitude > positionTolerance * positionTolerance)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                return;
            }

            throw new InvalidOperationException(
                "Could not remove the generated support post at " + localPosition + " from " + parent.name + ".");
        }

        private static void AddSafetyRail(Transform parent, Vector3 center, float length, bool alongX, MapMaterials materials)
        {
            Vector3 barScale = alongX
                ? new Vector3(length, 0.12f, 0.12f)
                : new Vector3(0.12f, 0.12f, length);
            CreateBox("Safety Rail Top", parent, center, barScale, materials.Frame, false);
            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 2.4f) + 1);
            for (int index = 0; index < posts; index++)
            {
                float offset = Mathf.Lerp(-length * 0.5f, length * 0.5f, posts == 1 ? 0f : index / (float)(posts - 1));
                Vector3 position = alongX
                    ? center + new Vector3(offset, -0.38f, 0f)
                    : center + new Vector3(0f, -0.38f, offset);
                CreateBox("Safety Rail Post", parent, position, new Vector3(0.12f, 0.76f, 0.12f), materials.Frame, false);
            }
        }

        private static void AddDeckCornerBrackets(Transform parent, Vector3 center, Vector2 size, float height, MapMaterials materials)
        {
            foreach (float xSign in new[] { -1f, 1f })
            {
                foreach (float zSign in new[] { -1f, 1f })
                {
                    CreateBox(
                        "Corner Bracket",
                        parent,
                        center + new Vector3(xSign * size.x * 0.46f, height * 0.5f, zSign * size.y * 0.46f),
                        new Vector3(0.16f, height, 0.16f),
                        materials.Frame,
                        false);
                }
            }
        }

        private static FactoryMachineHealth AddMachineCombat(
            GameObject root,
            string machineName,
            int maximumHealth,
            float repairDuration,
            Vector3 targetPointPosition,
            Renderer[] statusRenderers,
            GameObject brokenMarker = null)
        {
            Health health = root.AddComponent<Health>();
            FactionMember factionMember = root.AddComponent<FactionMember>();
            Targetable targetable = root.AddComponent<Targetable>();
            FactoryMachineHealth machineHealth = root.AddComponent<FactoryMachineHealth>();

            GameObject targetPointObject = CreateGroup("Target Point", root.transform);
            targetPointObject.transform.localPosition = targetPointPosition;
            TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();

            targetable.Configure(factionMember, targetPoint, machineHealth, true);
            machineHealth.Configure(
                machineName,
                maximumHealth,
                repairDuration,
                health,
                factionMember,
                targetable,
                statusRenderers,
                brokenMarker);
            return machineHealth;
        }

        private static GameObject CreateBrokenMarker(
            Transform parent,
            Vector3 localPosition,
            Material material)
        {
            GameObject marker = CreateGroup("Broken Machine Marker", parent);
            marker.transform.localPosition = localPosition;
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Red Alert Beacon",
                marker.transform,
                Vector3.zero,
                Vector3.one * 0.62f,
                Quaternion.identity,
                material,
                false);
            CreateBox(
                "Alert Stem",
                marker.transform,
                new Vector3(0f, -0.62f, 0f),
                new Vector3(0.16f, 0.58f, 0.16f),
                material,
                false);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Alert Point",
                marker.transform,
                new Vector3(0f, -1.02f, 0f),
                Vector3.one * 0.22f,
                Quaternion.identity,
                material,
                false);
            marker.SetActive(false);
            return marker;
        }

        private static GameObject CreateMaintenanceDoor(
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            MapMaterials materials,
            bool open,
            MaintenanceDoorLayout layout)
        {
            GameObject root = CreateGroup(name, parent);
            root.transform.SetPositionAndRotation(position, rotation);
            CreateBox(
                open ? "Raised Door Slab" : "Door Slab",
                root.transform,
                open ? new Vector3(0f, layout.SlabY, layout.SlabZ) : Vector3.zero,
                new Vector3(3.2f, 3.1f, 0.24f) * layout.Scale,
                materials.Dark,
                !open);
            CreateBox("Door Frame Left", root.transform, new Vector3(-1.72f * layout.Scale, layout.SideFrameY, -0.02f), new Vector3(0.26f, 3.5f, 0.34f) * layout.Scale, materials.Frame, true);
            CreateBox("Door Frame Right", root.transform, new Vector3(1.72f * layout.Scale, layout.SideFrameY, -0.02f), new Vector3(0.26f, 3.5f, 0.34f) * layout.Scale, materials.Frame, true);
            CreateBox("Door Frame Top", root.transform, new Vector3(0f, layout.TopFrameY, -0.02f), new Vector3(3.6f, 0.28f, 0.34f) * layout.Scale, materials.Frame, true);
            CreateBox("Door Warning Stripe", root.transform, new Vector3(0f, -0.45f, -0.18f), new Vector3(2.4f, 0.18f, 0.08f), materials.Hazard, false, Quaternion.Euler(0f, 0f, -12f));
            CreateBox("Dormant Status Light", root.transform, new Vector3(0f, 1.1f, -0.18f), new Vector3(0.6f, 0.14f, 0.08f), materials.Dark, false);
            return root;
        }

        private static GameObject PlacePrefab(string path, Vector3 position, Quaternion rotation, Transform parent, string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab: " + path);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static GameObject[] CompactObjects(params GameObject[] objects)
        {
            List<GameObject> compact = new List<GameObject>();
            foreach (GameObject item in objects)
            {
                if (item != null)
                {
                    compact.Add(item);
                }
            }

            return compact.ToArray();
        }

        private static GameObject FindDescendant(GameObject root, string name)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == name)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static GameObject CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, rotation ?? Quaternion.identity, material, collider);
        }

        private static GameObject CreateBoxBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float thickness,
            Material material,
            bool collider)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return null;
            }

            return CreateBox(
                name,
                parent,
                Vector3.Lerp(start, end, 0.5f),
                new Vector3(thickness, distance, thickness),
                material,
                collider,
                Quaternion.FromToRotation(Vector3.up, delta.normalized));
        }

        private static GameObject CreateCylinder(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material,
            bool collider)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, scale, rotation, material, collider);
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material,
            bool collider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
            gameObject.transform.localScale = scale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            if (!collider)
            {
                Collider existingCollider = gameObject.GetComponent<Collider>();
                if (existingCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingCollider);
                }
            }

            return gameObject;
        }

        private static GameObject CreatePipeBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float diameter,
            Material material,
            bool collider)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            GameObject pipe = CreateCylinder(
                name,
                parent,
                Vector3.Lerp(start, end, 0.5f),
                new Vector3(diameter, distance * 0.5f, diameter),
                Quaternion.FromToRotation(Vector3.up, delta.normalized),
                material,
                collider);
            return pipe;
        }

        private static Material RequireMaterial(string path, Color color, float metallic, float smoothness, Color emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            return material != null ? material : CreateOrUpdateMaterial(path, color, metallic, smoothness, emission);
        }

        private static Material CreateOrUpdateMaterial(string path, Color color, float metallic, float smoothness, Color emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emission);
                }
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existingIndex = scenes.FindIndex(scene => scene.path == ScenePath);
            if (existingIndex >= 0)
            {
                scenes[existingIndex] = new EditorBuildSettingsScene(ScenePath, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }

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

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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

        private sealed class PlayerRigSet
        {
            public PlayerRigSet(
                GameObject player,
                GameObject hud,
                Targetable targetable,
                PlayerHealth playerHealth,
                CharacterController characterController,
                ThirdPersonPlayerController playerController,
                PlayerInteractor playerInteractor,
                ThirdPersonOrbitCamera orbitCamera,
                Transform cameraTransform,
                PlayerStatusPresenter statusPresenter,
                FactoryGameOverController gameOverController,
                InputActionReference pauseAction,
                CameraShakeController cameraShake)
            {
                Player = player;
                Hud = hud;
                Targetable = targetable;
                PlayerHealth = playerHealth;
                CharacterController = characterController;
                PlayerController = playerController;
                PlayerInteractor = playerInteractor;
                OrbitCamera = orbitCamera;
                CameraTransform = cameraTransform;
                StatusPresenter = statusPresenter;
                GameOverController = gameOverController;
                PauseAction = pauseAction;
                CameraShake = cameraShake;
            }

            public GameObject Player { get; }
            public GameObject Hud { get; }
            public Targetable Targetable { get; }
            public PlayerHealth PlayerHealth { get; }
            public CharacterController CharacterController { get; }
            public ThirdPersonPlayerController PlayerController { get; }
            public PlayerInteractor PlayerInteractor { get; }
            public ThirdPersonOrbitCamera OrbitCamera { get; }
            public Transform CameraTransform { get; }
            public PlayerStatusPresenter StatusPresenter { get; }
            public FactoryGameOverController GameOverController { get; }
            public InputActionReference PauseAction { get; }
            public CameraShakeController CameraShake { get; }
        }

        private sealed class EnemyEntranceSet
        {
            public EnemyEntranceSet(EnemySpawnPoint mineDoor, EnemySpawnPoint generatorDoor)
            {
                MineDoor = mineDoor;
                GeneratorDoor = generatorDoor;
            }

            public EnemySpawnPoint MineDoor { get; }
            public EnemySpawnPoint GeneratorDoor { get; }
        }

        private readonly struct MaintenanceDoorLayout
        {
            public MaintenanceDoorLayout(
                float scale,
                float sideFrameY,
                float topFrameY,
                float slabY,
                float slabZ)
            {
                Scale = scale;
                SideFrameY = sideFrameY;
                TopFrameY = topFrameY;
                SlabY = slabY;
                SlabZ = slabZ;
            }

            public float Scale { get; }
            public float SideFrameY { get; }
            public float TopFrameY { get; }
            public float SlabY { get; }
            public float SlabZ { get; }
        }

        private sealed class ProductionConveyorRoute
        {
            public ProductionConveyorRoute(
                GameObject root,
                ConveyorBelt[] belts,
                Vector3[] routePoints)
            {
                Root = root;
                Belts = belts;
                RoutePoints = routePoints;
            }

            public GameObject Root { get; }
            public ConveyorBelt[] Belts { get; }
            public Vector3[] RoutePoints { get; }
        }

        private sealed class EnemyAccessRouteSet
        {
            private readonly EnemyAccessRouteBuildData _smelterRoute;
            private readonly EnemyAccessRouteBuildData _generatorRoute;
            private readonly EnemyAccessRouteBuildData _assemblerRoute;

            public EnemyAccessRouteSet(
                EnemyAccessRouteBuildData smelterRoute,
                EnemyAccessRouteBuildData generatorRoute,
                EnemyAccessRouteBuildData assemblerRoute)
            {
                _smelterRoute = smelterRoute;
                _generatorRoute = generatorRoute;
                _assemblerRoute = assemblerRoute;
            }

            public void Configure(
                FactoryObjectiveTerminal smelterTerminal,
                FactoryObjectiveTerminal generatorTerminal,
                FactoryObjectiveTerminal assemblerTerminal)
            {
                _smelterRoute.Configure(smelterTerminal);
                _generatorRoute.Configure(generatorTerminal);
                _assemblerRoute.Configure(assemblerTerminal);
            }
        }

        private sealed class EnemyAccessRouteBuildData
        {
            private readonly List<EnemyTraversalLink> _links = new List<EnemyTraversalLink>();
            private readonly List<Transform> _deploymentParts = new List<Transform>();
            private readonly List<Vector3> _retractedPositions = new List<Vector3>();

            public EnemyAccessRouteBuildData(Transform root, EnemyAccessRoute route)
            {
                Root = root;
                Route = route;
            }

            public Transform Root { get; }
            public EnemyAccessRoute Route { get; }

            public void Add(EnemyTraversalLink traversalLink)
            {
                if (traversalLink != null)
                {
                    _links.Add(traversalLink);
                }
            }

            public void Add(EnemyLadderBuildData ladder)
            {
                if (ladder == null)
                {
                    return;
                }

                Add(ladder.TraversalLink);
                _deploymentParts.AddRange(ladder.DeploymentParts);
                _retractedPositions.AddRange(ladder.RetractedPositions);
            }

            public void Configure(FactoryObjectiveTerminal terminal)
            {
                Route.Configure(
                    terminal,
                    _links.ToArray(),
                    _deploymentParts.ToArray(),
                    _retractedPositions.ToArray(),
                    1.35f);
            }
        }

        private sealed class EnemyLadderBuildData
        {
            public EnemyLadderBuildData(
                EnemyTraversalLink traversalLink,
                Transform[] deploymentParts,
                Vector3[] retractedPositions)
            {
                TraversalLink = traversalLink;
                DeploymentParts = deploymentParts;
                RetractedPositions = retractedPositions;
            }

            public EnemyTraversalLink TraversalLink { get; }
            public Transform[] DeploymentParts { get; }
            public Vector3[] RetractedPositions { get; }
        }

        private readonly struct HoverDeckBuildData
        {
            public HoverDeckBuildData(GameObject root, Transform visualRoot)
            {
                Root = root;
                VisualRoot = visualRoot;
            }

            public GameObject Root { get; }
            public Transform VisualRoot { get; }
        }

        private sealed class MapMaterials
        {
            public Material Frame;
            public Material Dark;
            public Material Steel;
            public Material Machine;
            public Material Energy;
            public Material EnergyParticle;
            public Material Furnace;
            public Material Active;
            public Material Broken;
            public Material Floor;
            public Material Deck;
            public Material Wall;
            public Material Hazard;
            public Material Ore;
        }
    }
}
