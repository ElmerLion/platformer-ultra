using System;
using PlatformerUltra.Combat;
using PlatformerUltra.FactoryDefense;
using PlatformerUltra.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PlatformerUltra.Factory.Editor
{
    public static class FactoryDefenseAssetFactory
    {
        public const string TurretPrefabPath = "Assets/Game/FactoryDefense/Prefabs/PF_Factory_Turret.prefab";
        public const string BuildSpotPrefabPath = "Assets/Game/FactoryDefense/Prefabs/PF_Factory_TurretBuildSpot.prefab";

        private const string SyntyTurretPath =
            "Assets/Synty/PolygonSciFiSpace/Prefabs/Props/SM_Prop_Turret_Base_Single_01.prefab";
        private const string FrameMaterialPath = "Assets/Game/Factory/Conveyors/Materials/M_Conveyor_Frame.mat";
        private const string DarkMaterialPath = "Assets/Game/Factory/Conveyors/Materials/M_Conveyor_Belt.mat";
        private const string HazardMaterialPath = "Assets/Game/Factory/Materials/M_Factory_MapHazard.mat";
        private const string MuzzleMaterialPath = "Assets/Game/Factory/Materials/M_Factory_EmissiveOrange.mat";

        public static void BuildAll()
        {
            EnsureFolder("Assets/Game/FactoryDefense");
            EnsureFolder("Assets/Game/FactoryDefense/Prefabs");
            BuildTurretPrefab();
            BuildSpotPrefab();
        }

        private static void BuildTurretPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyTurretPath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing selected Synty turret at " + SyntyTurretPath + ".");
            }

            GameObject root = new GameObject("PF_Factory_Turret");
            try
            {
                Health health = root.AddComponent<Health>();
                FactionMember factionMember = root.AddComponent<FactionMember>();
                Targetable targetable = root.AddComponent<Targetable>();
                FactoryTurret turret = root.AddComponent<FactoryTurret>();

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                visual.name = "Synty Single Gatling Turret Visual";
                visual.transform.SetParent(root.transform, false);

                Transform yawPivot = FindChild(visual.transform, "SM_Prop_Turret_Gattling_Base_01");
                Transform barrel = FindChild(visual.transform, "SM_Prop_Turret_Gattling_Barrel_01");
                if (yawPivot == null || barrel == null)
                {
                    throw new InvalidOperationException("The selected Synty turret hierarchy no longer exposes its Gatling pivots.");
                }

                GameObject muzzleObject = new GameObject("Muzzle");
                muzzleObject.transform.SetParent(barrel, false);
                muzzleObject.transform.localPosition = new Vector3(0f, 0f, 1.55f);

                GameObject targetPointObject = new GameObject("Target Point");
                targetPointObject.transform.SetParent(root.transform, false);
                targetPointObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                TargetPoint targetPoint = targetPointObject.AddComponent<TargetPoint>();

                GameObject muzzleFlash = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Muzzle Flash",
                    muzzleObject.transform,
                    Vector3.zero,
                    new Vector3(0.22f, 0.22f, 0.36f),
                    LoadMaterial(MuzzleMaterialPath));
                muzzleFlash.SetActive(false);

                targetable.Configure(factionMember, targetPoint, turret, true);
                turret.Configure(
                    health,
                    factionMember,
                    targetable,
                    targetPoint,
                    yawPivot,
                    muzzleObject.transform,
                    muzzleFlash,
                    ~0,
                    80,
                    12f,
                    10,
                    1.2f,
                    90f,
                    5f,
                    0.2f);
                PrefabUtility.SaveAsPrefabAsset(root, TurretPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildSpotPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SyntyTurretPath);
            GameObject turretPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
            if (source == null || turretPrefabObject == null)
            {
                throw new InvalidOperationException("Build the Synty turret wrapper before its construction spot.");
            }

            GameObject root = new GameObject("PF_Factory_TurretBuildSpot");
            try
            {
                GameObject mount = new GameObject("Operational Turret Mount");
                mount.transform.SetParent(root.transform, false);

                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Industrial Mounting Plate",
                    root.transform,
                    new Vector3(0f, 0.1f, 0f),
                    new Vector3(1.35f, 0.1f, 1.35f),
                    LoadMaterial(DarkMaterialPath));
                CreateHazardPerimeter(root.transform);

                GameObject damaged = new GameObject("Damaged Turret Installation");
                damaged.transform.SetParent(root.transform, false);
                GameObject damagedVisual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                damagedVisual.name = "Broken Synty Single Gatling Turret";
                damagedVisual.transform.SetParent(damaged.transform, false);
                foreach (Collider collider in damagedVisual.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }

                Transform housing = FindChild(damagedVisual.transform, "SM_Prop_Turret_Gattling_Base_01");
                Transform barrel = FindChild(damagedVisual.transform, "SM_Prop_Turret_Gattling_Barrel_01");
                if (housing != null)
                {
                    housing.localRotation = Quaternion.Euler(12f, 28f, 16f);
                }

                if (barrel != null)
                {
                    barrel.localPosition = new Vector3(1.05f, 0.32f, 0.6f);
                    barrel.localRotation = Quaternion.Euler(68f, 22f, 35f);
                }

                BoxCollider trigger = root.AddComponent<BoxCollider>();
                trigger.center = new Vector3(0f, 1.15f, 0f);
                trigger.size = new Vector3(3.4f, 2.8f, 3.4f);
                trigger.isTrigger = true;

                FactoryTurret turretPrefab = turretPrefabObject.GetComponent<FactoryTurret>();
                TurretBuildSpot spot = root.AddComponent<TurretBuildSpot>();
                spot.Configure(
                    turretPrefab,
                    mount.transform,
                    damaged,
                    trigger,
                    null,
                    null,
                    12f);
                root.AddComponent<InteractionTarget>().Configure(spot);
                PrefabUtility.SaveAsPrefabAsset(root, BuildSpotPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateHazardPerimeter(Transform parent)
        {
            Material frame = LoadMaterial(FrameMaterialPath);
            Material hazard = LoadMaterial(HazardMaterialPath);
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Hazard Rail X " + side,
                    parent,
                    new Vector3(0f, 0.17f, side * 1.35f),
                    new Vector3(3f, 0.12f, 0.16f),
                    side > 0 ? hazard : frame);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Hazard Rail Z " + side,
                    parent,
                    new Vector3(side * 1.35f, 0.17f, 0f),
                    new Vector3(0.16f, 0.12f, 3f),
                    side > 0 ? frame : hazard);
            }
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == name)
                {
                    return children[index];
                }
            }

            return null;
        }

        private static Material LoadMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException("Missing factory material at " + path + ".");
            }

            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            string name = path.Substring(split + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
