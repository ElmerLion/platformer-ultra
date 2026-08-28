using System.Collections.Generic;
using PlatformerUltra.Audio;
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

            BuildSmelter(materials);
            BuildAssembler(materials);
            BuildCrusher(materials);
            BuildPortal(materials, portalSegment);
            BuildPortalCore(materials, portalCrystal);
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
                    new Color(0.08f, 0.78f, 1f, 0.78f))
            };
        }

        private static void BuildSmelter(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_Smelter");
            try
            {
                CreateBox("Structural Base", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(5.4f, 0.5f, 4.2f), materials.Frame);
                CreateBox("Furnace Housing", root.transform, new Vector3(0f, 1.8f, 0.25f), new Vector3(2.6f, 3.1f, 2.5f), materials.Machine);
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
                ParticleSystem smokePlume = CreateParticleSystem(
                    "Smoke Plume",
                    root.transform,
                    new Vector3(0f, 6.35f, 0.65f),
                    materials.Smoke,
                    new Color(0.2f, 0.23f, 0.25f, 0.7f),
                    8f,
                    0.72f,
                    0.48f,
                    2.6f,
                    ParticleSystemShapeType.Cone);
                MachineLoopAudio smelterAudio = smokePlume.gameObject.AddComponent<MachineLoopAudio>();
                smelterAudio.Configure(
                    AssetDatabase.LoadAssetAtPath<AudioClip>(SmelterAudioPath),
                    0.08f,
                    3f,
                    16f);

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
                AddMachineCombat(
                    root,
                    "Smelter",
                    140,
                    5f,
                    new Vector3(0f, 2.2f, -2.25f),
                    new[] { furnaceChamber.GetComponent<Renderer>() },
                    brokenMarker);
                SavePrefab(root, PrefabFolder + "/PF_Factory_Smelter.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildAssembler(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_Assembler");
            try
            {
                List<Renderer> statusRenderers = new List<Renderer>();
                CreateBox("Structural Base", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(5.5f, 0.5f, 4.3f), materials.Frame);
                CreateBox("Machine Cabinet", root.transform, new Vector3(0f, 1.8f, 0.7f), new Vector3(4.6f, 3.1f, 1.8f), materials.Machine);
                for (int index = -1; index <= 1; index++)
                {
                    float x = index * 1.35f;
                    CreateBox("Modular Panel " + (index + 2), root.transform, new Vector3(x, 1.95f, -0.23f), new Vector3(1.12f, 1.55f, 0.12f), materials.Steel);
                    CreateBox("Panel Header " + (index + 2), root.transform, new Vector3(x, 2.64f, -0.34f), new Vector3(1.2f, 0.16f, 0.16f), materials.Frame);
                    GameObject statusLight = CreateBox("Status Light " + (index + 2), root.transform, new Vector3(x, 1.5f, -0.36f), new Vector3(0.55f, 0.12f, 0.12f), index == 0 ? materials.Energy : materials.Active);
                    statusRenderers.Add(statusLight.GetComponent<Renderer>());
                }

                CreateRobotArm(root.transform, "Left Robotic Arm", -1f, materials);
                CreateRobotArm(root.transform, "Right Robotic Arm", 1f, materials);

                CreateBox("Output Cradle Deck", root.transform, new Vector3(0f, 0.72f, -1.55f), new Vector3(2.7f, 0.18f, 1.35f), materials.Dark);
                CreateBox("Output Cradle Left Rail", root.transform, new Vector3(-1.35f, 0.92f, -1.55f), new Vector3(0.16f, 0.42f, 1.45f), materials.Frame);
                CreateBox("Output Cradle Right Rail", root.transform, new Vector3(1.35f, 0.92f, -1.55f), new Vector3(0.16f, 0.42f, 1.45f), materials.Frame);
                for (int index = 0; index < 5; index++)
                {
                    CreateCylinder(
                        "Output Roller " + (index + 1),
                        root.transform,
                        new Vector3(0f, 0.84f, -2.02f + index * 0.24f),
                        new Vector3(0.12f, 1.24f, 0.12f),
                        Quaternion.Euler(0f, 0f, 90f),
                        materials.Steel);
                }

                AddBoxCollider(root, new Vector3(0f, 0.25f, 0f), new Vector3(5.5f, 0.5f, 4.3f));
                AddBoxCollider(root, new Vector3(0f, 1.8f, 0.7f), new Vector3(4.6f, 3.1f, 1.8f));
                AddBoxCollider(root, new Vector3(0f, 0.72f, -1.55f), new Vector3(2.7f, 0.18f, 1.35f));
                GameObject brokenMarker = CreateBrokenMarker(
                    root.transform,
                    new Vector3(0f, 4.75f, 0f),
                    materials.Broken);
                AddMachineCombat(
                    root,
                    "Assembler",
                    160,
                    5f,
                    new Vector3(0f, 1.8f, -2.25f),
                    statusRenderers.ToArray(),
                    brokenMarker);
                SavePrefab(root, PrefabFolder + "/PF_Factory_Assembler.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildCrusher(FactoryMaterials materials)
        {
            GameObject root = new GameObject("PF_Factory_Crusher");
            try
            {
                CreateBox("Anvil Base", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(4.5f, 0.6f, 3.7f), materials.Machine);
                CreateBox("Anvil Surface", root.transform, new Vector3(0f, 0.68f, 0f), new Vector3(3.45f, 0.18f, 2.65f), materials.Steel);
                float[] xs = { -1.85f, 1.85f };
                float[] zs = { -1.45f, 1.45f };
                foreach (float x in xs)
                {
                    foreach (float z in zs)
                    {
                        CreateBox("Frame Post", root.transform, new Vector3(x, 2.35f, z), new Vector3(0.32f, 3.9f, 0.32f), materials.Frame);
                    }
                }

                CreateBox("Top Beam", root.transform, new Vector3(0f, 4.3f, 0f), new Vector3(4.4f, 0.48f, 3.55f), materials.Frame);
                CreateCylinder("Hydraulic Ram", root.transform, new Vector3(0f, 3.65f, 0f), new Vector3(0.55f, 0.75f, 0.55f), Quaternion.identity, materials.Steel);
                GameObject plate = CreateBox("Crushing Plate", root.transform, new Vector3(0f, 3.05f, 0f), new Vector3(3.35f, 0.42f, 2.55f), materials.Machine);
                plate.AddComponent<BoxCollider>();
                CreateBox("Crushing Plate Hazard Face", plate.transform, new Vector3(0f, -0.55f, 0f), new Vector3(0.92f, 0.16f, 0.92f), materials.Furnace);
                for (int index = -2; index <= 2; index++)
                {
                    CreateBox("Crusher Tooth " + (index + 3), plate.transform, new Vector3(index * 0.22f, -0.72f, 0f), new Vector3(0.12f, 0.32f, 0.7f), materials.Steel);
                }

                FactoryCrusherVisual visual = root.AddComponent<FactoryCrusherVisual>();
                visual.Configure(plate.transform, 3.05f, 1.05f, 1.5f);
                MachineLoopAudio crusherAudio = root.AddComponent<MachineLoopAudio>();
                crusherAudio.Configure(
                    AssetDatabase.LoadAssetAtPath<AudioClip>(CrusherAudioPath),
                    0.11f,
                    2.5f,
                    13f,
                    0.2f);
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

        private static void BuildPortal(FactoryMaterials materials, Mesh segmentMesh)
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
                }

                CreateBox("Left Frame Foot", root.transform, new Vector3(-2.35f, 1.05f, 0f), new Vector3(0.75f, 1.1f, 1.45f), materials.Frame);
                CreateBox("Right Frame Foot", root.transform, new Vector3(2.35f, 1.05f, 0f), new Vector3(0.75f, 1.1f, 1.45f), materials.Frame);

                GameObject energyRoot = new GameObject("Portal Energy - Activation State Visual");
                energyRoot.transform.SetParent(root.transform, false);
                energyRoot.transform.localPosition = new Vector3(0f, 3.15f, 0f);
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

                ParticleSystem energyParticles = CreateParticleSystem(
                    "Portal Energy Particles",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.22f),
                    materials.EnergyParticle,
                    new Color(0.1f, 0.85f, 1f, 0.9f),
                    24f,
                    0.18f,
                    0.12f,
                    1.4f,
                    ParticleSystemShapeType.Circle);
                ParticleSystem activationBurst = CreateParticleSystem(
                    "Portal Activation Sparks",
                    energyRoot.transform,
                    new Vector3(0f, 0f, -0.3f),
                    materials.EnergyParticle,
                    new Color(0.25f, 0.95f, 1f, 1f),
                    10f,
                    0.65f,
                    0.08f,
                    0.75f,
                    ParticleSystemShapeType.Sphere);

                CreateBox("State Inactive", root.transform, new Vector3(-0.55f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Dark);
                CreateBox("State Activating", root.transform, new Vector3(0f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Furnace);
                CreateBox("State Active", root.transform, new Vector3(0.55f, 0.85f, -1.02f), new Vector3(0.28f, 0.14f, 0.12f), materials.Active);

                FactoryPortalVisual visual = root.AddComponent<FactoryPortalVisual>();
                visual.Configure(energyRoot.transform, new[] { energyParticles, activationBurst });
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

        private static void BuildPortalCore(FactoryMaterials materials, Mesh crystalMesh)
        {
            GameObject root = new GameObject("PF_Factory_PortalCore");
            try
            {
                GameObject core = CreateMeshObject("Custom Low-Poly Core", root.transform, crystalMesh, materials.Energy);
                core.transform.localScale = Vector3.one * 0.75f;

                GameObject orbitA = new GameObject("Rotating Components A");
                orbitA.transform.SetParent(root.transform, false);
                CreateOrbitRing(orbitA.transform, 1.05f, materials.Frame);

                GameObject orbitB = new GameObject("Rotating Components B");
                orbitB.transform.SetParent(root.transform, false);
                orbitB.transform.localRotation = Quaternion.Euler(90f, 0f, 24f);
                CreateOrbitRing(orbitB.transform, 1.25f, materials.Steel);

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
                shape.radius = 0.85f;

                FactoryPortalCoreVisual visual = root.AddComponent<FactoryPortalCoreVisual>();
                visual.Configure(orbitA.transform, orbitB.transform, core.transform);
                SphereCollider collider = root.AddComponent<SphereCollider>();
                collider.radius = 1.4f;
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

        private static void CreateRobotArm(Transform parent, string name, float side, FactoryMaterials materials)
        {
            GameObject armRoot = new GameObject(name);
            armRoot.transform.SetParent(parent, false);
            Vector3 shoulder = new Vector3(side * 1.72f, 2.75f, -0.55f);
            Vector3 elbow = new Vector3(side * 1.48f, 1.88f, -1.15f);
            Vector3 wrist = new Vector3(side * 0.72f, 1.18f, -1.62f);

            CreateCylinder("Shoulder Joint", armRoot.transform, shoulder, new Vector3(0.42f, 0.28f, 0.42f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            CreateBarBetween("Upper Arm", armRoot.transform, shoulder, elbow, 0.31f, materials.Frame);
            CreateCylinder("Elbow Joint", armRoot.transform, elbow, new Vector3(0.34f, 0.3f, 0.34f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            CreateBarBetween("Forearm", armRoot.transform, elbow, wrist, 0.27f, materials.Dark);
            CreateCylinder("Wrist Joint", armRoot.transform, wrist, new Vector3(0.26f, 0.22f, 0.26f), Quaternion.Euler(90f, 0f, 0f), materials.Steel);
            CreateBox("Gripper Palm", armRoot.transform, wrist + new Vector3(0f, -0.2f, -0.12f), new Vector3(0.48f, 0.16f, 0.32f), materials.Frame);
            CreateBox("Gripper Left", armRoot.transform, wrist + new Vector3(-0.16f, -0.38f, -0.16f), new Vector3(0.1f, 0.36f, 0.12f), materials.Steel);
            CreateBox("Gripper Right", armRoot.transform, wrist + new Vector3(0.16f, -0.38f, -0.16f), new Vector3(0.1f, 0.36f, 0.12f), materials.Steel);
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
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = position;
            ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 96;
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = shapeType;
            shape.radius = shapeType == ParticleSystemShapeType.Circle ? 1.7f : 0.28f;
            if (shapeType == ParticleSystemShapeType.Cone)
            {
                shape.angle = 9f;
            }

            ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return particleSystem;
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
        }
    }
}
