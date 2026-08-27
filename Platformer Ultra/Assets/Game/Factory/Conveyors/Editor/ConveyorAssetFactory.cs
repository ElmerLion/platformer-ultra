using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors.Editor
{
    public static class ConveyorAssetFactory
    {
        private const string RootFolder = "Assets/Game/Factory/Conveyors";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string BeltMaterialPath = MaterialFolder + "/M_Conveyor_Belt.mat";
        private const string FrameMaterialPath = MaterialFolder + "/M_Conveyor_Frame.mat";
        private const string AccentMaterialPath = MaterialFolder + "/M_Conveyor_Accent.mat";
        private const string ConveyorPrefabPath = PrefabFolder + "/PF_Conveyor_PointToPoint.prefab";
        private const string EndpointPrefabPath = PrefabFolder + "/PF_Conveyor_Endpoint.prefab";

        [InitializeOnLoadMethod]
        private static void QueueFirstBuild()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/Factory/Conveyors/Build or Refresh Conveyor Assets")]
        public static void BuildOrRefreshAssets()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            Material beltMaterial = CreateOrUpdateMaterial(
                BeltMaterialPath,
                new Color(0.045f, 0.055f, 0.06f),
                0.05f,
                0.26f,
                Color.black);
            Material frameMaterial = CreateOrUpdateMaterial(
                FrameMaterialPath,
                new Color(0.95f, 0.38f, 0.045f),
                0.55f,
                0.42f,
                Color.black);
            Material accentMaterial = CreateOrUpdateMaterial(
                AccentMaterialPath,
                new Color(0.46f, 0.56f, 0.62f),
                0.72f,
                0.68f,
                new Color(0.03f, 0.09f, 0.12f));

            BuildConveyorPrefab(beltMaterial, frameMaterial, accentMaterial);
            BuildEndpointPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Conveyor assets built at {PrefabFolder}.");
        }

        [MenuItem("GameObject/Factory/Conveyors/Point-to-Point Conveyor", false, 10)]
        private static void CreateConveyor(MenuCommand menuCommand)
        {
            BuildIfMissing();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load conveyor prefab at {ConveyorPrefabPath}.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Point-to-Point Conveyor");
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);
            instance.transform.position = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        [MenuItem("GameObject/Factory/Conveyors/Connect Selected Endpoints", false, 11)]
        private static void ConnectSelectedEndpoints()
        {
            ConveyorEndpoint[] endpoints = Selection.GetFiltered<ConveyorEndpoint>(SelectionMode.Editable);
            if (endpoints.Length != 2)
            {
                Debug.LogWarning("Select exactly two ConveyorEndpoint components. The active selection becomes the destination.");
                return;
            }

            ConveyorEndpoint end = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<ConveyorEndpoint>()
                : null;
            if (end == null)
            {
                end = endpoints[1];
            }

            ConveyorEndpoint start = endpoints[0] == end ? endpoints[1] : endpoints[0];
            BuildIfMissing();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Connect Conveyor Endpoints");
            instance.name = $"Conveyor - {start.name} to {end.name}";

            ConveyorBelt belt = instance.GetComponent<ConveyorBelt>();
            ConveyorEndpoint[] localEndpoints = instance.GetComponentsInChildren<ConveyorEndpoint>(true);
            Undo.RecordObject(belt, "Assign Conveyor Endpoints");
            belt.SetEndpoints(start, end);
            PrefabUtility.RecordPrefabInstancePropertyModifications(belt);

            foreach (ConveyorEndpoint localEndpoint in localEndpoints)
            {
                Undo.DestroyObjectImmediate(localEndpoint.gameObject);
            }

            belt.RebuildNow();
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        [MenuItem("GameObject/Factory/Conveyors/Connect Selected Endpoints", true)]
        private static bool ValidateConnectSelectedEndpoints()
        {
            return Selection.GetFiltered<ConveyorEndpoint>(SelectionMode.Editable).Length == 2;
        }

        private static void BuildIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(EndpointPrefabPath) == null)
            {
                BuildOrRefreshAssets();
            }
        }

        private static Material CreateOrUpdateMaterial(
            string path,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
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

        private static void BuildConveyorPrefab(Material belt, Material frame, Material accent)
        {
            GameObject root = new GameObject("PF_Conveyor_PointToPoint");
            try
            {
                ConveyorEndpoint start = CreateEndpoint(root.transform, "Start Endpoint", new Vector3(0f, 0f, -3f), ConveyorEndpointKind.Output);
                ConveyorEndpoint end = CreateEndpoint(root.transform, "End Endpoint", new Vector3(0f, 0f, 3f), ConveyorEndpointKind.Input);
                ConveyorBelt conveyor = root.AddComponent<ConveyorBelt>();
                conveyor.Configure(start, end, belt, frame, accent);
                PrefabUtility.SaveAsPrefabAsset(root, ConveyorPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildEndpointPrefab()
        {
            GameObject root = new GameObject("PF_Conveyor_Endpoint");
            try
            {
                root.AddComponent<ConveyorEndpoint>().Configure(ConveyorEndpointKind.Bidirectional);
                PrefabUtility.SaveAsPrefabAsset(root, EndpointPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static ConveyorEndpoint CreateEndpoint(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            ConveyorEndpointKind kind)
        {
            GameObject endpointObject = new GameObject(objectName);
            endpointObject.transform.SetParent(parent, false);
            endpointObject.transform.localPosition = localPosition;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind);
            return endpoint;
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
    }
}
