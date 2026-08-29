using System;
using System.Collections.Generic;
using PlatformerUltra.Audio;
using PlatformerUltra.Audio.Editor;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PlatformerUltra.Factory.Editor
{
    public static class FactoryMachineAssetFactory
    {
        private const string RootFolder = "Assets/Game/Factory";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string MeshFolder = RootFolder + "/Meshes";

        private const string ConveyorFramePath = RootFolder + "/Conveyors/Materials/M_Conveyor_Frame.mat";
        private const string ConveyorBeltPath = RootFolder + "/Conveyors/Materials/M_Conveyor_Belt.mat";
        private const string ConveyorAccentPath = RootFolder + "/Conveyors/Materials/M_Conveyor_Accent.mat";
        private const string MachineBodyMaterialPath = MaterialFolder + "/M_Factory_MachinePurple.mat";
        private const string MinerAudioPath = "Assets/Audio/Miner.wav";
        private const string CrusherAudioPath = "Assets/Audio/Crusher.mp3";
        private const string SmelterAudioPath = "Assets/Audio/IndustrialFireBUrning.mp3";
        private const string OreCargoPrefabPath = PrefabFolder + "/PF_Factory_OreCargo.prefab";
        private const string IngotCargoPrefabPath = PrefabFolder + "/PF_Factory_IngotCargo.prefab";
        private const string PortalComponentCargoPrefabPath = PrefabFolder + "/PF_Factory_PortalComponentCargo.prefab";

        [MenuItem("Tools/Factory/Build Machine Assets")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);

            FactoryMaterials materials = BuildMaterials();
            Mesh portalSegment = CreateOrUpdateMesh(
                MeshFolder + "/SM_Factory_PortalFrameSegment.asset",
                BuildPortalFrameSegmentMesh());
            Mesh portalCrystal = CreateOrUpdateMesh(
                MeshFolder + "/SM_Factory_PortalCoreCrystal.asset",
                BuildPortalCoreCrystalMesh());
            Mesh directionArrow = CreateOrUpdateMesh(
                MeshFolder + "/SM_Factory_DirectionArrow.asset",
                BuildDirectionArrowMesh());
            Mesh chamferedBlock = CreateOrUpdateMesh(
                MeshFolder + "/SM_Factory_ChamferedBlock.asset",
                BuildChamferedBlockMesh());
            Mesh taperedHousing = CreateOrUpdateMesh(
                MeshFolder + "/SM_Factory_TaperedHousing.asset",
                BuildTaperedHousingMesh());

            BuildMine(materials, chamferedBlock, taperedHousing);
            BuildGenerator(materials, chamferedBlock, taperedHousing);
            BuildSmelter(materials, chamferedBlock, taperedHousing);
            BuildAssembler(materials, chamferedBlock, taperedHousing);
            BuildCrusher(materials, chamferedBlock, taperedHousing);
            BuildPortal(materials, portalSegment, chamferedBlock);
            BuildPortalCore(materials, portalCrystal, chamferedBlock, taperedHousing);
            BuildRouter(materials, directionArrow, false);
            BuildRouter(materials, directionArrow, true);
            BuildProductionCargoPrefabs(materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "/PF_Factory_Smelter.prefab");
            Debug.Log("Factory machine assets built under Assets/Game/Factory/Prefabs.");
        }

        private static FactoryMaterials BuildMaterials()
        {
            Material frame = AssetDatabase.LoadAssetAtPath<Material>(ConveyorFramePath);
            Material belt = AssetDatabase.LoadAssetAtPath<Material>(ConveyorBeltPath);
            Material accent = AssetDatabase.LoadAssetAtPath<Material>(ConveyorAccentPath);

            if (frame == null)
            {
                frame = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_FrameFallback.mat",
                    new Color(0.95f, 0.38f, 0.045f),
                    0.55f,
                    0.42f,
                    Color.black);
            }

            if (belt == null)
            {
                belt = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_DarkFallback.mat",
                    new Color(0.045f, 0.055f, 0.06f),
                    0.05f,
                    0.26f,
                    Color.black);
            }

            if (accent == null)
            {
                accent = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_SteelFallback.mat",
                    new Color(0.46f, 0.56f, 0.62f),
                    0.72f,
                    0.68f,
                    new Color(0.03f, 0.09f, 0.12f));
            }

            return new FactoryMaterials
            {
                Frame = frame,
                Dark = belt,
                Steel = accent,
                Machine = CreateOrUpdateLitMaterial(
                    MachineBodyMaterialPath,
                    new Color(0.38f, 0.12f, 0.58f),
                    0.42f,
                    0.5f,
                    new Color(0.08f, 0.01f, 0.14f)),
                Furnace = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_EmissiveOrange.mat",
                    new Color(1f, 0.24f, 0.025f),
                    0.08f,
                    0.5f,
                    new Color(4.5f, 0.42f, 0.025f)),
                Energy = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_EmissiveCyan.mat",
                    new Color(0.08f, 0.72f, 0.92f),
                    0.16f,
                    0.78f,
                    new Color(0.12f, 2.5f, 4.8f)),
                Active = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_IndicatorGreen.mat",
                    new Color(0.12f, 0.85f, 0.42f),
                    0.12f,
                    0.62f,
                    new Color(0.05f, 1.8f, 0.35f)),
                Broken = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_BrokenMarker.mat",
                    new Color(1f, 0.035f, 0.02f),
                    0.05f,
                    0.68f,
                    new Color(5.5f, 0.03f, 0.01f)),
                OreCargo = CreateOrUpdateLitMaterial(
                    MaterialFolder + "/M_Factory_ProductionOre.mat",
                    new Color(0.08f, 0.28f, 0.34f),
                    0.7f,
                    0.32f,
                    new Color(0.01f, 0.18f, 0.24f)),
                Smoke = CreateOrUpdateParticleMaterial(
                    MaterialFolder + "/M_Factory_Smoke.mat",
                    new Color(0.18f, 0.21f, 0.23f, 0.62f)),
                EnergyParticle = CreateOrUpdateParticleMaterial(
                    MaterialFolder + "/M_Factory_EnergyParticle.mat",
                    new Color(0.08f, 0.78f, 1f, 0.78f)),
                Dust = CreateOrUpdateParticleMaterial(
                    MaterialFolder + "/M_Factory_Dust.mat",
                    new Color(0.32f, 0.38f, 0.4f, 0.58f)),
                Sparks = CreateOrUpdateParticleMaterial(
                    MaterialFolder + "/M_Factory_Sparks.mat",
                    new Color(1f, 0.48f, 0.06f, 0.92f)),
                Embers = CreateOrUpdateParticleMaterial(
                    MaterialFolder + "/M_Factory_Embers.mat",
                    new Color(1f, 0.2f, 0.025f, 0.85f))
            };
        }

        private static void BuildMine(
            FactoryMaterials materials,
            Mesh chamferedBlock,
            Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_Mine");
            try
            {
                CreateBox("Structural Base", root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(6.2f, 0.44f, 5.2f), materials.Frame);
                CreateMeshPart("Extractor Housing", root.transform, chamferedBlock, new Vector3(0f, 1.65f, 0.8f), new Vector3(3.8f, 2.85f, 2.5f), Quaternion.identity, materials.Machine);
                CreateMeshPart("Tapered Service Shell", root.transform, taperedHousing, new Vector3(0f, 2.03f, 0.12f), new Vector3(3.25f, 1.7f, 2.15f), Quaternion.identity, materials.Dark);
                CreateBox("Housing Service Panel", root.transform, new Vector3(0f, 1.8f, -0.49f), new Vector3(2.5f, 1.35f, 0.1f), materials.Steel);
                CreateBox("Panel Header", root.transform, new Vector3(0f, 2.5f, -0.56f), new Vector3(2.8f, 0.18f, 0.16f), materials.Frame);
                for (int index = -2; index <= 2; index++)
                {
                    CreateBox("Service Vent " + (index + 3), root.transform, new Vector3(index * 0.42f, 1.82f, -0.57f), new Vector3(0.24f, 0.78f, 0.08f), materials.Dark);
                }

                GameObject drillAssembly = new GameObject("Drill Assembly");
                drillAssembly.transform.SetParent(root.transform, false);
                drillAssembly.transform.localPosition = new Vector3(0f, 1.18f, -1.65f);
                CreateCylinder("Drill Drum", drillAssembly.transform, Vector3.zero, new Vector3(1.15f, 0.82f, 1.15f), Quaternion.Euler(0f, 0f, 90f), materials.Dark);
                CreateCylinder("Drill Hub Left", drillAssembly.transform, new Vector3(-0.9f, 0f, 0f), new Vector3(0.48f, 0.18f, 0.48f), Quaternion.Euler(0f, 0f, 90f), materials.Steel);
                CreateCylinder("Drill Hub Right", drillAssembly.transform, new Vector3(0.9f, 0f, 0f), new Vector3(0.48f, 0.18f, 0.48f), Quaternion.Euler(0f, 0f, 90f), materials.Steel);
                for (int index = 0; index < 10; index++)
                {
                    float angle = index * 36f * Mathf.Deg2Rad;
                    CreateMeshPart(
                        "Segmented Drill Tooth " + (index + 1),
                        drillAssembly.transform,
                        taperedHousing,
                        new Vector3(0f, Mathf.Cos(angle) * 1.18f, Mathf.Sin(angle) * 1.18f),
                        new Vector3(1.85f, 0.18f, 0.36f),
                        Quaternion.Euler(index * 36f, 0f, 0f),
                        materials.Steel);
                }

                CreateBox("Hopper Floor", root.transform, new Vector3(0f, 0.82f, 2f), new Vector3(3.4f, 0.24f, 1.4f), materials.Dark);
                CreateMeshPart("Hopper Left", root.transform, taperedHousing, new Vector3(-1.45f, 1.35f, 2f), new Vector3(0.24f, 1.3f, 1.6f), Quaternion.Euler(0f, 0f, -15f), materials.Machine);
                CreateMeshPart("Hopper Right", root.transform, taperedHousing, new Vector3(1.45f, 1.35f, 2f), new Vector3(0.24f, 1.3f, 1.6f), Quaternion.Euler(0f, 0f, 15f), materials.Machine);
                for (int index = -2; index <= 2; index++)
                {
                    CreateBox("Hopper Rib " + (index + 3), root.transform, new Vector3(index * 0.62f, 1.02f, 2.62f), new Vector3(0.1f, 0.75f, 0.12f), materials.Frame);
                }

                GameObject hydraulicPiston = CreateCylinder("Hydraulic Piston", root.transform, new Vector3(2.35f, 1.15f, 0.7f), new Vector3(0.46f, 0.72f, 0.46f), Quaternion.identity, materials.Steel);
                CreateCylinder("Hydraulic Tank", root.transform, new Vector3(2.35f, 1.15f, 0.7f), new Vector3(0.65f, 1.05f, 0.65f), Quaternion.identity, materials.Dark);
                CreateCylinder("Tank Band A", root.transform, new Vector3(2.35f, 0.55f, 0.7f), new Vector3(0.72f, 0.1f, 0.72f), Quaternion.identity, materials.Frame);
                CreateCylinder("Tank Band B", root.transform, new Vector3(2.35f, 1.75f, 0.7f), new Vector3(0.72f, 0.1f, 0.72f), Quaternion.identity, materials.Frame);
                CreateCylinderBetween("Hydraulic Feed", root.transform, new Vector3(2.35f, 2.2f, 0.7f), new Vector3(1.3f, 2.75f, 0.7f), 0.18f, materials.Steel);
                Renderer statusRenderer = CreateBox("Mine Status Beacon", root.transform, new Vector3(0f, 3.25f, 0.7f), new Vector3(0.85f, 0.16f, 0.28f), materials.Energy).GetComponent<Renderer>();

                MachineLoopAudio minerAudio = drillAssembly.AddComponent<MachineLoopAudio>();
                minerAudio.Configure(AssetDatabase.LoadAssetAtPath<AudioClip>(MinerAudioPath), 0.1f, 2.5f, 15f, 0.45f, false,
                    GameAudioAssetFactory.GetGroup(GameAudioAssetFactory.SfxGroupName));
                ParticleSystem startupFlash = CreateBurstParticleSystem("Mine Startup Flash", root.transform, new Vector3(0f, 2.15f, -1.45f), materials.EnergyParticle, new Color(0.18f, 0.9f, 1f, 0.9f), 20, 2.2f, 0.24f, 0.55f, ParticleSystemShapeType.Sphere, 0.45f, false);
                ParticleSystem startupDust = CreateBurstParticleSystem("Mine Startup Dust", root.transform, new Vector3(0f, 0.45f, -1.75f), materials.Dust, new Color(0.32f, 0.38f, 0.4f, 0.55f), 16, 1.25f, 0.42f, 1.05f, ParticleSystemShapeType.Cone, 0.55f, false);
                ParticleSystem workingDust = CreateLoopParticleSystem("Mine Working Dust", root.transform, new Vector3(0f, 0.5f, -1.78f), materials.Dust, new Color(0.34f, 0.4f, 0.42f, 0.48f), 10f, 0.75f, 0.34f, 0.8f, ParticleSystemShapeType.Cone, 0.6f, false);
                ParticleSystem outletBurst = CreateBurstParticleSystem("Mine Outlet Ore Burst", root.transform, new Vector3(0f, 1.05f, 2.65f), materials.Dust, new Color(0.18f, 0.55f, 0.62f, 0.8f), 14, 2f, 0.18f, 0.65f, ParticleSystemShapeType.Box, 0.4f, true);

                AddBoxCollider(root, new Vector3(0f, 0.22f, 0f), new Vector3(6.2f, 0.44f, 5.2f));
                AddBoxCollider(root, new Vector3(0f, 1.65f, 0.8f), new Vector3(3.8f, 2.85f, 2.5f));
                AddCapsuleCollider(root, new Vector3(0f, 1.18f, -1.65f), 1.12f, 2.3f, 0);
                AddBoxCollider(root, new Vector3(0f, 0.82f, 2f), new Vector3(3.4f, 0.24f, 1.4f));
                AddCapsuleCollider(root, new Vector3(2.35f, 1.15f, 0.7f), 0.48f, 2.1f, 1);

                GameObject brokenMarker = CreateBrokenMarker(root.transform, new Vector3(0f, 4.75f, 0f), materials.Broken);
                FactoryMachineHealth health = AddMachineCombat(root, "Mine", 120, 5f, new Vector3(0f, 2f, -2.75f), new[] { statusRenderer }, brokenMarker);
                FactoryMachinePresentation presentation = root.AddComponent<FactoryMachinePresentation>();
                presentation.Configure(
                    FactoryMachinePresentationKind.Mine,
                    health,
                    drillAssembly.transform,
                    new[] { drillAssembly.transform },
                    new[] { hydraulicPiston.transform },
                    new[] { statusRenderer.transform },
                    Array.Empty<Transform>(),
                    new[] { statusRenderer },
                    new Color(0.08f, 0.78f, 1f),
                    new[] { minerAudio },
                    new[] { startupFlash, startupDust },
                    Array.Empty<ParticleSystem>(),
                    new[] { workingDust },
                    Vector3.right,
                    Vector3.up,
                    18f,
                    210f,
                    0.1f,
                    0.018f,
                    1.05f,
                    new[] { outletBurst });
                SavePrefab(root, PrefabFolder + "/PF_Factory_Mine.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildGenerator(
            FactoryMaterials materials,
            Mesh chamferedBlock,
            Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_Generator");
            try
            {
                CreateBox("Generator Base", root.transform, new Vector3(0f, 0.24f, 0f), new Vector3(5.6f, 0.48f, 4.4f), materials.Frame);
                CreateMeshPart("Generator Housing", root.transform, chamferedBlock, new Vector3(0f, 1.55f, 0.2f), new Vector3(3.3f, 2.4f, 2.5f), Quaternion.identity, materials.Machine);
                CreateMeshPart("Upper Turbine Shroud", root.transform, taperedHousing, new Vector3(0f, 2.8f, 0.25f), new Vector3(2.35f, 1.25f, 1.85f), Quaternion.identity, materials.Dark);

                GameObject energyAssembly = new GameObject("Energy Assembly");
                energyAssembly.transform.SetParent(root.transform, false);
                GameObject leftDynamo = CreateDynamoRotor(energyAssembly.transform, "Left Dynamo Rotor", new Vector3(-1.55f, 1.55f, 0.1f), materials, chamferedBlock);
                GameObject rightDynamo = CreateDynamoRotor(energyAssembly.transform, "Right Dynamo Rotor", new Vector3(1.55f, 1.55f, 0.1f), materials, chamferedBlock);
                List<Transform> pulseParts = new List<Transform>();
                List<Renderer> emissives = new List<Renderer>();
                for (int index = -2; index <= 2; index++)
                {
                    GameObject coil = CreateCylinder("Energy Coil " + (index + 3), energyAssembly.transform, new Vector3(index * 0.45f, 1.55f, -1.28f), new Vector3(0.65f, 0.13f, 0.65f), Quaternion.Euler(90f, 0f, 0f), index == 0 ? materials.Energy : materials.Frame);
                    if (index == 0)
                    {
                        pulseParts.Add(coil.transform);
                        emissives.Add(coil.GetComponent<Renderer>());
                    }
                }

                GameObject turbine = new GameObject("Central Turbine Rotor");
                turbine.transform.SetParent(root.transform, false);
                turbine.transform.localPosition = new Vector3(0f, 2.95f, 0.25f);
                CreateCylinder("Turbine Housing", turbine.transform, Vector3.zero, new Vector3(0.82f, 0.5f, 0.82f), Quaternion.Euler(90f, 0f, 0f), materials.Machine);
                for (int index = 0; index < 8; index++)
                {
                    float angle = index * 45f;
                    CreateMeshPart("Turbine Fin " + (index + 1), turbine.transform, taperedHousing, Vector3.zero, new Vector3(0.14f, 0.78f, 0.28f), Quaternion.Euler(0f, 0f, angle), materials.Steel);
                }
                GameObject core = CreateCylinder("Turbine Core", turbine.transform, new Vector3(0f, 0f, -1.07f), new Vector3(0.45f, 0.2f, 0.45f), Quaternion.Euler(90f, 0f, 0f), materials.Energy);
                pulseParts.Add(core.transform);
                emissives.Add(core.GetComponent<Renderer>());
                CreateCylinderBetween("Exhaust Left", root.transform, new Vector3(-1.2f, 2.7f, 1.1f), new Vector3(-2.15f, 4.1f, 1.1f), 0.24f, materials.Steel);
                CreateCylinderBetween("Exhaust Right", root.transform, new Vector3(1.2f, 2.7f, 1.1f), new Vector3(2.15f, 4.1f, 1.1f), 0.24f, materials.Steel);
                CreateCylinder("Exhaust Crown Left", root.transform, new Vector3(-2.15f, 4.15f, 1.1f), new Vector3(0.38f, 0.12f, 0.38f), Quaternion.identity, materials.Frame);
                CreateCylinder("Exhaust Crown Right", root.transform, new Vector3(2.15f, 4.15f, 1.1f), new Vector3(0.38f, 0.12f, 0.38f), Quaternion.identity, materials.Frame);
                Renderer statusRenderer = CreateBox("Generator Status", root.transform, new Vector3(0f, 2.35f, -1.1f), new Vector3(1.5f, 0.18f, 0.12f), materials.Active).GetComponent<Renderer>();
                emissives.Add(statusRenderer);

                ParticleSystem startupEnergy = CreateBurstParticleSystem("Generator Startup Energy", root.transform, new Vector3(0f, 2.75f, -0.85f), materials.EnergyParticle, new Color(0.12f, 0.9f, 1f, 0.9f), 28, 2.4f, 0.2f, 0.65f, ParticleSystemShapeType.Circle, 1.15f, true);
                ParticleSystem startupSparks = CreateBurstParticleSystem("Generator Startup Sparks", root.transform, new Vector3(0f, 2.75f, -0.9f), materials.Sparks, new Color(0.45f, 0.95f, 1f, 0.9f), 18, 3.1f, 0.1f, 0.45f, ParticleSystemShapeType.Sphere, 0.55f, true);
                ParticleSystem idleMotes = CreateLoopParticleSystem("Generator Energy Motes", root.transform, new Vector3(0f, 2.6f, -0.9f), materials.EnergyParticle, new Color(0.1f, 0.8f, 1f, 0.62f), 5f, 0.35f, 0.13f, 1.4f, ParticleSystemShapeType.Circle, 1.25f, false);
                ParticleSystem loadSparks = CreateLoopParticleSystem("Generator Load Sparks", root.transform, new Vector3(0f, 2.65f, -0.95f), materials.EnergyParticle, new Color(0.32f, 0.95f, 1f, 0.86f), 12f, 1.35f, 0.09f, 0.55f, ParticleSystemShapeType.Circle, 1.15f, true);

                AddBoxCollider(root, new Vector3(0f, 0.24f, 0f), new Vector3(5.6f, 0.48f, 4.4f));
                AddBoxCollider(root, new Vector3(0f, 1.55f, 0.2f), new Vector3(3.3f, 2.4f, 2.5f));
                AddCapsuleCollider(root, new Vector3(-1.55f, 1.55f, 0.1f), 0.72f, 2f, 0);
                AddCapsuleCollider(root, new Vector3(1.55f, 1.55f, 0.1f), 0.72f, 2f, 0);
                AddCapsuleCollider(root, new Vector3(0f, 2.95f, 0.25f), 0.62f, 1.5f, 2);

                GameObject brokenMarker = CreateBrokenMarker(root.transform, new Vector3(0f, 5.35f, 0f), materials.Broken);
                FactoryMachineHealth health = AddMachineCombat(root, "Generator", 180, 5f, new Vector3(0f, 2f, -2.35f), new[] { statusRenderer }, brokenMarker);
                FactoryMachinePresentation presentation = root.AddComponent<FactoryMachinePresentation>();
                presentation.Configure(
                    FactoryMachinePresentationKind.Generator,
                    health,
                    energyAssembly.transform,
                    new[] { leftDynamo.transform, rightDynamo.transform, turbine.transform },
                    Array.Empty<Transform>(),
                    pulseParts.ToArray(),
                    Array.Empty<Transform>(),
                    emissives.ToArray(),
                    new Color(0.08f, 0.78f, 1f),
                    Array.Empty<MachineLoopAudio>(),
                    new[] { startupEnergy, startupSparks },
                    new[] { idleMotes },
                    new[] { loadSparks },
                    Vector3.up,
                    Vector3.up,
                    24f,
                    185f,
                    0f,
                    0.012f,
                    1.2f);
                SavePrefab(root, PrefabFolder + "/PF_Factory_Generator.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildSmelter(FactoryMaterials materials, Mesh chamferedBlock, Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_Smelter");
            try
            {
                CreateBox("Structural Base", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(5.4f, 0.5f, 4.2f), materials.Frame);
                CreateMeshPart("Furnace Housing", root.transform, chamferedBlock, new Vector3(0f, 1.8f, 0.25f), new Vector3(2.6f, 3.1f, 2.5f), Quaternion.identity, materials.Machine);
                CreateMeshPart("Upper Heat Shield", root.transform, taperedHousing, new Vector3(0f, 3.1f, 0.32f), new Vector3(2.35f, 0.85f, 2.2f), Quaternion.identity, materials.Dark);
                GameObject furnaceChamber = CreateBox("Furnace Chamber", root.transform, new Vector3(0f, 1.72f, -1.04f), new Vector3(1.55f, 1.3f, 0.14f), materials.Furnace);
                CreateBox("Chamber Frame Top", root.transform, new Vector3(0f, 2.44f, -1.17f), new Vector3(1.95f, 0.17f, 0.22f), materials.Frame);
                CreateBox("Chamber Frame Bottom", root.transform, new Vector3(0f, 1f, -1.17f), new Vector3(1.95f, 0.17f, 0.22f), materials.Frame);
                CreateBox("Chamber Frame Left", root.transform, new Vector3(-0.9f, 1.72f, -1.17f), new Vector3(0.17f, 1.6f, 0.22f), materials.Frame);
                CreateBox("Chamber Frame Right", root.transform, new Vector3(0.9f, 1.72f, -1.17f), new Vector3(0.17f, 1.6f, 0.22f), materials.Frame);

                CreateTank(root.transform, "Feed Tank Left", new Vector3(-1.85f, 1.45f, 0.7f), materials);
                CreateTank(root.transform, "Feed Tank Right", new Vector3(1.85f, 1.45f, 0.7f), materials);

                GameObject compressor = CreateCylinder(
                    "Compressor Body",
                    root.transform,
                    new Vector3(1.7f, 0.9f, -1.05f),
                    new Vector3(0.68f, 0.8f, 0.68f),
                    Quaternion.Euler(0f, 0f, 90f),
                    materials.Dark);
                CreateCylinder("Compressor End A", compressor.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.78f, 0.08f, 0.78f), Quaternion.identity, materials.Frame);
                CreateCylinder("Compressor End B", compressor.transform, new Vector3(0f, -0.9f, 0f), new Vector3(0.78f, 0.08f, 0.78f), Quaternion.identity, materials.Frame);
                for (int index = 0; index < 6; index++)
                {
                    CreateBox("Compressor Spoke " + (index + 1), compressor.transform, new Vector3(0f, -0.93f, 0f), new Vector3(0.1f, 0.05f, 0.62f), materials.Steel).transform.localRotation = Quaternion.Euler(0f, index * 60f, 0f);
                }
                GameObject pressureValve = CreateCylinder("Pressure Valve", root.transform, new Vector3(-1.85f, 2.5f, 0.7f), new Vector3(0.24f, 0.38f, 0.24f), Quaternion.identity, materials.Frame);

                CreatePipeRun(root.transform, new[]
                {
                    new Vector3(-1.85f, 2.75f, 0.7f),
                    new Vector3(-1.85f, 3.35f, 0.7f),
                    new Vector3(-1.15f, 3.35f, 0.7f)
                }, 0.18f, materials.Steel, "Left Feed Pipe");
                CreatePipeRun(root.transform, new[]
                {
                    new Vector3(1.85f, 2.75f, 0.7f),
                    new Vector3(1.85f, 3.35f, 0.7f),
                    new Vector3(1.15f, 3.35f, 0.7f)
                }, 0.18f, materials.Steel, "Right Feed Pipe");
                CreatePipeRun(root.transform, new[]
                {
                    new Vector3(1.7f, 0.9f, -0.4f),
                    new Vector3(2.35f, 0.9f, -0.4f),
                    new Vector3(2.35f, 2.2f, 0.15f)
                }, 0.16f, materials.Steel, "Compressor Pipe");

                CreateCylinder("Smoke Stack", root.transform, new Vector3(0f, 4.55f, 0.65f), new Vector3(0.58f, 1.7f, 0.58f), Quaternion.identity, materials.Dark);
                CreateCylinder("Stack Orange Band", root.transform, new Vector3(0f, 5.25f, 0.65f), new Vector3(0.68f, 0.12f, 0.68f), Quaternion.identity, materials.Frame);
                CreateCylinder("Stack Crown", root.transform, new Vector3(0f, 6.18f, 0.65f), new Vector3(0.76f, 0.14f, 0.76f), Quaternion.identity, materials.Steel);
                ParticleSystem smokePlume = CreateLoopParticleSystem(
                    "Smoke Plume",
                    root.transform,
                    new Vector3(0f, 6.35f, 0.65f),
                    materials.Smoke,
                    new Color(0.2f, 0.23f, 0.25f, 0.7f),
                    8f,
                    0.72f,
                    0.48f,
                    2.6f,
                    ParticleSystemShapeType.Cone,
                    0.32f,
                    false);
                MachineLoopAudio smelterAudio = smokePlume.gameObject.AddComponent<MachineLoopAudio>();
                smelterAudio.Configure(
                    AssetDatabase.LoadAssetAtPath<AudioClip>(SmelterAudioPath),
                    0.08f,
                    3f,
                    16f,
                    0.45f,
                    false,
                    GameAudioAssetFactory.GetGroup(GameAudioAssetFactory.SfxGroupName));
                ParticleSystem ignitionFlash = CreateBurstParticleSystem("Smelter Ignition Flash", root.transform, new Vector3(0f, 1.72f, -1.3f), materials.Embers, new Color(1f, 0.3f, 0.025f, 0.92f), 24, 2.6f, 0.22f, 0.55f, ParticleSystemShapeType.Box, 0.75f, true);
                ParticleSystem ignitionSmoke = CreateBurstParticleSystem("Smelter Ignition Smoke", root.transform, new Vector3(0f, 2.35f, -1.1f), materials.Smoke, new Color(0.25f, 0.27f, 0.28f, 0.55f), 12, 0.85f, 0.5f, 1.3f, ParticleSystemShapeType.Cone, 0.5f, false);
                ParticleSystem workingEmbers = CreateLoopParticleSystem("Smelter Working Embers", root.transform, new Vector3(0f, 1.65f, -1.35f), materials.Embers, new Color(1f, 0.26f, 0.025f, 0.88f), 14f, 1.4f, 0.1f, 0.65f, ParticleSystemShapeType.Box, 0.75f, true);

                AddBoxCollider(root, new Vector3(0f, 0.25f, 0f), new Vector3(5.4f, 0.5f, 4.2f));
                AddBoxCollider(root, new Vector3(0f, 1.8f, 0.25f), new Vector3(2.6f, 3.1f, 2.5f));
                AddCapsuleCollider(root, new Vector3(-1.85f, 1.45f, 0.7f), 0.47f, 2.6f, 1);
                AddCapsuleCollider(root, new Vector3(1.85f, 1.45f, 0.7f), 0.47f, 2.6f, 1);
                AddCapsuleCollider(root, new Vector3(1.7f, 0.9f, -1.05f), 0.42f, 1.8f, 0);
                AddCapsuleCollider(root, new Vector3(0f, 4.55f, 0.65f), 0.38f, 3.8f, 1);
                GameObject brokenMarker = CreateBrokenMarker(
                    root.transform,
                    new Vector3(0f, 7.35f, 0f),
                    materials.Broken);
                Renderer furnaceRenderer = furnaceChamber.GetComponent<Renderer>();
                FactoryMachineHealth health = AddMachineCombat(
                    root,
                    "Smelter",
                    140,
                    5f,
                    new Vector3(0f, 2.2f, -2.25f),
                    new[] { furnaceRenderer },
                    brokenMarker);
                FactoryMachinePresentation presentation = root.AddComponent<FactoryMachinePresentation>();
                presentation.Configure(
                    FactoryMachinePresentationKind.Smelter,
                    health,
                    compressor.transform,
                    new[] { compressor.transform },
                    new[] { pressureValve.transform },
                    new[] { furnaceChamber.transform },
                    new[] { pressureValve.transform },
                    new[] { furnaceRenderer },
                    new Color(1f, 0.24f, 0.025f),
                    new[] { smelterAudio },
                    new[] { ignitionFlash, ignitionSmoke },
                    new[] { smokePlume },
                    new[] { workingEmbers },
                    Vector3.up,
                    Vector3.up,
                    10f,
                    125f,
                    0.07f,
                    0.012f,
                    1.25f);
                SavePrefab(root, PrefabFolder + "/PF_Factory_Smelter.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildAssembler(FactoryMaterials materials, Mesh chamferedBlock, Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_Assembler");
            try
            {
                List<Renderer> statusRenderers = new List<Renderer>();
                CreateBox("Structural Base", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(5.5f, 0.5f, 4.3f), materials.Frame);
                CreateMeshPart("Machine Cabinet", root.transform, chamferedBlock, new Vector3(0f, 1.8f, 0.7f), new Vector3(4.6f, 3.1f, 1.8f), Quaternion.identity, materials.Machine);
                CreateMeshPart("Tapered Cabinet Crown", root.transform, taperedHousing, new Vector3(0f, 3.25f, 0.7f), new Vector3(4.25f, 0.55f, 1.65f), Quaternion.identity, materials.Dark);
                for (int index = -1; index <= 1; index++)
                {
                    float x = index * 1.35f;
                    CreateMeshPart("Chamfered Modular Panel " + (index + 2), root.transform, chamferedBlock, new Vector3(x, 1.95f, -0.23f), new Vector3(1.12f, 1.55f, 0.12f), Quaternion.identity, materials.Steel);
                    CreateBox("Panel Header " + (index + 2), root.transform, new Vector3(x, 2.64f, -0.34f), new Vector3(1.2f, 0.16f, 0.16f), materials.Frame);
                    GameObject statusLight = CreateBox("Status Light " + (index + 2), root.transform, new Vector3(x, 1.5f, -0.36f), new Vector3(0.55f, 0.12f, 0.12f), index == 0 ? materials.Energy : materials.Active);
                    statusRenderers.Add(statusLight.GetComponent<Renderer>());
                }

                GameObject leftArm = CreateRobotArm(root.transform, "Left Robotic Arm", -1f, materials, chamferedBlock);
                GameObject rightArm = CreateRobotArm(root.transform, "Right Robotic Arm", 1f, materials, chamferedBlock);

                CreateBox("Output Cradle Deck", root.transform, new Vector3(0f, 0.72f, -1.55f), new Vector3(2.7f, 0.18f, 1.35f), materials.Dark);
                CreateBox("Output Cradle Left Rail", root.transform, new Vector3(-1.35f, 0.92f, -1.55f), new Vector3(0.16f, 0.42f, 1.45f), materials.Frame);
                CreateBox("Output Cradle Right Rail", root.transform, new Vector3(1.35f, 0.92f, -1.55f), new Vector3(0.16f, 0.42f, 1.45f), materials.Frame);
                List<Transform> rollers = new List<Transform>();
                for (int index = 0; index < 5; index++)
                {
                    GameObject roller = CreateCylinder(
                        "Output Roller " + (index + 1),
                        root.transform,
                        new Vector3(0f, 0.84f, -2.02f + index * 0.24f),
                        new Vector3(0.12f, 1.24f, 0.12f),
                        Quaternion.Euler(0f, 0f, 90f),
                        materials.Steel);
                    rollers.Add(roller.transform);
                }

                ParticleSystem startupStatus = CreateBurstParticleSystem("Assembler Startup Chase", root.transform, new Vector3(0f, 2f, -0.65f), materials.EnergyParticle, new Color(0.12f, 0.9f, 1f, 0.9f), 18, 1.4f, 0.16f, 0.55f, ParticleSystemShapeType.Box, 1.25f, false);
                ParticleSystem startupServo = CreateBurstParticleSystem("Assembler Servo Dust", root.transform, new Vector3(0f, 0.75f, -1.55f), materials.Dust, new Color(0.35f, 0.4f, 0.42f, 0.48f), 10, 0.65f, 0.3f, 0.85f, ParticleSystemShapeType.Box, 0.9f, false);
                ParticleSystem weldingSparks = CreateLoopParticleSystem("Assembler Welding Sparks", root.transform, new Vector3(0f, 1.05f, -1.62f), materials.Sparks, new Color(1f, 0.48f, 0.06f, 0.95f), 16f, 2.7f, 0.09f, 0.42f, ParticleSystemShapeType.Cone, 0.28f, true);
                ParticleSystem completionFlash = CreateBurstParticleSystem("Assembler Completion Flash", root.transform, new Vector3(0f, 1.05f, -2.05f), materials.EnergyParticle, new Color(0.35f, 1f, 0.75f, 0.92f), 18, 1.6f, 0.2f, 0.5f, ParticleSystemShapeType.Sphere, 0.35f, false);

                AddBoxCollider(root, new Vector3(0f, 0.25f, 0f), new Vector3(5.5f, 0.5f, 4.3f));
                AddBoxCollider(root, new Vector3(0f, 1.8f, 0.7f), new Vector3(4.6f, 3.1f, 1.8f));
                AddBoxCollider(root, new Vector3(0f, 0.72f, -1.55f), new Vector3(2.7f, 0.18f, 1.35f));
                GameObject brokenMarker = CreateBrokenMarker(
                    root.transform,
                    new Vector3(0f, 4.75f, 0f),
                    materials.Broken);
                FactoryMachineHealth health = AddMachineCombat(
                    root,
                    "Assembler",
                    160,
                    5f,
                    new Vector3(0f, 1.8f, -2.25f),
                    statusRenderers.ToArray(),
                    brokenMarker);
                FactoryMachinePresentation presentation = root.AddComponent<FactoryMachinePresentation>();
                presentation.Configure(
                    FactoryMachinePresentationKind.Assembler,
                    health,
                    leftArm.transform,
                    rollers.ToArray(),
                    Array.Empty<Transform>(),
                    Array.ConvertAll(statusRenderers.ToArray(), renderer => renderer.transform),
                    new[] { leftArm.transform, rightArm.transform },
                    statusRenderers.ToArray(),
                    new Color(0.08f, 0.78f, 1f),
                    Array.Empty<MachineLoopAudio>(),
                    new[] { startupStatus, startupServo },
                    Array.Empty<ParticleSystem>(),
                    new[] { weldingSparks },
                    Vector3.up,
                    Vector3.up,
                    12f,
                    165f,
                    0f,
                    0.008f,
                    1.15f,
                    new[] { completionFlash });
                SavePrefab(root, PrefabFolder + "/PF_Factory_Assembler.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildCrusher(FactoryMaterials materials, Mesh chamferedBlock, Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_Crusher");
            try
            {
                CreateMeshPart("Anvil Base", root.transform, chamferedBlock, new Vector3(0f, 0.3f, 0f), new Vector3(4.5f, 0.6f, 3.7f), Quaternion.identity, materials.Machine);
                CreateBox("Anvil Surface", root.transform, new Vector3(0f, 0.68f, 0f), new Vector3(3.45f, 0.18f, 2.65f), materials.Steel);
                float[] xs = { -1.85f, 1.85f };
                float[] zs = { -1.45f, 1.45f };
                foreach (float x in xs)
                {
                    foreach (float z in zs)
                    {
                        CreateBox("Frame Post", root.transform, new Vector3(x, 2.35f, z), new Vector3(0.32f, 3.9f, 0.32f), materials.Frame);
                        CreateBox("Post Guide", root.transform, new Vector3(x * 0.94f, 2.35f, z * 0.94f), new Vector3(0.12f, 3.35f, 0.12f), materials.Steel);
                    }
                }

                GameObject topBeam = CreateMeshPart("Chamfered Top Beam", root.transform, chamferedBlock, new Vector3(0f, 4.3f, 0f), new Vector3(4.4f, 0.48f, 3.55f), Quaternion.identity, materials.Frame);
                GameObject hydraulicRam = CreateCylinder("Telescoping Hydraulic Ram", root.transform, new Vector3(0f, 3.65f, 0f), new Vector3(0.55f, 0.75f, 0.55f), Quaternion.identity, materials.Steel);
                CreateCylinder("Ram Collar", root.transform, new Vector3(0f, 4.05f, 0f), new Vector3(0.78f, 0.18f, 0.78f), Quaternion.identity, materials.Dark);
                CreateCylinderBetween("Hydraulic Hose Left", root.transform, new Vector3(-1.75f, 4.05f, 1.28f), new Vector3(-0.55f, 3.55f, 0.55f), 0.12f, materials.Dark);
                CreateCylinderBetween("Hydraulic Hose Right", root.transform, new Vector3(1.75f, 4.05f, 1.28f), new Vector3(0.55f, 3.55f, 0.55f), 0.12f, materials.Dark);
                GameObject plate = CreateMeshPart("Crushing Plate", root.transform, chamferedBlock, new Vector3(0f, 3.05f, 0f), new Vector3(3.35f, 0.42f, 2.55f), Quaternion.identity, materials.Machine);
                plate.AddComponent<BoxCollider>();
                CreateBox("Crushing Plate Hazard Face", plate.transform, new Vector3(0f, -0.55f, 0f), new Vector3(0.92f, 0.16f, 0.92f), materials.Furnace);
                for (int index = -2; index <= 2; index++)
                {
                    CreateMeshPart("Crusher Tooth " + (index + 3), plate.transform, taperedHousing, new Vector3(index * 0.42f, -0.72f, 0f), new Vector3(0.2f, 0.42f, 0.82f), Quaternion.identity, materials.Steel);
                }

                ParticleSystem impactDust = CreateBurstParticleSystem("Crusher Impact Dust", root.transform, new Vector3(0f, 0.82f, 0f), materials.Dust, new Color(0.35f, 0.4f, 0.42f, 0.62f), 24, 2f, 0.45f, 0.95f, ParticleSystemShapeType.Box, 1.25f, false);
                ParticleSystem impactSparks = CreateBurstParticleSystem("Crusher Impact Sparks", root.transform, new Vector3(0f, 0.9f, 0f), materials.Sparks, new Color(1f, 0.48f, 0.06f, 0.95f), 18, 3.4f, 0.09f, 0.42f, ParticleSystemShapeType.Box, 1.1f, true);
                FactoryCrusherVisual visual = root.AddComponent<FactoryCrusherVisual>();
                visual.Configure(plate.transform, hydraulicRam.transform, topBeam.transform, new[] { impactDust, impactSparks }, 3.05f, 1.05f, 1.5f);
                MachineLoopAudio crusherAudio = root.AddComponent<MachineLoopAudio>();
                crusherAudio.Configure(
                    AssetDatabase.LoadAssetAtPath<AudioClip>(CrusherAudioPath),
                    0.11f,
                    2.5f,
                    13f,
                    0.2f,
                    true,
                    GameAudioAssetFactory.GetGroup(GameAudioAssetFactory.SfxGroupName));
                AddBoxCollider(root, new Vector3(0f, 0.3f, 0f), new Vector3(4.5f, 0.6f, 3.7f));
                foreach (float x in xs)
                {
                    foreach (float z in zs)
                    {
                        AddBoxCollider(root, new Vector3(x, 2.35f, z), new Vector3(0.32f, 3.9f, 0.32f));
                    }
                }

                AddBoxCollider(root, new Vector3(0f, 4.3f, 0f), new Vector3(4.4f, 0.48f, 3.55f));
                SavePrefab(root, PrefabFolder + "/PF_Factory_Crusher.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPortal(FactoryMaterials materials, Mesh segmentMesh, Mesh chamferedBlock)
        {
            GameObject root = new GameObject("PF_Factory_Portal");
            try
            {
                CreateBox("Portal Base", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(5.8f, 0.7f, 2.2f), materials.Dark);
                CreateBox("Base Orange Rail", root.transform, new Vector3(0f, 0.73f, -0.86f), new Vector3(5.5f, 0.18f, 0.22f), materials.Frame);

                GameObject frameRoot = new GameObject("Custom Octagonal Frame");
                frameRoot.transform.SetParent(root.transform, false);
                frameRoot.transform.localPosition = new Vector3(0f, 3.15f, 0f);
                for (int index = 0; index < 8; index++)
                {
                    GameObject segment = CreateMeshObject(
                        "Frame Segment " + (index + 1),
                        frameRoot.transform,
                        segmentMesh,
                        materials.Frame);
                    segment.transform.localRotation = Quaternion.Euler(0f, 0f, index * 45f);
                    float angle = index * 45f * Mathf.Deg2Rad;
                    CreateCylinder(
                        "Frame Bolt " + (index + 1),
                        frameRoot.transform,
                        new Vector3(Mathf.Cos(angle) * 2.22f, Mathf.Sin(angle) * 2.22f, -0.36f),
                        new Vector3(0.18f, 0.08f, 0.18f),
                        Quaternion.Euler(90f, 0f, 0f),
                        materials.Steel);
                    CreateMeshPart(
                        "Frame Brace " + (index + 1),
                        frameRoot.transform,
                        chamferedBlock,
                        new Vector3(Mathf.Cos(angle) * 2.55f, Mathf.Sin(angle) * 2.55f, 0.12f),
                        new Vector3(0.36f, 0.72f, 0.38f),
                        Quaternion.Euler(0f, 0f, index * 45f),
                        materials.Dark);
                }

                CreateBox("Left Frame Foot", root.transform, new Vector3(-2.35f, 1.05f, 0f), new Vector3(0.75f, 1.1f, 1.45f), materials.Frame);
                CreateBox("Right Frame Foot", root.transform, new Vector3(2.35f, 1.05f, 0f), new Vector3(0.75f, 1.1f, 1.45f), materials.Frame);

                GameObject energyRoot = new GameObject("Portal Energy - Activation State Visual");
                energyRoot.transform.SetParent(root.transform, false);
                energyRoot.transform.localPosition = new Vector3(0f, 3.15f, 0f);
                GameObject outerEnergyRing = new GameObject("Counter Rotating Outer Ring");
                outerEnergyRing.transform.SetParent(energyRoot.transform, false);
                CreatePortalRing(outerEnergyRing.transform, 1.78f, 16, materials.Frame);
                GameObject innerEnergyRing = new GameObject("Counter Rotating Inner Ring");
                innerEnergyRing.transform.SetParent(energyRoot.transform, false);
                innerEnergyRing.transform.localRotation = Quaternion.Euler(0f, 0f, 11.25f);
                CreatePortalRing(innerEnergyRing.transform, 1.28f, 12, materials.Steel);
                CreateCylinder(
                    "Emissive Energy Chamber",
                    energyRoot.transform,
                    Vector3.zero,
                    new Vector3(3.65f, 0.035f, 3.65f),
                    Quaternion.Euler(90f, 0f, 0f),
                    materials.Energy);
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * 60f * Mathf.Deg2Rad;
                    CreateBox(
                        "Energy Spoke " + (index + 1),
                        energyRoot.transform,
                        new Vector3(Mathf.Cos(angle) * 0.95f, Mathf.Sin(angle) * 0.95f, -0.08f),
                        new Vector3(0.12f, 1.75f, 0.08f),
                        materials.Energy).transform.localRotation = Quaternion.Euler(0f, 0f, 90f - index * 60f);
                }

                ParticleSystem energyParticles = CreateLoopParticleSystem(
                    "Portal Energy Particles",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.22f),
                    materials.EnergyParticle,
                    new Color(0.1f, 0.85f, 1f, 0.9f),
                    24f,
                    0.18f,
                    0.12f,
                    1.4f,
                    ParticleSystemShapeType.Circle,
                    1.7f,
                    false);
                ParticleSystem energyMotes = CreateLoopParticleSystem(
                    "Portal Energy Motes",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.3f),
                    materials.EnergyParticle,
                    new Color(0.18f, 0.9f, 1f, 0.72f),
                    9f,
                    0.35f,
                    0.18f,
                    1.8f,
                    ParticleSystemShapeType.Circle,
                    1.35f,
                    false);
                ParticleSystem activationBurst = CreateBurstParticleSystem(
                    "Portal Activation Sparks",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.3f),
                    materials.EnergyParticle,
                    new Color(0.25f, 0.95f, 1f, 1f),
                    42,
                    2.4f,
                    0.08f,
                    0.75f,
                    ParticleSystemShapeType.Circle,
                    1.75f,
                    true);
                ParticleSystem activationFlash = CreateBurstParticleSystem(
                    "Portal Activation Flash",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.35f),
                    materials.EnergyParticle,
                    new Color(0.6f, 1f, 1f, 0.95f),
                    18,
                    0.7f,
                    0.42f,
                    0.48f,
                    ParticleSystemShapeType.Sphere,
                    0.65f,
                    false);

                CreateBox("State Inactive", root.transform, new Vector3(-0.55f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Dark);
                CreateBox("State Activating", root.transform, new Vector3(0f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Furnace);
                CreateBox("State Active", root.transform, new Vector3(0.55f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Active);

                FactoryPortalVisual visual = root.AddComponent<FactoryPortalVisual>();
                visual.Configure(
                    energyRoot.transform,
                    new[] { outerEnergyRing.transform, innerEnergyRing.transform },
                    new[] { energyParticles, energyMotes },
                    new[] { activationBurst, activationFlash });
                BoxCollider leftCollider = root.AddComponent<BoxCollider>();
                leftCollider.center = new Vector3(-2.35f, 3f, 0f);
                leftCollider.size = new Vector3(0.8f, 5.5f, 1.4f);
                BoxCollider rightCollider = root.AddComponent<BoxCollider>();
                rightCollider.center = new Vector3(2.35f, 3f, 0f);
                rightCollider.size = new Vector3(0.8f, 5.5f, 1.4f);
                SavePrefab(root, PrefabFolder + "/PF_Factory_Portal.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPortalCore(
            FactoryMaterials materials,
            Mesh crystalMesh,
            Mesh chamferedBlock,
            Mesh taperedHousing)
        {
            GameObject root = new GameObject("PF_Factory_PortalCore");
            try
            {
                CreateMeshPart(
                    "Graphite Containment Base",
                    root.transform,
                    chamferedBlock,
                    new Vector3(0f, -0.92f, 0f),
                    new Vector3(1.12f, 0.28f, 1.12f),
                    Quaternion.identity,
                    materials.Dark);
                CreateCylinder("Lower Bearing Collar", root.transform, new Vector3(0f, -0.67f, 0f), new Vector3(0.72f, 0.12f, 0.72f), Quaternion.identity, materials.Steel);
                CreateCylinder("Upper Bearing Collar", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.66f, 0.1f, 0.66f), Quaternion.identity, materials.Frame);

                for (int index = 0; index < 3; index++)
                {
                    float angle = index * 120f;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Sin(radians) * 0.72f, -0.02f, Mathf.Cos(radians) * 0.72f);
                    CreateMeshPart(
                        "Tapered Containment Vane " + (index + 1),
                        root.transform,
                        taperedHousing,
                        position,
                        new Vector3(0.34f, 1.2f, 0.26f),
                        Quaternion.Euler(0f, angle, 0f),
                        materials.Machine);
                    GameObject energySeam = CreateBox(
                        "Vane Energy Seam " + (index + 1),
                        root.transform,
                        position + Vector3.up * 0.08f,
                        new Vector3(0.07f, 0.72f, 0.06f),
                        materials.Energy);
                    energySeam.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                }

                GameObject core = CreateMeshObject("Faceted Energy Crystal", root.transform, crystalMesh, materials.Energy);
                core.transform.localScale = Vector3.one * 0.56f;

                GameObject orbitA = new GameObject("Rotating Components A");
                orbitA.transform.SetParent(root.transform, false);
                CreateOrbitRing(orbitA.transform, 0.88f, materials.Frame);

                GameObject orbitB = new GameObject("Rotating Components B");
                orbitB.transform.SetParent(root.transform, false);
                orbitB.transform.localRotation = Quaternion.Euler(90f, 0f, 24f);
                CreateOrbitRing(orbitB.transform, 1.02f, materials.Steel);

                ParticleSystem particles = CreateParticleSystem(
                    "Core Emissive Particles",
                    root.transform,
                    Vector3.zero,
                    materials.EnergyParticle,
                    new Color(0.12f, 0.9f, 1f, 1f),
                    12f,
                    0.22f,
                    0.08f,
                    1.1f,
                    ParticleSystemShapeType.Sphere);
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.radius = 0.65f;

                FactoryPortalCoreVisual visual = root.AddComponent<FactoryPortalCoreVisual>();
                visual.Configure(orbitA.transform, orbitB.transform, core.transform);
                SphereCollider collider = root.AddComponent<SphereCollider>();
                collider.radius = 1.2f;
                SavePrefab(root, PrefabFolder + "/PF_Factory_PortalCore.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildRouter(FactoryMaterials materials, Mesh arrowMesh, bool fourWay)
        {
            string suffix = fourWay ? "4Way" : "3Way";
            GameObject root = new GameObject("PF_Factory_ConveyorRouter_" + suffix);
            try
            {
                CreateBox("Junction Structural Deck", root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(3.2f, 0.36f, 3.2f), materials.Frame);
                CreateBox("Junction Belt Surface", root.transform, new Vector3(0f, 0.39f, 0f), new Vector3(2.65f, 0.08f, 2.65f), materials.Dark);
                CreateCylinder("Router Hub", root.transform, new Vector3(0f, 0.47f, 0f), new Vector3(1.05f, 0.08f, 1.05f), Quaternion.identity, materials.Steel);

                List<float> directions = new List<float> { 0f, 90f, 270f };
                if (fourWay)
                {
                    directions.Add(180f);
                }

                foreach (float direction in directions)
                {
                    float radians = direction * Mathf.Deg2Rad;
                    Vector3 outward = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                    Vector3 branchPosition = outward * 2.25f;
                    bool horizontal = Mathf.Abs(outward.x) > 0.5f;
                    Vector3 deckScale = horizontal
                        ? new Vector3(1.8f, 0.36f, 2.6f)
                        : new Vector3(2.6f, 0.36f, 1.8f);
                    Vector3 beltScale = horizontal
                        ? new Vector3(1.9f, 0.08f, 2.05f)
                        : new Vector3(2.05f, 0.08f, 1.9f);

                    CreateBox("Branch Deck " + direction, root.transform, new Vector3(branchPosition.x, 0.18f, branchPosition.z), deckScale, materials.Frame);
                    CreateBox("Branch Belt " + direction, root.transform, new Vector3(branchPosition.x, 0.39f, branchPosition.z), beltScale, materials.Dark);

                    GameObject arrow = CreateMeshObject("Direction Indicator " + direction, root.transform, arrowMesh, materials.Energy);
                    arrow.transform.localPosition = new Vector3(branchPosition.x, 0.48f, branchPosition.z);
                    arrow.transform.localRotation = Quaternion.Euler(0f, direction, 0f);
                    arrow.transform.localScale = Vector3.one * 0.72f;
                }

                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.18f, 0f);
                collider.size = fourWay ? new Vector3(6.6f, 0.36f, 6.6f) : new Vector3(6.6f, 0.36f, 5.1f);
                SavePrefab(root, PrefabFolder + "/PF_Factory_ConveyorRouter_" + suffix + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateTank(Transform parent, string name, Vector3 position, FactoryMaterials materials)
        {
            CreateCylinder(name + " Body", parent, position, new Vector3(0.82f, 1.25f, 0.82f), Quaternion.identity, materials.Machine);
            CreateCylinder(name + " Top Band", parent, position + Vector3.up * 1.03f, new Vector3(0.94f, 0.11f, 0.94f), Quaternion.identity, materials.Frame);
            CreateCylinder(name + " Bottom Band", parent, position + Vector3.down * 1.03f, new Vector3(0.94f, 0.11f, 0.94f), Quaternion.identity, materials.Frame);
            CreateCylinder(name + " Valve", parent, position + new Vector3(0f, 1.38f, 0f), new Vector3(0.22f, 0.22f, 0.22f), Quaternion.identity, materials.Dark);
        }

        private static void BuildProductionCargoPrefabs(FactoryMaterials materials)
        {
            BuildOreCargoPrefab(materials);
            BuildIngotCargoPrefab(materials);
            BuildPortalComponentCargoPrefab(materials);
        }

        private static void BuildOreCargoPrefab(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_OreCargo");
            try
            {
                CreatePrimitive(PrimitiveType.Sphere, "Ore Mass A", root.transform, new Vector3(-0.2f, 0f, 0f), Quaternion.Euler(12f, 25f, 8f), new Vector3(0.55f, 0.42f, 0.48f), materials.OreCargo);
                CreatePrimitive(PrimitiveType.Sphere, "Ore Mass B", root.transform, new Vector3(0.22f, 0.04f, 0.06f), Quaternion.Euler(-8f, 12f, 25f), new Vector3(0.46f, 0.34f, 0.4f), materials.OreCargo);
                CreateBox("Ore Vein", root.transform, new Vector3(0f, 0.08f, -0.24f), new Vector3(0.5f, 0.08f, 0.12f), materials.Energy);
                root.AddComponent<FactoryProductionCargo>();
                SavePrefab(root, OreCargoPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildIngotCargoPrefab(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_IngotCargo");
            try
            {
                CreateBox("Ingot Body", root.transform, Vector3.zero, new Vector3(0.72f, 0.24f, 0.42f), materials.Steel);
                CreateBox("Hot Ingot Core", root.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.5f, 0.04f, 0.24f), materials.Furnace);
                CreateBox("Ingot Band", root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.12f, 0.3f, 0.48f), materials.Frame);
                root.AddComponent<FactoryProductionCargo>();
                SavePrefab(root, IngotCargoPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildPortalComponentCargoPrefab(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_PortalComponentCargo");
            try
            {
                CreatePrimitive(PrimitiveType.Sphere, "Portal Component Core", root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.48f, materials.Energy);
                CreateCylinder("Portal Component Ring", root.transform, Vector3.zero, new Vector3(0.55f, 0.06f, 0.55f), Quaternion.Euler(90f, 0f, 0f), materials.Frame);
                CreateCylinder("Portal Component Collar", root.transform, Vector3.zero, new Vector3(0.34f, 0.08f, 0.34f), Quaternion.identity, materials.Steel);
                root.AddComponent<FactoryProductionCargo>();
                SavePrefab(root, PortalComponentCargoPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
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

            GameObject targetPointObject = new GameObject("Target Point");
            targetPointObject.transform.SetParent(root.transform, false);
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
            GameObject marker = new GameObject("Broken Machine Marker");
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;

            CreatePrimitive(
                PrimitiveType.Sphere,
                "Red Alert Beacon",
                marker.transform,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one * 0.62f,
                material);
            CreateBox(
                "Alert Stem",
                marker.transform,
                new Vector3(0f, -0.62f, 0f),
                new Vector3(0.16f, 0.58f, 0.16f),
                material);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Alert Point",
                marker.transform,
                new Vector3(0f, -1.02f, 0f),
                Quaternion.identity,
                Vector3.one * 0.22f,
                material);

            marker.SetActive(false);
            return marker;
        }

        private static GameObject CreateRobotArm(
            Transform parent,
            string name,
            float side,
            FactoryMaterials materials,
            Mesh chamferedBlock)
        {
            GameObject armRoot = new GameObject(name);
            armRoot.transform.SetParent(parent, false);
            Vector3 shoulder = new Vector3(side * 1.72f, 2.75f, -0.55f);
            Vector3 elbow = new Vector3(side * 1.48f, 1.88f, -1.15f);
            Vector3 wrist = new Vector3(side * 0.72f, 1.18f, -1.62f);
            armRoot.transform.localPosition = shoulder;
            elbow -= shoulder;
            wrist -= shoulder;
            shoulder = Vector3.zero;

            CreateCylinder("Shoulder Joint", armRoot.transform, shoulder, new Vector3(0.42f, 0.28f, 0.42f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            GameObject upperArm = CreateBarBetween("Upper Arm Underframe", armRoot.transform, shoulder, elbow, 0.31f, materials.Dark);
            CreateMeshPart("Upper Arm Shell", upperArm.transform, chamferedBlock, Vector3.zero, new Vector3(1.45f, 0.62f, 1.45f), Quaternion.identity, materials.Frame);
            CreateCylinder("Elbow Joint", armRoot.transform, elbow, new Vector3(0.34f, 0.3f, 0.34f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            GameObject forearm = CreateBarBetween("Forearm Underframe", armRoot.transform, elbow, wrist, 0.27f, materials.Dark);
            CreateMeshPart("Forearm Shell", forearm.transform, chamferedBlock, Vector3.zero, new Vector3(1.38f, 0.56f, 1.38f), Quaternion.identity, materials.Machine);
            CreateCylinder("Wrist Joint", armRoot.transform, wrist, new Vector3(0.26f, 0.22f, 0.26f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            CreateBox("Gripper Palm", armRoot.transform, wrist + new Vector3(0f, -0.2f, -0.12f), new Vector3(0.48f, 0.16f, 0.32f), materials.Frame);
            CreateBox("Gripper Left", armRoot.transform, wrist + new Vector3(-0.16f, -0.38f, -0.16f), new Vector3(0.1f, 0.36f, 0.12f), materials.Steel);
            CreateBox("Gripper Right", armRoot.transform, wrist + new Vector3(0.16f, -0.38f, -0.16f), new Vector3(0.1f, 0.36f, 0.12f), materials.Steel);
            return armRoot;
        }

        private static GameObject CreateDynamoRotor(
            Transform parent,
            string name,
            Vector3 position,
            FactoryMaterials materials,
            Mesh chamferedBlock)
        {
            GameObject rotor = new GameObject(name);
            rotor.transform.SetParent(parent, false);
            rotor.transform.localPosition = position;
            CreateCylinder("Dynamo Drum", rotor.transform, Vector3.zero, new Vector3(1f, 0.9f, 1f), Quaternion.Euler(0f, 0f, 90f), materials.Steel);
            for (int index = 0; index < 8; index++)
            {
                float angle = index * 45f * Mathf.Deg2Rad;
                CreateMeshPart(
                    "Dynamo Fin " + (index + 1),
                    rotor.transform,
                    chamferedBlock,
                    new Vector3(0f, Mathf.Cos(angle) * 0.82f, Mathf.Sin(angle) * 0.82f),
                    new Vector3(1.85f, 0.14f, 0.3f),
                    Quaternion.Euler(index * 45f, 0f, 0f),
                    materials.Frame);
            }

            CreateCylinder("Outer Bearing", rotor.transform, new Vector3(-0.95f, 0f, 0f), new Vector3(0.42f, 0.16f, 0.42f), Quaternion.Euler(0f, 0f, 90f), materials.Dark);
            return rotor;
        }

        private static GameObject CreateMeshPart(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            GameObject part = CreateMeshObject(name, parent, mesh, material);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            return part;
        }

        private static void CreatePortalRing(Transform parent, float radius, int segmentCount, Material material)
        {
            for (int index = 0; index < segmentCount; index++)
            {
                float angle = index * 360f / segmentCount;
                float radians = angle * Mathf.Deg2Rad;
                GameObject segment = CreateBox(
                    "Energy Ring Segment " + (index + 1),
                    parent,
                    new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, -0.16f),
                    new Vector3(0.42f, 0.12f, 0.12f),
                    material);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private static void CreateOrbitRing(Transform parent, float radius, Material material)
        {
            const int segmentCount = 12;
            for (int index = 0; index < segmentCount; index++)
            {
                float angle = index * 360f / segmentCount;
                float radians = angle * Mathf.Deg2Rad;
                GameObject segment = CreateBox(
                    "Orbit Segment " + (index + 1),
                    parent,
                    new Vector3(Mathf.Sin(radians) * radius, 0f, Mathf.Cos(radians) * radius),
                    new Vector3(0.12f, 0.1f, 0.55f),
                    material);
                segment.transform.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);
            }
        }

        private static void CreatePipeRun(Transform parent, Vector3[] points, float diameter, Material material, string prefix)
        {
            for (int index = 0; index < points.Length - 1; index++)
            {
                CreateCylinderBetween(prefix + " " + (index + 1), parent, points[index], points[index + 1], diameter, material);
                if (index < points.Length - 2)
                {
                    CreatePrimitive(
                        PrimitiveType.Sphere,
                        prefix + " Elbow " + (index + 1),
                        parent,
                        points[index + 1],
                        Quaternion.identity,
                        Vector3.one * diameter * 1.18f,
                        material);
                }
            }
        }

        private static GameObject CreateCylinderBetween(string name, Transform parent, Vector3 start, Vector3 end, float diameter, Material material)
        {
            Vector3 direction = end - start;
            return CreateCylinder(
                name,
                parent,
                (start + end) * 0.5f,
                new Vector3(diameter, direction.magnitude * 0.5f, diameter),
                Quaternion.FromToRotation(Vector3.up, direction.normalized),
                material);
        }

        private static GameObject CreateBarBetween(string name, Transform parent, Vector3 start, Vector3 end, float thickness, Material material)
        {
            Vector3 direction = end - start;
            GameObject bar = CreateBox(name, parent, (start + end) * 0.5f, new Vector3(thickness, direction.magnitude, thickness), material);
            bar.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return bar;
        }

        private static BoxCollider AddBoxCollider(GameObject target, Vector3 center, Vector3 size)
        {
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            return collider;
        }

        private static CapsuleCollider AddCapsuleCollider(
            GameObject target,
            Vector3 center,
            float radius,
            float height,
            int direction)
        {
            CapsuleCollider collider = target.AddComponent<CapsuleCollider>();
            collider.center = center;
            collider.radius = radius;
            collider.height = height;
            collider.direction = direction;
            return collider;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, position, Quaternion.identity, scale, material);
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, rotation, scale, material);
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = rotation;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return primitive;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            target.AddComponent<MeshRenderer>().sharedMaterial = material;
            return target;
        }

        private static ParticleSystem CreateParticleSystem(
            string name,
            Transform parent,
            Vector3 position,
            Material material,
            Color color,
            float rate,
            float speed,
            float size,
            float lifetime,
            ParticleSystemShapeType shapeType)
        {
            ParticleSystem particleSystem = CreateLoopParticleSystem(
                name,
                parent,
                position,
                material,
                color,
                rate,
                speed,
                size,
                lifetime,
                shapeType,
                shapeType == ParticleSystemShapeType.Circle ? 1.7f : 0.28f,
                false);
            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = true;
            return particleSystem;
        }

        private static ParticleSystem CreateLoopParticleSystem(
            string name,
            Transform parent,
            Vector3 position,
            Material material,
            Color color,
            float rate,
            float speed,
            float size,
            float lifetime,
            ParticleSystemShapeType shapeType,
            float shapeRadius,
            bool stretched)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = position;
            ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 96;
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = shapeType;
            shape.radius = shapeRadius;
            if (shapeType == ParticleSystemShapeType.Box)
            {
                shape.scale = Vector3.one * shapeRadius;
            }
            if (shapeType == ParticleSystemShapeType.Cone)
            {
                shape.angle = 9f;
            }

            ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = stretched
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            if (stretched)
            {
                renderer.velocityScale = 0.18f;
                renderer.lengthScale = 1.4f;
            }

            ConfigureParticleFade(particleSystem, 0.1f, 0.72f);
            return particleSystem;
        }

        private static ParticleSystem CreateBurstParticleSystem(
            string name,
            Transform parent,
            Vector3 position,
            Material material,
            Color color,
            short count,
            float speed,
            float size,
            float lifetime,
            ParticleSystemShapeType shapeType,
            float shapeRadius,
            bool stretched)
        {
            ParticleSystem particleSystem = CreateLoopParticleSystem(
                name,
                parent,
                position,
                material,
                color,
                0f,
                speed,
                size,
                lifetime,
                shapeType,
                shapeRadius,
                stretched);
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.maxParticles = Mathf.Min(128, Mathf.Max(count, 1));
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            return particleSystem;
        }

        private static void ConfigureParticleFade(
            ParticleSystem particleSystem,
            float fadeInFraction,
            float holdFraction)
        {
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, fadeInFraction),
                    new GradientAlphaKey(0.78f, holdFraction),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.25f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(1f, 0.12f)));
        }

        private static Material CreateOrUpdateLitMaterial(string path, Color baseColor, float metallic, float smoothness, Color emission)
        {
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

        private static Material CreateOrUpdateParticleMaterial(string path, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
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
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateMesh(string path, Mesh generated)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            existing.name = generated.name;
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generated);
            return existing;
        }

        private static Mesh BuildChamferedBlockMesh()
        {
            Vector2[] outline =
            {
                new Vector2(-0.36f, -0.5f), new Vector2(0.36f, -0.5f),
                new Vector2(0.5f, -0.36f), new Vector2(0.5f, 0.36f),
                new Vector2(0.36f, 0.5f), new Vector2(-0.36f, 0.5f),
                new Vector2(-0.5f, 0.36f), new Vector2(-0.5f, -0.36f)
            };
            return BuildExtrudedOutlineMesh("SM_Factory_ChamferedBlock", outline, outline);
        }

        private static Mesh BuildTaperedHousingMesh()
        {
            Vector2[] bottom =
            {
                new Vector2(-0.43f, -0.5f), new Vector2(0.43f, -0.5f),
                new Vector2(0.5f, -0.43f), new Vector2(0.5f, 0.43f),
                new Vector2(0.43f, 0.5f), new Vector2(-0.43f, 0.5f),
                new Vector2(-0.5f, 0.43f), new Vector2(-0.5f, -0.43f)
            };
            Vector2[] top =
            {
                new Vector2(-0.31f, -0.42f), new Vector2(0.31f, -0.42f),
                new Vector2(0.42f, -0.31f), new Vector2(0.42f, 0.31f),
                new Vector2(0.31f, 0.42f), new Vector2(-0.31f, 0.42f),
                new Vector2(-0.42f, 0.31f), new Vector2(-0.42f, -0.31f)
            };
            return BuildExtrudedOutlineMesh("SM_Factory_TaperedHousing", bottom, top);
        }

        private static Mesh BuildExtrudedOutlineMesh(string name, Vector2[] bottom, Vector2[] top)
        {
            List<Vector3> vertices = new List<Vector3>(bottom.Length * 10);
            List<int> triangles = new List<int>(bottom.Length * 12);
            for (int index = 0; index < bottom.Length; index++)
            {
                int next = (index + 1) % bottom.Length;
                Vector3 bottomCurrent = new Vector3(bottom[index].x, -0.5f, bottom[index].y);
                Vector3 bottomNext = new Vector3(bottom[next].x, -0.5f, bottom[next].y);
                Vector3 topCurrent = new Vector3(top[index].x, 0.5f, top[index].y);
                Vector3 topNext = new Vector3(top[next].x, 0.5f, top[next].y);
                AddMeshQuad(vertices, triangles, bottomCurrent, topCurrent, topNext, bottomNext);
                AddMeshTriangle(vertices, triangles, Vector3.up * 0.5f, topNext, topCurrent);
                AddMeshTriangle(vertices, triangles, Vector3.down * 0.5f, bottomCurrent, bottomNext);
            }

            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddMeshQuad(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddMeshTriangle(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static Mesh BuildPortalFrameSegmentMesh()
        {
            const float innerRadius = 1.85f;
            const float outerRadius = 2.5f;
            const float halfAngle = 22.5f;
            const float halfDepth = 0.34f;
            Vector2 innerA = Polar(innerRadius, -halfAngle);
            Vector2 innerB = Polar(innerRadius, halfAngle);
            Vector2 outerA = Polar(outerRadius, -halfAngle);
            Vector2 outerB = Polar(outerRadius, halfAngle);
            Vector3[] vertices =
            {
                new Vector3(innerA.x, innerA.y, -halfDepth),
                new Vector3(innerB.x, innerB.y, -halfDepth),
                new Vector3(outerB.x, outerB.y, -halfDepth),
                new Vector3(outerA.x, outerA.y, -halfDepth),
                new Vector3(innerA.x, innerA.y, halfDepth),
                new Vector3(innerB.x, innerB.y, halfDepth),
                new Vector3(outerB.x, outerB.y, halfDepth),
                new Vector3(outerA.x, outerA.y, halfDepth)
            };
            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };
            return CreateMesh("SM_Factory_PortalFrameSegment", vertices, triangles);
        }

        private static Mesh BuildPortalCoreCrystalMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(new Vector3(0f, 1.25f, 0f));
            for (int index = 0; index < 6; index++)
            {
                float angle = index * 60f * Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.62f, 0.32f, Mathf.Sin(angle) * 0.62f));
            }

            for (int index = 0; index < 6; index++)
            {
                float angle = (index * 60f + 30f) * Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, -0.32f, Mathf.Sin(angle) * 0.5f));
            }

            vertices.Add(new Vector3(0f, -1.25f, 0f));
            List<int> triangles = new List<int>();
            for (int index = 0; index < 6; index++)
            {
                int next = (index + 1) % 6;
                triangles.Add(0);
                triangles.Add(1 + index);
                triangles.Add(1 + next);

                int upperA = 1 + index;
                int upperB = 1 + next;
                int lowerA = 7 + index;
                int lowerB = 7 + next;
                triangles.Add(upperA);
                triangles.Add(lowerA);
                triangles.Add(upperB);
                triangles.Add(upperB);
                triangles.Add(lowerA);
                triangles.Add(lowerB);

                triangles.Add(13);
                triangles.Add(lowerB);
                triangles.Add(lowerA);
            }

            return CreateMesh("SM_Factory_PortalCoreCrystal", vertices.ToArray(), triangles.ToArray());
        }

        private static Mesh BuildDirectionArrowMesh()
        {
            Vector2[] outline =
            {
                new Vector2(-0.26f, -0.62f),
                new Vector2(0.26f, -0.62f),
                new Vector2(0.26f, 0.05f),
                new Vector2(0.56f, 0.05f),
                new Vector2(0f, 0.68f),
                new Vector2(-0.56f, 0.05f),
                new Vector2(-0.26f, 0.05f)
            };
            List<Vector3> vertices = new List<Vector3>();
            for (int index = 0; index < outline.Length; index++)
            {
                vertices.Add(new Vector3(outline[index].x, 0.045f, outline[index].y));
            }

            for (int index = 0; index < outline.Length; index++)
            {
                vertices.Add(new Vector3(outline[index].x, -0.045f, outline[index].y));
            }

            vertices.Add(new Vector3(0f, 0.045f, 0f));
            vertices.Add(new Vector3(0f, -0.045f, 0f));
            int topCenter = outline.Length * 2;
            int bottomCenter = topCenter + 1;
            List<int> triangles = new List<int>();
            for (int index = 0; index < outline.Length; index++)
            {
                int next = (index + 1) % outline.Length;
                triangles.Add(topCenter);
                triangles.Add(index);
                triangles.Add(next);
                triangles.Add(bottomCenter);
                triangles.Add(outline.Length + next);
                triangles.Add(outline.Length + index);
                triangles.Add(index);
                triangles.Add(outline.Length + index);
                triangles.Add(next);
                triangles.Add(next);
                triangles.Add(outline.Length + index);
                triangles.Add(outline.Length + next);
            }

            return CreateMesh("SM_Factory_DirectionArrow", vertices.ToArray(), triangles.ToArray());
        }

        private static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2 Polar(float radius, float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
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

        private sealed class FactoryMaterials
        {
            public Material Frame;
            public Material Dark;
            public Material Steel;
            public Material Machine;
            public Material Furnace;
            public Material Energy;
            public Material Active;
            public Material Broken;
            public Material OreCargo;
            public Material Smoke;
            public Material EnergyParticle;
            public Material Dust;
            public Material Sparks;
            public Material Embers;
        }
    }
}
