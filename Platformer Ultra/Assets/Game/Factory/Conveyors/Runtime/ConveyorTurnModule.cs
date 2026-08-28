using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ConveyorTurnModule : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Turn";
        private const int SegmentCount = 8;

        [SerializeField] private Vector3 _incomingPoint = Vector3.back;
        [SerializeField] private Vector3 _cornerPoint;
        [SerializeField] private Vector3 _outgoingPoint = Vector3.right;
        [SerializeField, Min(0.35f)] private float _radius = 0.78f;
        [SerializeField, Min(0.5f)] private float _width = 2f;
        [SerializeField, Min(0.05f)] private float _deckThickness = 0.18f;
        [SerializeField, Min(0.02f)] private float _beltThickness = 0.08f;
        [SerializeField] private Material _beltMaterial;
        [SerializeField] private Material _frameMaterial;
        [SerializeField] private Material _accentMaterial;
        [SerializeField, HideInInspector] private Transform _generatedRoot;

        public Vector3 IncomingPoint => _incomingPoint;
        public Vector3 CornerPoint => _cornerPoint;
        public Vector3 OutgoingPoint => _outgoingPoint;
        public float Radius => _radius;

        private void OnEnable()
        {
            if (Application.isPlaying && _generatedRoot == null && HasValidTurn())
            {
                RebuildNow();
            }
        }

        private void OnValidate()
        {
            ClampValues();
        }

        public void Configure(Vector3 incomingPoint, Vector3 cornerPoint, Vector3 outgoingPoint)
        {
            _incomingPoint = incomingPoint;
            _cornerPoint = cornerPoint;
            _outgoingPoint = outgoingPoint;
            RebuildNow();
        }

        public void Configure(
            Vector3 incomingPoint,
            Vector3 cornerPoint,
            Vector3 outgoingPoint,
            Material beltMaterial,
            Material frameMaterial,
            Material accentMaterial,
            float width = 2f,
            float radius = 0.78f)
        {
            _beltMaterial = beltMaterial;
            _frameMaterial = frameMaterial;
            _accentMaterial = accentMaterial;
            _width = width;
            _radius = radius;
            Configure(incomingPoint, cornerPoint, outgoingPoint);
        }

        public void RebuildNow()
        {
            ClampValues();
            if (!HasValidTurn())
            {
                ClearGeneratedGeometry();
                return;
            }

            transform.position = _cornerPoint;
            EnsureGeneratedRoot();
            ClearChildren(_generatedRoot);

            Vector3 incomingDirection = (_cornerPoint - _incomingPoint).normalized;
            Vector3 outgoingDirection = (_outgoingPoint - _cornerPoint).normalized;
            float incomingRadius = Mathf.Min(_radius, Vector3.Distance(_incomingPoint, _cornerPoint) * 0.45f);
            float outgoingRadius = Mathf.Min(_radius, Vector3.Distance(_cornerPoint, _outgoingPoint) * 0.45f);
            Vector3 start = -incomingDirection * incomingRadius;
            Vector3 end = outgoingDirection * outgoingRadius;
            Vector3 previous = start;

            for (int index = 1; index <= SegmentCount; index++)
            {
                float t = index / (float)SegmentCount;
                Vector3 current = QuadraticBezier(start, Vector3.zero, end, t);
                CreateTurnSection(index, previous, current);
                previous = current;
            }
        }

        private void CreateTurnSection(int index, Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.001f)
            {
                return;
            }

            GameObject sectionObject = new GameObject($"Turn Section {index:00}");
            Transform section = sectionObject.transform;
            section.SetParent(_generatedRoot, false);
            section.localPosition = (start + end) * 0.5f;
            section.localRotation = BuildSpanRotation(direction / length);

            float overlappingLength = length + 0.12f;
            float innerWidth = Mathf.Max(0.2f, _width - 0.24f);
            CreateCube(
                "Structural Deck",
                section,
                new Vector3(0f, -_deckThickness * 0.5f, 0f),
                new Vector3(_width, _deckThickness, overlappingLength),
                _frameMaterial,
                true);
            CreateCube(
                "Curved Belt Surface",
                section,
                new Vector3(0f, _beltThickness * 0.5f, 0f),
                new Vector3(innerWidth, _beltThickness, overlappingLength),
                _beltMaterial,
                false);
            CreateCube(
                "Turn Slat",
                section,
                new Vector3(0f, _beltThickness + 0.012f, 0f),
                new Vector3(innerWidth * 0.94f, 0.025f, Mathf.Min(0.1f, overlappingLength * 0.45f)),
                _accentMaterial,
                false);

            float railX = (_width * 0.5f) - 0.06f;
            CreateCube(
                "Left Curved Rail",
                section,
                new Vector3(-railX, 0.14f, 0f),
                new Vector3(0.12f, 0.28f, overlappingLength),
                _frameMaterial,
                false);
            CreateCube(
                "Right Curved Rail",
                section,
                new Vector3(railX, 0.14f, 0f),
                new Vector3(0.12f, 0.28f, overlappingLength),
                _frameMaterial,
                false);
        }

        private bool HasValidTurn()
        {
            Vector3 incoming = _cornerPoint - _incomingPoint;
            Vector3 outgoing = _outgoingPoint - _cornerPoint;
            return incoming.sqrMagnitude > 0.01f && outgoing.sqrMagnitude > 0.01f &&
                   Vector3.Dot(incoming.normalized, outgoing.normalized) < 0.999f;
        }

        private void ClampValues()
        {
            _radius = Mathf.Max(0.35f, _radius);
            _width = Mathf.Max(0.5f, _width);
            _deckThickness = Mathf.Max(0.05f, _deckThickness);
            _beltThickness = Mathf.Max(0.02f, _beltThickness);
        }

        private void EnsureGeneratedRoot()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            Transform existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                _generatedRoot = existing;
                return;
            }

            GameObject generatedObject = new GameObject(GeneratedRootName);
            _generatedRoot = generatedObject.transform;
            _generatedRoot.SetParent(transform, false);
        }

        private void ClearGeneratedGeometry()
        {
            if (_generatedRoot != null)
            {
                ClearChildren(_generatedRoot);
            }
        }

        private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private static Quaternion BuildSpanRotation(Vector3 direction)
        {
            Vector3 stableUp = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f
                ? Vector3.forward
                : Vector3.up;
            Vector3 right = Vector3.Cross(stableUp, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            return Quaternion.LookRotation(direction, up);
        }

        private static GameObject CreateCube(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                DestroySafely(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = parent.GetChild(index).gameObject;
                DestroySafely(child);
            }
        }

        private static void DestroySafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
