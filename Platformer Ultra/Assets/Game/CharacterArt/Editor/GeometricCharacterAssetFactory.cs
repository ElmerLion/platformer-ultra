using System;
using System.Collections.Generic;
using PlatformerUltra.Enemies;
using PlatformerUltra.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlatformerUltra.CharacterArt.Editor
{
    public static class GeometricCharacterAssetFactory
    {
        public const string PlayerVisualPrefabPath =
            "Assets/Game/CharacterArt/Prefabs/PF_Player_MaintenanceUnit_Visual.prefab";
        public const string SaboteurVisualPrefabPath =
            "Assets/Game/CharacterArt/Prefabs/PF_Enemy_Saboteur_Cutter_Visual.prefab";
        public const string ArmoredVisualPrefabPath =
            "Assets/Game/CharacterArt/Prefabs/PF_Enemy_Armored_FoundryBrute_Visual.prefab";

        public const string LegacyPlayerPrefabPath =
            "Assets/Game/CharacterArt/Old/Player/PF_Player_Prototype_Old.prefab";
        public const string LegacySaboteurPrefabPath =
            "Assets/Game/CharacterArt/Old/Enemies/PF_Enemy_Saboteur_Old.prefab";
        public const string LegacyArmoredPrefabPath =
            "Assets/Game/CharacterArt/Old/Enemies/PF_Enemy_Armored_Old.prefab";

        private const string RootFolder = "Assets/Game/CharacterArt";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string MeshFolder = RootFolder + "/Meshes";
        private const string OldFolder = RootFolder + "/Old";
        private const string OldPlayerFolder = OldFolder + "/Player";
        private const string OldEnemyFolder = OldFolder + "/Enemies";

        private const string CurrentPlayerPrefabPath = "Assets/Game/Gameplay/Prefabs/PF_Player_Prototype.prefab";
        private const string CurrentPlayerControllerPath = "Assets/Game/Gameplay/Animations/AC_Player_Prototype.controller";
        private const string CurrentSaboteurPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Saboteur.prefab";
        private const string CurrentArmoredPrefabPath = "Assets/Game/Enemies/Prefabs/PF_Enemy_Armored.prefab";
        private const string CurrentSaboteurDefinitionPath = "Assets/Game/Enemies/Data/DA_Enemy_Saboteur.asset";
        private const string CurrentArmoredDefinitionPath = "Assets/Game/Enemies/Data/DA_Enemy_Armored.asset";
        private const string CurrentSaboteurControllerPath = "Assets/Game/Enemies/Animations/AC_Enemy_Saboteur.controller";
        private const string CurrentArmoredControllerPath = "Assets/Game/Enemies/Animations/AC_Enemy_Armored.controller";

        private const string ChamferedBlockMeshPath = MeshFolder + "/SM_Character_ChamferedBlock.asset";
        private const string TaperedBlockMeshPath = MeshFolder + "/SM_Character_TaperedBlock.asset";
        private const string BladeMeshPath = MeshFolder + "/SM_Character_IndustrialBlade.asset";

        [MenuItem("Tools/Platformer Ultra/Character Art/Build Geometric Character Visuals")]
        public static void BuildAssets()
        {
            EnsureFolders();
            BackupLegacyAssets();

            CharacterMeshes meshes = BuildMeshes();
            CharacterMaterials materials = BuildMaterials();
            BuildPlayerVisual(meshes, materials);
            BuildSaboteurVisual(meshes, materials);
            BuildArmoredVisual(meshes, materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built the maintenance unit, saboteur cutter, and foundry brute geometric visual rigs.");
        }

        public static void BackupLegacyAssets()
        {
            EnsureFolders();
            CopyAssetOnce(CurrentPlayerPrefabPath, LegacyPlayerPrefabPath);
            CopyAssetOnce(CurrentPlayerControllerPath, OldPlayerFolder + "/AC_Player_Prototype_Old.controller");
            CopyAssetOnce(CurrentSaboteurPrefabPath, LegacySaboteurPrefabPath);
            CopyAssetOnce(CurrentSaboteurDefinitionPath, OldEnemyFolder + "/DA_Enemy_Saboteur_Old.asset");
            CopyAssetOnce(CurrentSaboteurControllerPath, OldEnemyFolder + "/AC_Enemy_Saboteur_Old.controller");
            CopyAssetOnce(CurrentArmoredPrefabPath, LegacyArmoredPrefabPath);
            CopyAssetOnce(CurrentArmoredDefinitionPath, OldEnemyFolder + "/DA_Enemy_Armored_Old.asset");
            CopyAssetOnce(CurrentArmoredControllerPath, OldEnemyFolder + "/AC_Enemy_Armored_Old.controller");
            RewireLegacyBackups();
        }

        private static void RewireLegacyBackups()
        {
            RuntimeAnimatorController playerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                OldPlayerFolder + "/AC_Player_Prototype_Old.controller");
            RewireLegacyPlayerPrefab(playerController);

            RewireLegacyEnemySet(
                CurrentSaboteurDefinitionPath,
                OldEnemyFolder + "/DA_Enemy_Saboteur_Old.asset",
                OldEnemyFolder + "/AC_Enemy_Saboteur_Old.controller",
                LegacySaboteurPrefabPath);
            RewireLegacyEnemySet(
                CurrentArmoredDefinitionPath,
                OldEnemyFolder + "/DA_Enemy_Armored_Old.asset",
                OldEnemyFolder + "/AC_Enemy_Armored_Old.controller",
                LegacyArmoredPrefabPath);
        }

        private static void RewireLegacyPlayerPrefab(RuntimeAnimatorController legacyController)
        {
            if (legacyController == null || AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPlayerPrefabPath) == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(LegacyPlayerPrefabPath);
            try
            {
                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                for (int index = 0; index < animators.Length; index++)
                {
                    animators[index].runtimeAnimatorController = legacyController;
                }

                PrefabUtility.SaveAsPrefabAsset(root, LegacyPlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RewireLegacyEnemySet(
            string currentDefinitionPath,
            string legacyDefinitionPath,
            string legacyControllerPath,
            string legacyPrefabPath)
        {
            EnemyDefinition currentDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(currentDefinitionPath);
            EnemyDefinition legacyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(legacyDefinitionPath);
            RuntimeAnimatorController legacyController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                legacyControllerPath);
            GameObject legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(legacyPrefabPath);
            if (currentDefinition == null || legacyDefinition == null || legacyController == null || legacyPrefab == null)
            {
                return;
            }

            legacyDefinition.ConfigureIdentity(
                legacyDefinition.Archetype,
                legacyDefinition.VisualPrefab,
                legacyController,
                legacyPrefab,
                legacyDefinition.ProjectilePrefab,
                legacyDefinition.SpawnWeight);
            legacyDefinition.SetSpawnPrefab(legacyPrefab);
            EditorUtility.SetDirty(legacyDefinition);

            GameObject root = PrefabUtility.LoadPrefabContents(legacyPrefabPath);
            try
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    MonoBehaviour behaviour = behaviours[behaviourIndex];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    SerializedObject serialized = new SerializedObject(behaviour);
                    SerializedProperty property = serialized.GetIterator();
                    bool enterChildren = true;
                    bool changed = false;
                    while (property.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue == currentDefinition)
                        {
                            property.objectReferenceValue = legacyDefinition;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                for (int index = 0; index < animators.Length; index++)
                {
                    animators[index].runtimeAnimatorController = legacyController;
                }

                PrefabUtility.SaveAsPrefabAsset(root, legacyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildPlayerVisual(CharacterMeshes meshes, CharacterMaterials materials)
        {
            GameObject root = new GameObject("PF_Player_MaintenanceUnit_Visual");
            try
            {
                Transform rig = CreatePivot("Procedural Rig", root.transform, Vector3.zero);
                Transform pelvis = CreatePivot("Pelvis", rig, new Vector3(0f, 0.91f, 0f));
                CreatePart("Pelvis Underframe", pelvis, meshes.ChamferedBlock, materials.Graphite,
                    Vector3.zero, new Vector3(0.43f, 0.22f, 0.30f));
                CreatePart("Pelvis Safety Band", pelvis, meshes.ChamferedBlock, materials.SafetyOrange,
                    new Vector3(0f, 0.105f, 0.012f), new Vector3(0.51f, 0.105f, 0.31f));
                CreatePart("Pelvis Front Plate", pelvis, meshes.ChamferedBlock, materials.Ceramic,
                    new Vector3(0f, 0.015f, 0.17f), new Vector3(0.24f, 0.12f, 0.035f));
                CreatePart("Left Hip Bearing", pelvis, meshes.Sphere, materials.Steel,
                    new Vector3(-0.25f, -0.03f, 0f), Vector3.one * 0.145f);
                CreatePart("Right Hip Bearing", pelvis, meshes.Sphere, materials.Steel,
                    new Vector3(0.25f, -0.03f, 0f), Vector3.one * 0.145f);

                Transform chest = CreatePivot("Chest", rig, new Vector3(0f, 1.13f, 0f));
                CreatePart("Torso Underframe", chest, meshes.TaperedBlock, materials.Graphite,
                    new Vector3(0f, 0.16f, 0f), new Vector3(0.61f, 0.53f, 0.36f));
                CreatePart("Left Torso Shell", chest, meshes.ChamferedBlock, materials.SafetyOrange,
                    new Vector3(-0.19f, 0.18f, 0.20f), new Vector3(0.26f, 0.34f, 0.065f),
                    Quaternion.Euler(0f, -7f, -3f));
                CreatePart("Right Torso Shell", chest, meshes.ChamferedBlock, materials.SafetyOrange,
                    new Vector3(0.19f, 0.18f, 0.20f), new Vector3(0.26f, 0.34f, 0.065f),
                    Quaternion.Euler(0f, 7f, 3f));
                CreatePart("Chest Center Plate", chest, meshes.TaperedBlock, materials.Ceramic,
                    new Vector3(0f, 0.21f, 0.224f), new Vector3(0.22f, 0.25f, 0.045f));
                Transform core = CreatePart("Cyan Service Core", chest, meshes.Cylinder, materials.EnergyCyan,
                    new Vector3(0f, 0.20f, 0.272f), new Vector3(0.105f, 0.035f, 0.105f),
                    Quaternion.Euler(90f, 0f, 0f));
                CreatePart("Core Guard Left", chest, meshes.ChamferedBlock, materials.Steel,
                    new Vector3(-0.14f, 0.20f, 0.258f), new Vector3(0.04f, 0.24f, 0.035f));
                CreatePart("Core Guard Right", chest, meshes.ChamferedBlock, materials.Steel,
                    new Vector3(0.14f, 0.20f, 0.258f), new Vector3(0.04f, 0.24f, 0.035f));
                CreatePart("Left Collar", chest, meshes.ChamferedBlock, materials.Steel,
                    new Vector3(-0.19f, 0.45f, 0f), new Vector3(0.25f, 0.09f, 0.29f),
                    Quaternion.Euler(0f, 0f, -8f));
                CreatePart("Right Collar", chest, meshes.ChamferedBlock, materials.Steel,
                    new Vector3(0.19f, 0.45f, 0f), new Vector3(0.25f, 0.09f, 0.29f),
                    Quaternion.Euler(0f, 0f, 8f));
                AddVentRow(chest, meshes, materials.Graphite, new Vector3(0f, 0.12f, -0.205f), 4, 0.08f, false);

                Transform head = CreatePivot("Head", chest, new Vector3(0f, 0.53f, 0f));
                CreatePart("Helmet Cranium", head, meshes.ChamferedBlock, materials.Ceramic,
                    new Vector3(0f, 0.08f, 0f), new Vector3(0.47f, 0.31f, 0.39f));
                CreatePart("Helmet Brow", head, meshes.ChamferedBlock, materials.SafetyOrange,
                    new Vector3(0f, 0.13f, 0.205f), new Vector3(0.49f, 0.105f, 0.055f));
                CreatePart("Protected Visor", head, meshes.ChamferedBlock, materials.EnergyCyan,
                    new Vector3(0f, 0.055f, 0.238f), new Vector3(0.34f, 0.075f, 0.035f));
                CreatePart("Helmet Chin", head, meshes.TaperedBlock, materials.Graphite,
                    new Vector3(0f, -0.075f, 0.15f), new Vector3(0.30f, 0.085f, 0.16f),
                    Quaternion.Euler(-8f, 0f, 0f));
                CreatePart("Left Audio Joint", head, meshes.Cylinder, materials.Steel,
                    new Vector3(-0.255f, 0.07f, 0f), new Vector3(0.105f, 0.045f, 0.105f),
                    Quaternion.Euler(0f, 0f, 90f));
                CreatePart("Right Audio Joint", head, meshes.Cylinder, materials.Steel,
                    new Vector3(0.255f, 0.07f, 0f), new Vector3(0.105f, 0.045f, 0.105f),
                    Quaternion.Euler(0f, 0f, 90f));
                CreatePart("Antenna Mast", head, meshes.Cylinder, materials.Graphite,
                    new Vector3(-0.13f, 0.28f, -0.02f), new Vector3(0.025f, 0.105f, 0.025f),
                    Quaternion.Euler(0f, 0f, -11f));
                CreatePart("Antenna Status Lamp", head, meshes.Sphere, materials.StatusGreen,
                    new Vector3(-0.17f, 0.39f, -0.02f), Vector3.one * 0.055f);

                Transform leftUpperArm = BuildPlayerArm("Left", chest, -1f, meshes, materials,
                    out Transform leftForearm);
                Transform rightUpperArm = BuildPlayerArm("Right", chest, 1f, meshes, materials,
                    out Transform rightForearm);
                Transform leftThigh = BuildPlayerLeg("Left", pelvis, -1f, meshes, materials,
                    out Transform leftShin, out Transform leftFoot);
                Transform rightThigh = BuildPlayerLeg("Right", pelvis, 1f, meshes, materials,
                    out Transform rightShin, out Transform rightFoot);

                Transform backpack = CreatePivot("Dash Power Pack", chest, new Vector3(0f, 0.18f, -0.25f));
                CreatePart("Backpack Chassis", backpack, meshes.ChamferedBlock, materials.Graphite,
                    Vector3.zero, new Vector3(0.38f, 0.40f, 0.20f));
                CreatePart("Backpack Orange Cap", backpack, meshes.ChamferedBlock, materials.SafetyOrange,
                    new Vector3(0f, 0.20f, -0.005f), new Vector3(0.32f, 0.085f, 0.19f));
                CreatePart("Backpack Energy Canister", backpack, meshes.Cylinder, materials.EnergyCyan,
                    new Vector3(0f, 0.015f, -0.125f), new Vector3(0.085f, 0.16f, 0.085f));
                CreatePart("Backpack Canister Collar Top", backpack, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, 0.18f, -0.125f), new Vector3(0.11f, 0.025f, 0.11f));
                CreatePart("Backpack Canister Collar Bottom", backpack, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, -0.15f, -0.125f), new Vector3(0.11f, 0.025f, 0.11f));
                Transform leftFin = CreatePivot("Left Dash Stabilizer", backpack, new Vector3(-0.22f, 0.02f, -0.08f));
                Transform rightFin = CreatePivot("Right Dash Stabilizer", backpack, new Vector3(0.22f, 0.02f, -0.08f));
                CreatePart("Left Stabilizer Shell", leftFin, meshes.Blade, materials.SafetyOrange,
                    new Vector3(-0.035f, -0.08f, -0.03f), new Vector3(0.42f, 0.32f, 0.85f),
                    Quaternion.Euler(0f, 0f, -8f));
                CreatePart("Right Stabilizer Shell", rightFin, meshes.Blade, materials.SafetyOrange,
                    new Vector3(0.035f, -0.08f, -0.03f), new Vector3(0.42f, 0.32f, 0.85f),
                    Quaternion.Euler(0f, 0f, 8f));
                CreatePart("Left Thruster Aperture", leftFin, meshes.Cylinder, materials.EnergyCyan,
                    new Vector3(-0.09f, -0.04f, -0.16f), new Vector3(0.055f, 0.025f, 0.055f),
                    Quaternion.Euler(90f, 0f, 0f));
                CreatePart("Right Thruster Aperture", rightFin, meshes.Cylinder, materials.EnergyCyan,
                    new Vector3(0.09f, -0.04f, -0.16f), new Vector3(0.055f, 0.025f, 0.055f),
                    Quaternion.Euler(90f, 0f, 0f));

                ProceduralPlayerAnimator animator = root.AddComponent<ProceduralPlayerAnimator>();
                animator.ConfigureRig(
                    rig, pelvis, chest, head,
                    leftUpperArm, rightUpperArm, leftForearm, rightForearm,
                    leftThigh, rightThigh, leftShin, rightShin, leftFoot, rightFoot,
                    backpack, leftFin, rightFin, core);
                SavePrefab(root, PlayerVisualPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform BuildPlayerArm(
            string sideName,
            Transform chest,
            float side,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform forearm)
        {
            Transform upperArm = CreatePivot(sideName + " Shoulder", chest, new Vector3(0.44f * side, 0.31f, 0f));
            CreatePart(sideName + " Shoulder Bearing", upperArm, meshes.Sphere, materials.Steel,
                Vector3.zero, Vector3.one * 0.16f);
            CreatePart(sideName + " Shoulder Shell", upperArm, meshes.ChamferedBlock, materials.SafetyOrange,
                new Vector3(0.055f * side, -0.025f, 0f), new Vector3(0.22f, 0.19f, 0.29f),
                Quaternion.Euler(0f, 0f, 9f * side));
            CreatePart(sideName + " Upper Servo", upperArm, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.19f, 0f), new Vector3(0.13f, 0.20f, 0.13f));
            CreatePart(sideName + " Upper Arm Plate", upperArm, meshes.ChamferedBlock, materials.Ceramic,
                new Vector3(0f, -0.19f, 0.095f), new Vector3(0.18f, 0.24f, 0.055f));
            forearm = CreatePivot(sideName + " Elbow", upperArm, new Vector3(0f, -0.36f, 0f));
            CreatePart(sideName + " Elbow Bearing", forearm, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Forearm Underframe", forearm, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.17f, 0.01f), new Vector3(0.12f, 0.18f, 0.12f));
            CreatePart(sideName + " Forearm Cowl", forearm, meshes.TaperedBlock, materials.SafetyOrange,
                new Vector3(0f, -0.17f, 0.075f), new Vector3(0.21f, 0.27f, 0.12f));
            CreatePart(sideName + " Wrist Collar", forearm, meshes.Cylinder, materials.Steel,
                new Vector3(0f, -0.35f, 0f), new Vector3(0.11f, 0.05f, 0.11f));
            CreatePart(sideName + " Service Hand", forearm, meshes.ChamferedBlock, materials.Graphite,
                new Vector3(0f, -0.42f, 0.045f), new Vector3(0.18f, 0.16f, 0.15f));
            return upperArm;
        }

        private static Transform BuildPlayerLeg(
            string sideName,
            Transform pelvis,
            float side,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform shin,
            out Transform foot)
        {
            Transform thigh = CreatePivot(sideName + " Hip", pelvis, new Vector3(0.20f * side, -0.08f, 0f));
            CreatePart(sideName + " Thigh Servo", thigh, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.17f, 0f), new Vector3(0.14f, 0.19f, 0.14f));
            CreatePart(sideName + " Thigh Shell", thigh, meshes.TaperedBlock, materials.Ceramic,
                new Vector3(0f, -0.16f, 0.09f), new Vector3(0.23f, 0.26f, 0.12f));
            CreatePart(sideName + " Thigh Orange Rail", thigh, meshes.ChamferedBlock, materials.SafetyOrange,
                new Vector3(0.095f * side, -0.16f, 0.105f), new Vector3(0.045f, 0.23f, 0.04f));
            shin = CreatePivot(sideName + " Knee", thigh, new Vector3(0f, -0.35f, 0f));
            CreatePart(sideName + " Knee Bearing", shin, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.14f, 0.08f, 0.14f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Knee Guard", shin, meshes.TaperedBlock, materials.SafetyOrange,
                new Vector3(0f, -0.015f, 0.13f), new Vector3(0.19f, 0.15f, 0.08f),
                Quaternion.Euler(-9f, 0f, 0f));
            CreatePart(sideName + " Shin Piston", shin, meshes.Capsule, materials.Steel,
                new Vector3(0f, -0.18f, -0.025f), new Vector3(0.095f, 0.20f, 0.095f));
            CreatePart(sideName + " Shin Armor", shin, meshes.TaperedBlock, materials.Ceramic,
                new Vector3(0f, -0.19f, 0.09f), new Vector3(0.21f, 0.29f, 0.12f));
            foot = CreatePivot(sideName + " Ankle", shin, new Vector3(0f, -0.37f, 0f));
            CreatePart(sideName + " Ankle Collar", foot, meshes.Cylinder, materials.Graphite,
                Vector3.zero, new Vector3(0.11f, 0.065f, 0.11f));
            CreatePart(sideName + " Magnetic Boot", foot, meshes.ChamferedBlock, materials.Graphite,
                new Vector3(0f, -0.07f, 0.09f), new Vector3(0.27f, 0.16f, 0.39f));
            CreatePart(sideName + " Boot Toe", foot, meshes.TaperedBlock, materials.SafetyOrange,
                new Vector3(0f, -0.045f, 0.25f), new Vector3(0.25f, 0.12f, 0.20f),
                Quaternion.Euler(-3f, 0f, 0f));
            CreatePart(sideName + " Boot Energy Sole", foot, meshes.ChamferedBlock, materials.EnergyCyan,
                new Vector3(0f, -0.155f, 0.09f), new Vector3(0.22f, 0.025f, 0.34f));
            return thigh;
        }

        private static void BuildSaboteurVisual(CharacterMeshes meshes, CharacterMaterials materials)
        {
            GameObject root = new GameObject("PF_Enemy_Saboteur_Cutter_Visual");
            try
            {
                Transform rig = CreatePivot("Procedural Rig", root.transform, Vector3.zero);
                Transform pelvis = CreatePivot("Pelvis", rig, new Vector3(0f, 0.91f, 0f));
                CreatePart("Needle Pelvis", pelvis, meshes.TaperedBlock, materials.Graphite,
                    Vector3.zero, new Vector3(0.31f, 0.25f, 0.26f));
                CreatePart("Pelvis Blade Guard", pelvis, meshes.ChamferedBlock, materials.EnemyPurple,
                    new Vector3(0f, 0.08f, 0.02f), new Vector3(0.40f, 0.09f, 0.29f));
                CreatePart("Left Hip Ring", pelvis, meshes.Cylinder, materials.Steel,
                    new Vector3(-0.20f, -0.06f, 0f), new Vector3(0.10f, 0.05f, 0.10f),
                    Quaternion.Euler(0f, 0f, 90f));
                CreatePart("Right Hip Ring", pelvis, meshes.Cylinder, materials.Steel,
                    new Vector3(0.20f, -0.06f, 0f), new Vector3(0.10f, 0.05f, 0.10f),
                    Quaternion.Euler(0f, 0f, 90f));

                Transform chest = CreatePivot("Predator Chest", rig, new Vector3(0f, 1.12f, 0f),
                    Quaternion.Euler(7f, 0f, 0f));
                CreatePart("Thorax Underframe", chest, meshes.TaperedBlock, materials.Graphite,
                    new Vector3(0f, 0.17f, 0f), new Vector3(0.47f, 0.58f, 0.31f));
                CreatePart("Left Thorax Blade", chest, meshes.Blade, materials.EnemyPurple,
                    new Vector3(-0.17f, 0.40f, 0.05f), new Vector3(0.72f, 0.29f, 1.8f),
                    Quaternion.Euler(0f, 0f, 158f));
                CreatePart("Right Thorax Blade", chest, meshes.Blade, materials.EnemyPurple,
                    new Vector3(0.17f, 0.40f, 0.05f), new Vector3(0.72f, 0.29f, 1.8f),
                    Quaternion.Euler(0f, 0f, -158f));
                CreatePart("Rib Plate Left", chest, meshes.ChamferedBlock, materials.EnemyPurple,
                    new Vector3(-0.14f, 0.15f, 0.18f), new Vector3(0.18f, 0.31f, 0.05f),
                    Quaternion.Euler(0f, -9f, -5f));
                CreatePart("Rib Plate Right", chest, meshes.ChamferedBlock, materials.EnemyPurple,
                    new Vector3(0.14f, 0.15f, 0.18f), new Vector3(0.18f, 0.31f, 0.05f),
                    Quaternion.Euler(0f, 9f, 5f));
                Transform core = CreatePart("Saboteur Threat Core", chest, meshes.ChamferedBlock, materials.ThreatOrange,
                    new Vector3(0f, 0.19f, 0.225f), new Vector3(0.075f, 0.24f, 0.035f));

                Transform back = CreatePivot("Segmented Spine", chest, new Vector3(0f, 0.15f, -0.21f));
                for (int index = 0; index < 5; index++)
                {
                    CreatePart("Spine Segment " + (index + 1), back, meshes.ChamferedBlock,
                        index % 2 == 0 ? materials.Steel : materials.Graphite,
                        new Vector3(0f, 0.22f - index * 0.11f, -index * 0.015f),
                        new Vector3(0.12f, 0.065f, 0.10f), Quaternion.Euler(index * 2f, 0f, 0f));
                }
                CreatePart("Spine Tail Fin", back, meshes.Blade, materials.EnemyPurple,
                    new Vector3(0f, -0.16f, -0.02f), new Vector3(0.55f, 0.24f, 1.2f));

                Transform head = CreatePivot("Wedge Head", chest, new Vector3(0f, 0.58f, 0.04f));
                CreatePart("Head Armor", head, meshes.TaperedBlock, materials.Graphite,
                    new Vector3(0f, 0.04f, 0f), new Vector3(0.37f, 0.24f, 0.42f),
                    Quaternion.Euler(-8f, 0f, 0f));
                CreatePart("Crown Servo Mount", head, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, 0.215f, -0.05f), new Vector3(0.08f, 0.075f, 0.08f));
                CreatePart("Crown Blade", head, meshes.Blade, materials.EnemyPurple,
                    new Vector3(0f, 0.27f, -0.05f), new Vector3(0.62f, 0.26f, 1.4f),
                    Quaternion.Euler(0f, 0f, 180f));
                CreatePart("Single Threat Visor", head, meshes.ChamferedBlock, materials.ThreatOrange,
                    new Vector3(0f, 0.055f, 0.235f), new Vector3(0.28f, 0.05f, 0.028f));
                CreatePart("Left Jaw Fang", head, meshes.Blade, materials.Steel,
                    new Vector3(-0.12f, -0.04f, 0.16f), new Vector3(0.25f, 0.13f, 0.44f),
                    Quaternion.Euler(-12f, 0f, -7f));
                CreatePart("Right Jaw Fang", head, meshes.Blade, materials.Steel,
                    new Vector3(0.12f, -0.04f, 0.16f), new Vector3(0.25f, 0.13f, 0.44f),
                    Quaternion.Euler(-12f, 0f, 7f));

                Transform leftUpperArm = BuildSaboteurArm("Left", chest, -1f, false, meshes, materials,
                    out Transform leftForearm, out Transform leftWeapon);
                Transform rightUpperArm = BuildSaboteurArm("Right", chest, 1f, true, meshes, materials,
                    out Transform rightForearm, out Transform rightWeapon);
                Transform leftThigh = BuildSaboteurLeg("Left", pelvis, -1f, meshes, materials,
                    out Transform leftShin, out Transform leftFoot);
                Transform rightThigh = BuildSaboteurLeg("Right", pelvis, 1f, meshes, materials,
                    out Transform rightShin, out Transform rightFoot);

                ProceduralEnemyAnimator animator = root.AddComponent<ProceduralEnemyAnimator>();
                animator.ConfigureRig(
                    ProceduralEnemyRigKind.Saboteur,
                    rig, pelvis, chest, head,
                    leftUpperArm, rightUpperArm, leftForearm, rightForearm,
                    leftThigh, rightThigh, leftShin, rightShin, leftFoot, rightFoot,
                    leftWeapon, rightWeapon, core, back, 0.95f, 36f);
                SavePrefab(root, SaboteurVisualPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform BuildSaboteurArm(
            string sideName,
            Transform chest,
            float side,
            bool forked,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform forearm,
            out Transform weapon)
        {
            Transform upperArm = CreatePivot(sideName + " Shoulder", chest, new Vector3(0.37f * side, 0.38f, 0f),
                Quaternion.Euler(0f, 0f, 7f * side));
            CreatePart(sideName + " Shoulder Spike Mount", upperArm, meshes.Cylinder, materials.Graphite,
                new Vector3(0.04f * side, 0.08f, 0f), new Vector3(0.055f, 0.105f, 0.055f),
                Quaternion.Euler(0f, 0f, -10f * side));
            CreatePart(sideName + " Shoulder Spike", upperArm, meshes.Blade, materials.EnemyPurple,
                new Vector3(0.08f * side, 0.15f, 0f), new Vector3(0.52f, 0.27f, 1.2f),
                Quaternion.Euler(0f, 0f, 180f - 18f * side));
            CreatePart(sideName + " Shoulder Bearing", upperArm, meshes.Sphere, materials.Steel,
                Vector3.zero, Vector3.one * 0.12f);
            CreatePart(sideName + " Upper Arm Strut", upperArm, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.21f, 0f), new Vector3(0.085f, 0.23f, 0.085f));
            CreatePart(sideName + " Upper Arm Blade Plate", upperArm, meshes.Blade, materials.EnemyPurple,
                new Vector3(0f, -0.10f, 0.07f), new Vector3(0.45f, 0.30f, 0.52f),
                Quaternion.Euler(-7f, 0f, 0f));
            forearm = CreatePivot(sideName + " Elbow", upperArm, new Vector3(0f, -0.41f, 0f));
            CreatePart(sideName + " Elbow Ring", forearm, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.105f, 0.045f, 0.105f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Forearm Strut", forearm, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.23f, 0f), new Vector3(0.09f, 0.25f, 0.09f));
            CreatePart(sideName + " Forearm Casing", forearm, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.22f, 0.065f), new Vector3(0.18f, 0.37f, 0.13f));
            CreatePart(sideName + " Blade Conduit", forearm, meshes.ChamferedBlock, materials.ThreatOrange,
                new Vector3(0.07f * side, -0.25f, 0.14f), new Vector3(0.025f, 0.27f, 0.025f));
            weapon = CreatePivot(sideName + " Knife Hand", forearm, new Vector3(0f, -0.47f, 0.035f));
            CreatePart(sideName + " Blade Guard", weapon, meshes.ChamferedBlock, materials.Steel,
                new Vector3(0f, -0.01f, 0.01f), new Vector3(0.24f, 0.08f, 0.16f));
            CreatePart(sideName + " Primary Knife", weapon, meshes.Blade, materials.BladeSteel,
                new Vector3(0f, -0.02f, 0.07f), new Vector3(0.86f, 0.67f, 1.6f),
                Quaternion.Euler(-16f, 0f, 0f));
            CreatePart(sideName + " Hot Blade Edge", weapon, meshes.Blade, materials.ThreatOrange,
                new Vector3(0.075f * side, -0.06f, 0.105f), new Vector3(0.13f, 0.69f, 1.05f),
                Quaternion.Euler(-16f, 0f, -4f * side));
            CreateBladeTrail(sideName + " Knife Trail", weapon,
                new Vector3(0.075f * side, -0.74f, 0.12f), materials.ThreatOrange);
            if (forked)
            {
                CreatePart(sideName + " Fork Blade", weapon, meshes.Blade, materials.BladeSteel,
                    new Vector3(0.13f, -0.03f, 0.015f), new Vector3(0.48f, 0.58f, 1.05f),
                    Quaternion.Euler(-12f, 0f, -12f));
            }

            return upperArm;
        }

        private static Transform BuildSaboteurLeg(
            string sideName,
            Transform pelvis,
            float side,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform shin,
            out Transform foot)
        {
            Transform thigh = CreatePivot(sideName + " Hip", pelvis, new Vector3(0.18f * side, -0.07f, 0f));
            CreatePart(sideName + " Thigh Strut", thigh, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.18f, 0f), new Vector3(0.09f, 0.20f, 0.09f));
            CreatePart(sideName + " Thigh Razor", thigh, meshes.Blade, materials.EnemyPurple,
                new Vector3(0.04f * side, -0.08f, 0.075f), new Vector3(0.47f, 0.29f, 0.50f),
                Quaternion.Euler(-6f, 0f, 0f));
            shin = CreatePivot(sideName + " Knee", thigh, new Vector3(0f, -0.36f, 0f));
            CreatePart(sideName + " Knee Ring", shin, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.11f, 0.045f, 0.11f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Knee Point", shin, meshes.Blade, materials.EnemyPurple,
                new Vector3(0f, 0.02f, 0.14f), new Vector3(0.43f, 0.21f, 0.47f),
                Quaternion.Euler(18f, 0f, 0f));
            CreatePart(sideName + " Shin Strut", shin, meshes.Capsule, materials.Graphite,
                new Vector3(0f, -0.19f, -0.02f), new Vector3(0.08f, 0.21f, 0.08f));
            CreatePart(sideName + " Shin Blade", shin, meshes.Blade, materials.EnemyPurple,
                new Vector3(0f, -0.12f, 0.08f), new Vector3(0.47f, 0.35f, 0.58f));
            foot = CreatePivot(sideName + " Ankle", shin, new Vector3(0f, -0.38f, 0f));
            CreatePart(sideName + " Talon Foot", foot, meshes.TaperedBlock, materials.Graphite,
                new Vector3(0f, -0.055f, 0.11f), new Vector3(0.20f, 0.13f, 0.36f),
                Quaternion.Euler(-5f, 0f, 0f));
            CreatePart(sideName + " Talon", foot, meshes.Blade, materials.BladeSteel,
                new Vector3(0f, -0.015f, 0.31f), new Vector3(0.34f, 0.24f, 0.44f),
                Quaternion.Euler(72f, 0f, 0f));
            return thigh;
        }

        private static void BuildArmoredVisual(CharacterMeshes meshes, CharacterMaterials materials)
        {
            GameObject root = new GameObject("PF_Enemy_Armored_FoundryBrute_Visual");
            try
            {
                Transform rig = CreatePivot("Procedural Rig", root.transform, Vector3.zero);
                Transform pelvis = CreatePivot("Pelvis", rig, new Vector3(0f, 1.50f, 0f));
                CreatePart("Pelvis Pressure Frame", pelvis, meshes.ChamferedBlock, materials.Graphite,
                    Vector3.zero, new Vector3(1.10f, 0.50f, 0.72f));
                CreatePart("Pelvis Armor Cap", pelvis, meshes.TaperedBlock, materials.EnemyPurple,
                    new Vector3(0f, 0.15f, 0.02f), new Vector3(1.25f, 0.32f, 0.76f));
                CreatePart("Pelvis Furnace Band", pelvis, meshes.ChamferedBlock, materials.FurnaceOrange,
                    new Vector3(0f, 0.06f, 0.39f), new Vector3(0.54f, 0.08f, 0.035f));
                CreatePart("Left Hip Bearing", pelvis, meshes.Cylinder, materials.Steel,
                    new Vector3(-0.63f, -0.12f, 0f), new Vector3(0.27f, 0.11f, 0.27f),
                    Quaternion.Euler(0f, 0f, 90f));
                CreatePart("Right Hip Bearing", pelvis, meshes.Cylinder, materials.Steel,
                    new Vector3(0.63f, -0.12f, 0f), new Vector3(0.27f, 0.11f, 0.27f),
                    Quaternion.Euler(0f, 0f, 90f));

                Transform chest = CreatePivot("Foundry Thorax", rig, new Vector3(0f, 1.62f, 0f));
                CreatePart("Thorax Pressure Vessel", chest, meshes.TaperedBlock, materials.Graphite,
                    new Vector3(0f, 0.57f, 0f), new Vector3(1.55f, 1.48f, 0.88f));
                CreatePart("Left Chest Armor", chest, meshes.ChamferedBlock, materials.EnemyPurple,
                    new Vector3(-0.43f, 0.65f, 0.47f), new Vector3(0.62f, 0.86f, 0.11f),
                    Quaternion.Euler(0f, -7f, -4f));
                CreatePart("Right Chest Armor", chest, meshes.ChamferedBlock, materials.EnemyPurple,
                    new Vector3(0.43f, 0.65f, 0.47f), new Vector3(0.62f, 0.86f, 0.11f),
                    Quaternion.Euler(0f, 7f, 4f));
                CreatePart("Sternum Guard", chest, meshes.TaperedBlock, materials.Steel,
                    new Vector3(0f, 0.78f, 0.51f), new Vector3(0.44f, 0.75f, 0.10f));
                Transform core = CreatePart("Furnace Pressure Core", chest, meshes.Cylinder, materials.FurnaceOrange,
                    new Vector3(0f, 0.52f, 0.58f), new Vector3(0.30f, 0.055f, 0.30f),
                    Quaternion.Euler(90f, 0f, 0f));
                CreatePart("Core Outer Ring", chest, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, 0.52f, 0.555f), new Vector3(0.41f, 0.045f, 0.41f),
                    Quaternion.Euler(90f, 0f, 0f));
                CreatePart("Core Inner Aperture", chest, meshes.Cylinder, materials.Graphite,
                    new Vector3(0f, 0.52f, 0.61f), new Vector3(0.17f, 0.025f, 0.17f),
                    Quaternion.Euler(90f, 0f, 0f));
                for (int index = -2; index <= 2; index++)
                {
                    CreatePart("Heat Vent " + (index + 3), chest, meshes.ChamferedBlock,
                        index == 0 ? materials.FurnaceOrange : materials.Graphite,
                        new Vector3(index * 0.18f, 1.12f, 0.49f), new Vector3(0.10f, 0.22f, 0.04f),
                        Quaternion.Euler(0f, 0f, index * 3f));
                }

                Transform back = CreatePivot("Back Pressure Assembly", chest, new Vector3(0f, 0.62f, -0.50f));
                CreatePart("Pressure Tank", back, meshes.Cylinder, materials.EnemyPurple,
                    new Vector3(0f, 0f, -0.08f), new Vector3(0.36f, 0.55f, 0.36f));
                CreatePart("Tank Top Collar", back, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, 0.56f, -0.08f), new Vector3(0.42f, 0.09f, 0.42f));
                CreatePart("Tank Bottom Collar", back, meshes.Cylinder, materials.Steel,
                    new Vector3(0f, -0.56f, -0.08f), new Vector3(0.42f, 0.09f, 0.42f));
                CreatePart("Left Exhaust", back, meshes.Cylinder, materials.Graphite,
                    new Vector3(-0.42f, 0.44f, -0.06f), new Vector3(0.16f, 0.51f, 0.16f),
                    Quaternion.Euler(0f, 0f, -8f));
                CreatePart("Right Exhaust", back, meshes.Cylinder, materials.Graphite,
                    new Vector3(0.42f, 0.44f, -0.06f), new Vector3(0.16f, 0.51f, 0.16f),
                    Quaternion.Euler(0f, 0f, 8f));
                CreatePart("Left Exhaust Heat", back, meshes.Cylinder, materials.FurnaceOrange,
                    new Vector3(-0.49f, 0.94f, -0.06f), new Vector3(0.12f, 0.08f, 0.12f),
                    Quaternion.Euler(0f, 0f, -8f));
                CreatePart("Right Exhaust Heat", back, meshes.Cylinder, materials.FurnaceOrange,
                    new Vector3(0.49f, 0.94f, -0.06f), new Vector3(0.12f, 0.08f, 0.12f),
                    Quaternion.Euler(0f, 0f, 8f));

                Transform head = CreatePivot("Armored Head", chest, new Vector3(0f, 1.36f, 0.08f));
                CreatePart("Head Cradle", head, meshes.ChamferedBlock, materials.Graphite,
                    new Vector3(0f, 0.02f, 0f), new Vector3(0.70f, 0.38f, 0.52f));
                CreatePart("Head Brow Armor", head, meshes.TaperedBlock, materials.EnemyPurple,
                    new Vector3(0f, 0.12f, 0.25f), new Vector3(0.76f, 0.28f, 0.13f),
                    Quaternion.Euler(-8f, 0f, 0f));
                CreatePart("Furnace Visor", head, meshes.ChamferedBlock, materials.FurnaceOrange,
                    new Vector3(0f, 0.01f, 0.31f), new Vector3(0.45f, 0.075f, 0.035f));
                CreatePart("Left Horn Socket", head, meshes.Cylinder, materials.Steel,
                    new Vector3(-0.27f, 0.25f, 0f), new Vector3(0.14f, 0.09f, 0.14f),
                    Quaternion.Euler(0f, 0f, -12f));
                CreatePart("Right Horn Socket", head, meshes.Cylinder, materials.Steel,
                    new Vector3(0.27f, 0.25f, 0f), new Vector3(0.14f, 0.09f, 0.14f),
                    Quaternion.Euler(0f, 0f, 12f));
                CreatePart("Left Crown Horn", head, meshes.Blade, materials.EnemyPurple,
                    new Vector3(-0.27f, 0.28f, 0f), new Vector3(1.2f, 0.50f, 1.75f),
                    Quaternion.Euler(0f, 0f, 164f));
                CreatePart("Right Crown Horn", head, meshes.Blade, materials.EnemyPurple,
                    new Vector3(0.27f, 0.28f, 0f), new Vector3(1.2f, 0.50f, 1.75f),
                    Quaternion.Euler(0f, 0f, -164f));

                Transform leftUpperArm = BuildArmoredArm("Left", chest, -1f, false, meshes, materials,
                    out Transform leftForearm, out Transform leftWeapon);
                Transform rightUpperArm = BuildArmoredArm("Right", chest, 1f, true, meshes, materials,
                    out Transform rightForearm, out Transform rightWeapon);
                Transform leftThigh = BuildArmoredLeg("Left", pelvis, -1f, meshes, materials,
                    out Transform leftShin, out Transform leftFoot);
                Transform rightThigh = BuildArmoredLeg("Right", pelvis, 1f, meshes, materials,
                    out Transform rightShin, out Transform rightFoot);

                ProceduralEnemyAnimator animator = root.AddComponent<ProceduralEnemyAnimator>();
                animator.ConfigureRig(
                    ProceduralEnemyRigKind.Armored,
                    rig, pelvis, chest, head,
                    leftUpperArm, rightUpperArm, leftForearm, rightForearm,
                    leftThigh, rightThigh, leftShin, rightShin, leftFoot, rightFoot,
                    leftWeapon, rightWeapon, core, back, 2.35f, 26f);
                SavePrefab(root, ArmoredVisualPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform BuildArmoredArm(
            string sideName,
            Transform chest,
            float side,
            bool hammer,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform forearm,
            out Transform weapon)
        {
            Transform upperArm = CreatePivot(sideName + " Shoulder", chest, new Vector3(1.02f * side, 1.00f, 0f));
            CreatePart(sideName + " Shoulder Bearing", upperArm, meshes.Sphere, materials.Steel,
                Vector3.zero, Vector3.one * 0.34f);
            CreatePart(sideName + " Shoulder Arch", upperArm, meshes.ChamferedBlock, materials.EnemyPurple,
                new Vector3(0.13f * side, 0.12f, 0f), new Vector3(0.70f, 0.54f, 0.82f),
                Quaternion.Euler(0f, 0f, 7f * side));
            CreatePart(sideName + " Shoulder Heat Seam", upperArm, meshes.ChamferedBlock, materials.FurnaceOrange,
                new Vector3(0.13f * side, 0.12f, 0.425f), new Vector3(0.40f, 0.065f, 0.035f));
            CreatePart(sideName + " Upper Arm Piston", upperArm, meshes.Capsule, materials.Steel,
                new Vector3(0f, -0.39f, -0.04f), new Vector3(0.22f, 0.42f, 0.22f));
            CreatePart(sideName + " Upper Arm Armor", upperArm, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.34f, 0.18f), new Vector3(0.50f, 0.62f, 0.35f));
            forearm = CreatePivot(sideName + " Elbow", upperArm, new Vector3(0f, -0.73f, 0f));
            CreatePart(sideName + " Elbow Bearing", forearm, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.29f, 0.14f, 0.29f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Forearm Piston", forearm, meshes.Capsule, materials.Steel,
                new Vector3(0f, -0.41f, -0.07f), new Vector3(0.23f, 0.45f, 0.23f));
            CreatePart(sideName + " Forearm Pressure Shell", forearm, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.40f, 0.16f), new Vector3(0.66f, 0.76f, 0.48f));
            CreatePart(sideName + " Forearm Heat Rail", forearm, meshes.ChamferedBlock, materials.FurnaceOrange,
                new Vector3(0.28f * side, -0.40f, 0.41f), new Vector3(0.065f, 0.52f, 0.035f));
            weapon = CreatePivot(sideName + (hammer ? " Pile Driver" : " Crusher Fist"), forearm,
                new Vector3(0f, -0.82f, 0.03f));
            if (hammer)
            {
                CreatePart(sideName + " Hammer Block", weapon, meshes.ChamferedBlock, materials.Graphite,
                    new Vector3(0f, -0.15f, 0.07f), new Vector3(0.85f, 0.43f, 0.72f));
                CreatePart(sideName + " Hammer Face", weapon, meshes.ChamferedBlock, materials.BladeSteel,
                    new Vector3(0f, -0.39f, 0.08f), new Vector3(0.75f, 0.09f, 0.63f));
                CreatePart(sideName + " Hammer Furnace Slit", weapon, meshes.ChamferedBlock, materials.FurnaceOrange,
                    new Vector3(0f, -0.15f, 0.445f), new Vector3(0.42f, 0.10f, 0.035f));
            }
            else
            {
                CreatePart(sideName + " Crusher Palm", weapon, meshes.ChamferedBlock, materials.Graphite,
                    new Vector3(0f, -0.14f, 0.04f), new Vector3(0.70f, 0.43f, 0.63f));
                for (int index = -1; index <= 1; index++)
                {
                    CreatePart(sideName + " Crusher Talon " + (index + 2), weapon, meshes.Blade, materials.BladeSteel,
                        new Vector3(index * 0.22f, -0.31f, 0.20f), new Vector3(0.73f, 0.31f, 0.62f),
                        Quaternion.Euler(-16f, 0f, index * -4f));
                }
            }

            return upperArm;
        }

        private static Transform BuildArmoredLeg(
            string sideName,
            Transform pelvis,
            float side,
            CharacterMeshes meshes,
            CharacterMaterials materials,
            out Transform shin,
            out Transform foot)
        {
            Transform thigh = CreatePivot(sideName + " Hip", pelvis, new Vector3(0.55f * side, -0.14f, 0f));
            CreatePart(sideName + " Thigh Piston", thigh, meshes.Capsule, materials.Steel,
                new Vector3(0f, -0.34f, -0.04f), new Vector3(0.22f, 0.38f, 0.22f));
            CreatePart(sideName + " Thigh Armor", thigh, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.32f, 0.18f), new Vector3(0.58f, 0.58f, 0.41f));
            CreatePart(sideName + " Thigh Heat Seam", thigh, meshes.ChamferedBlock, materials.FurnaceOrange,
                new Vector3(0.24f * side, -0.31f, 0.395f), new Vector3(0.055f, 0.36f, 0.032f));
            shin = CreatePivot(sideName + " Knee", thigh, new Vector3(0f, -0.65f, 0f));
            CreatePart(sideName + " Knee Bearing", shin, meshes.Cylinder, materials.Steel,
                Vector3.zero, new Vector3(0.27f, 0.12f, 0.27f), Quaternion.Euler(0f, 0f, 90f));
            CreatePart(sideName + " Knee Ram", shin, meshes.TaperedBlock, materials.Graphite,
                new Vector3(0f, 0f, 0.32f), new Vector3(0.39f, 0.29f, 0.28f),
                Quaternion.Euler(-10f, 0f, 0f));
            CreatePart(sideName + " Shin Piston", shin, meshes.Capsule, materials.Steel,
                new Vector3(0f, -0.34f, -0.07f), new Vector3(0.20f, 0.38f, 0.20f));
            CreatePart(sideName + " Shin Armor", shin, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.33f, 0.16f), new Vector3(0.53f, 0.61f, 0.39f));
            foot = CreatePivot(sideName + " Ankle", shin, new Vector3(0f, -0.68f, 0f));
            CreatePart(sideName + " Ankle Bearing", foot, meshes.Cylinder, materials.Graphite,
                Vector3.zero, new Vector3(0.24f, 0.11f, 0.24f));
            CreatePart(sideName + " Foundry Boot", foot, meshes.ChamferedBlock, materials.Graphite,
                new Vector3(0f, -0.12f, 0.18f), new Vector3(0.68f, 0.29f, 0.89f));
            CreatePart(sideName + " Armored Toe", foot, meshes.TaperedBlock, materials.EnemyPurple,
                new Vector3(0f, -0.08f, 0.58f), new Vector3(0.63f, 0.23f, 0.37f),
                Quaternion.Euler(-3f, 0f, 0f));
            CreatePart(sideName + " Heat Sole", foot, meshes.ChamferedBlock, materials.FurnaceOrange,
                new Vector3(0f, -0.28f, 0.17f), new Vector3(0.54f, 0.035f, 0.75f));
            return thigh;
        }

        private static CharacterMeshes BuildMeshes()
        {
            Mesh chamferedBlock = CreateOrUpdateMesh(ChamferedBlockMeshPath, CreateChamferedBlockMesh);
            Mesh taperedBlock = CreateOrUpdateMesh(TaperedBlockMeshPath, CreateTaperedBlockMesh);
            Mesh blade = CreateOrUpdateMesh(BladeMeshPath, CreateBladeMesh);
            return new CharacterMeshes
            {
                ChamferedBlock = chamferedBlock,
                TaperedBlock = taperedBlock,
                Blade = blade,
                Sphere = GetPrimitiveMesh(PrimitiveType.Sphere),
                Cylinder = GetPrimitiveMesh(PrimitiveType.Cylinder),
                Capsule = GetPrimitiveMesh(PrimitiveType.Capsule)
            };
        }

        private static CharacterMaterials BuildMaterials()
        {
            return new CharacterMaterials
            {
                Graphite = CreateOrUpdateMaterial("M_Character_Graphite", new Color(0.043f, 0.055f, 0.060f),
                    0.74f, 0.43f, Color.black),
                SafetyOrange = CreateOrUpdateMaterial("M_Character_SafetyOrange", new Color(0.946f, 0.38f, 0.043f),
                    0.46f, 0.38f, new Color(0.12f, 0.018f, 0f)),
                Ceramic = CreateOrUpdateMaterial("M_Character_Ceramic", new Color(0.48f, 0.55f, 0.57f),
                    0.62f, 0.58f, Color.black),
                Steel = CreateOrUpdateMaterial("M_Character_Steel", new Color(0.46f, 0.56f, 0.62f),
                    0.86f, 0.67f, Color.black),
                BladeSteel = CreateOrUpdateMaterial("M_Character_BladeSteel", new Color(0.63f, 0.72f, 0.76f),
                    0.92f, 0.79f, Color.black),
                EnergyCyan = CreateOrUpdateMaterial("M_Character_EnergyCyan", new Color(0.08f, 0.72f, 0.92f),
                    0.18f, 0.76f, new Color(0.08f, 2.5f, 4.2f)),
                StatusGreen = CreateOrUpdateMaterial("M_Character_StatusGreen", new Color(0.12f, 0.85f, 0.42f),
                    0.12f, 0.64f, new Color(0.05f, 2.0f, 0.38f)),
                EnemyPurple = CreateOrUpdateMaterial("M_Character_EnemyPurple", new Color(0.30f, 0.075f, 0.48f),
                    0.68f, 0.52f, new Color(0.035f, 0f, 0.07f)),
                ThreatOrange = CreateOrUpdateMaterial("M_Character_ThreatOrange", new Color(1f, 0.17f, 0.025f),
                    0.15f, 0.68f, new Color(4.2f, 0.22f, 0.01f)),
                FurnaceOrange = CreateOrUpdateMaterial("M_Character_FurnaceOrange", new Color(1f, 0.24f, 0.025f),
                    0.16f, 0.65f, new Color(4.8f, 0.36f, 0.015f))
            };
        }

        private static void AddVentRow(
            Transform parent,
            CharacterMeshes meshes,
            Material material,
            Vector3 center,
            int count,
            float spacing,
            bool vertical)
        {
            float offset = (count - 1) * spacing * 0.5f;
            for (int index = 0; index < count; index++)
            {
                Vector3 position = center + (vertical
                    ? Vector3.up * (index * spacing - offset)
                    : Vector3.right * (index * spacing - offset));
                CreatePart("Vent " + (index + 1), parent, meshes.ChamferedBlock, material, position,
                    vertical ? new Vector3(0.07f, 0.025f, 0.025f) : new Vector3(0.035f, 0.15f, 0.025f));
            }
        }

        private static Transform CreatePivot(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion? localRotation = null)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            pivot.transform.localRotation = localRotation ?? Quaternion.identity;
            return pivot.transform;
        }

        private static Transform CreatePart(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion? localRotation = null)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation ?? Quaternion.identity;
            part.transform.localScale = localScale;
            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return part.transform;
        }

        private static void CreateBladeTrail(
            string name,
            Transform parent,
            Vector3 localPosition,
            Material material)
        {
            GameObject trailObject = new GameObject(name);
            trailObject.transform.SetParent(parent, false);
            trailObject.transform.localPosition = localPosition;
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.13f;
            trail.startWidth = 0.065f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.018f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not save geometric character visual at " + path + ".");
            }
        }

        private static Material CreateOrUpdateMaterial(
            string assetName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emission)
        {
            string path = MaterialFolder + "/" + assetName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No supported Lit shader is available for character materials.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
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

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            bool emissive = emission.maxColorComponent > 0.001f;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissive ? emission : Color.black);
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateMesh(string path, Func<Mesh> factory)
        {
            Mesh generated = factory();
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            GameObject temporary = GameObject.CreatePrimitive(primitiveType);
            try
            {
                return temporary.GetComponent<MeshFilter>().sharedMesh;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static Mesh CreateChamferedBlockMesh()
        {
            Vector2[] outline =
            {
                new Vector2(-0.36f, -0.5f), new Vector2(0.36f, -0.5f),
                new Vector2(0.5f, -0.36f), new Vector2(0.5f, 0.36f),
                new Vector2(0.36f, 0.5f), new Vector2(-0.36f, 0.5f),
                new Vector2(-0.5f, 0.36f), new Vector2(-0.5f, -0.36f)
            };
            return CreateExtrudedOutlineMesh("SM_Character_ChamferedBlock", outline, outline);
        }

        private static Mesh CreateTaperedBlockMesh()
        {
            Vector2[] bottom =
            {
                new Vector2(-0.42f, -0.5f), new Vector2(0.42f, -0.5f),
                new Vector2(0.5f, -0.42f), new Vector2(0.5f, 0.42f),
                new Vector2(0.42f, 0.5f), new Vector2(-0.42f, 0.5f),
                new Vector2(-0.5f, 0.42f), new Vector2(-0.5f, -0.42f)
            };
            Vector2[] top =
            {
                new Vector2(-0.29f, -0.40f), new Vector2(0.29f, -0.40f),
                new Vector2(0.39f, -0.29f), new Vector2(0.39f, 0.29f),
                new Vector2(0.29f, 0.40f), new Vector2(-0.29f, 0.40f),
                new Vector2(-0.39f, 0.29f), new Vector2(-0.39f, -0.29f)
            };
            return CreateExtrudedOutlineMesh("SM_Character_TaperedBlock", bottom, top);
        }

        private static Mesh CreateExtrudedOutlineMesh(string name, Vector2[] bottom, Vector2[] top)
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
                AddQuad(vertices, triangles, bottomCurrent, topCurrent, topNext, bottomNext);
                AddTriangle(vertices, triangles, Vector3.up * 0.5f, topNext, topCurrent);
                AddTriangle(vertices, triangles, Vector3.down * 0.5f, bottomCurrent, bottomNext);
            }

            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBladeMesh()
        {
            Vector2[] profile =
            {
                new Vector2(-0.08f, 0f), new Vector2(0.08f, 0f),
                new Vector2(0.16f, -0.20f), new Vector2(0.09f, -0.72f),
                new Vector2(0f, -1f), new Vector2(-0.09f, -0.72f),
                new Vector2(-0.16f, -0.20f)
            };
            const float halfThickness = 0.045f;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3 frontCenter = new Vector3(0f, -0.42f, halfThickness);
            Vector3 backCenter = new Vector3(0f, -0.42f, -halfThickness);
            for (int index = 0; index < profile.Length; index++)
            {
                int next = (index + 1) % profile.Length;
                Vector3 frontCurrent = new Vector3(profile[index].x, profile[index].y, halfThickness);
                Vector3 frontNext = new Vector3(profile[next].x, profile[next].y, halfThickness);
                Vector3 backCurrent = new Vector3(profile[index].x, profile[index].y, -halfThickness);
                Vector3 backNext = new Vector3(profile[next].x, profile[next].y, -halfThickness);
                AddTriangle(vertices, triangles, frontCenter, frontCurrent, frontNext);
                AddTriangle(vertices, triangles, backCenter, backNext, backCurrent);
                AddQuad(vertices, triangles, frontCurrent, backCurrent, backNext, frontNext);
            }

            Mesh mesh = new Mesh { name = "SM_Character_IndustrialBlade" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(
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

        private static void AddTriangle(
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

        private static void CopyAssetOnce(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null ||
                AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                throw new InvalidOperationException(
                    "Could not back up legacy character asset from " + sourcePath + " to " + destinationPath + ".");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder(OldFolder);
            EnsureFolder(OldPlayerFolder);
            EnsureFolder(OldEnemyFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private struct CharacterMeshes
        {
            public Mesh ChamferedBlock;
            public Mesh TaperedBlock;
            public Mesh Blade;
            public Mesh Sphere;
            public Mesh Cylinder;
            public Mesh Capsule;
        }

        private struct CharacterMaterials
        {
            public Material Graphite;
            public Material SafetyOrange;
            public Material Ceramic;
            public Material Steel;
            public Material BladeSteel;
            public Material EnergyCyan;
            public Material StatusGreen;
            public Material EnemyPurple;
            public Material ThreatOrange;
            public Material FurnaceOrange;
        }
    }
}
