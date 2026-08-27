using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors.Editor
{
    [CustomEditor(typeof(ConveyorBelt))]
    public sealed class ConveyorBeltEditor : UnityEditor.Editor
    {
        private ConveyorBelt Belt => (ConveyorBelt)target;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                Rebuild(Belt);
            }

            DrawValidation(Belt);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Conveyor"))
                {
                    Rebuild(Belt);
                }

                if (GUILayout.Button("Swap Direction"))
                {
                    Undo.RecordObject(Belt, "Swap Conveyor Direction");
                    Belt.SetEndpoints(Belt.EndEndpoint, Belt.StartEndpoint);
                    MarkDirty(Belt);
                }
            }

            if (!Belt.HasValidEndpoints && GUILayout.Button("Create Local Endpoints"))
            {
                CreateLocalEndpoints(Belt);
            }
        }

        private void OnSceneGUI()
        {
            DrawEndpointHandle(Belt.StartEndpoint, "Move Conveyor Start");
            DrawEndpointHandle(Belt.EndEndpoint, "Move Conveyor End");
        }

        private void DrawEndpointHandle(ConveyorEndpoint endpoint, string undoName)
        {
            if (endpoint == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(endpoint.transform.position, endpoint.transform.rotation);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(endpoint.transform, undoName);
            endpoint.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(endpoint.transform);
            Rebuild(Belt);
        }

        private static void DrawValidation(ConveyorBelt belt)
        {
            if (belt.StartEndpoint == null || belt.EndEndpoint == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign both endpoints, or create local endpoints. The generated span is rebuilt from their world positions.",
                    MessageType.Info);
                return;
            }

            if (belt.StartEndpoint == belt.EndEndpoint)
            {
                EditorGUILayout.HelpBox("Start and end must be different endpoints.", MessageType.Error);
            }
            else if (!belt.StartEndpoint.CanFeed(belt.EndEndpoint))
            {
                EditorGUILayout.HelpBox(
                    "These endpoint types do not form Output -> Input flow. Bidirectional sockets work at either end.",
                    MessageType.Warning);
            }

            if (belt.SpanLength < 0.5f)
            {
                EditorGUILayout.HelpBox("Move the endpoints at least 0.5 units apart.", MessageType.Error);
            }

            if (belt.transform.lossyScale != Vector3.one)
            {
                EditorGUILayout.HelpBox(
                    "Keep the conveyor root scale at (1, 1, 1). Change Width in the component and move endpoints to resize it.",
                    MessageType.Warning);
            }

            float slope = Mathf.Abs(Mathf.Asin(Mathf.Clamp(belt.Direction.y, -1f, 1f))) * Mathf.Rad2Deg;
            if (slope > 50f)
            {
                EditorGUILayout.HelpBox(
                    $"This {slope:0.#} degree slope is allowed, but physics passengers may need extra acceleration.",
                    MessageType.Info);
            }
        }

        private static void CreateLocalEndpoints(ConveyorBelt belt)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Conveyor Endpoints");

            ConveyorEndpoint start = CreateEndpointChild(
                belt.transform,
                "Start Endpoint",
                new Vector3(0f, 0f, -3f),
                ConveyorEndpointKind.Output);
            ConveyorEndpoint end = CreateEndpointChild(
                belt.transform,
                "End Endpoint",
                new Vector3(0f, 0f, 3f),
                ConveyorEndpointKind.Input);

            Undo.RecordObject(belt, "Assign Conveyor Endpoints");
            belt.SetEndpoints(start, end);
            MarkDirty(belt);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static ConveyorEndpoint CreateEndpointChild(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            ConveyorEndpointKind kind)
        {
            GameObject endpointObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(endpointObject, $"Create {objectName}");
            endpointObject.transform.SetParent(parent, false);
            endpointObject.transform.localPosition = localPosition;
            ConveyorEndpoint endpoint = endpointObject.AddComponent<ConveyorEndpoint>();
            endpoint.Configure(kind);
            return endpoint;
        }

        private static void Rebuild(ConveyorBelt belt)
        {
            belt.RebuildNow();
            MarkDirty(belt);
            SceneView.RepaintAll();
        }

        private static void MarkDirty(ConveyorBelt belt)
        {
            EditorUtility.SetDirty(belt);
            PrefabUtility.RecordPrefabInstancePropertyModifications(belt);

            if (belt.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(belt.gameObject.scene);
            }
        }
    }
}
