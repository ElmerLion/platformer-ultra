using System.Collections.Generic;
using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ConveyorBelt : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Conveyor";
        private const string AssemblyName = "Conveyor Assembly";
        private const string SlatPrefix = "Moving Slat ";
        private const float MinimumSpanLength = 0.5f;

        [Header("Endpoints")]
        [SerializeField] private ConveyorEndpoint _startEndpoint;
        [SerializeField] private ConveyorEndpoint _endEndpoint;
        [SerializeField] private bool _autoRebuild = true;

        [Header("Operation")]
        [SerializeField] private ConveyorOperatingState _operatingState = ConveyorOperatingState.Online;
        [SerializeField, Min(0f)] private float _speed = 2.5f;
        [SerializeField] private bool _reverseDirection;

        [Header("Dimensions")]
        [SerializeField, Min(0.5f)] private float _width = 2f;
        [SerializeField, Min(0.05f)] private float _deckThickness = 0.18f;
        [SerializeField, Min(0.02f)] private float _beltThickness = 0.08f;
        [SerializeField, Min(0.02f)] private float _sideRailWidth = 0.12f;
        [SerializeField, Min(0f)] private float _sideRailHeight = 0.28f;
        [SerializeField, Min(0.15f)] private float _slatSpacing = 0.5f;
        [SerializeField, Min(0.1f)] private float _surfaceZoneHeight = 0.75f;

        [Header("Visuals")]
        [SerializeField] private Material _beltMaterial;
        [SerializeField] private Material _frameMaterial;
        [SerializeField] private Material _accentMaterial;

        [SerializeField, HideInInspector] private Transform _generatedRoot;

        private readonly List<Transform> _movingSlats = new List<Transform>();
        private Vector3 _direction = Vector3.forward;
        private Vector3 _surfaceUp = Vector3.up;
        private float _spanLength = MinimumSpanLength;
        private Vector3 _lastStartPosition = new Vector3(float.PositiveInfinity, 0f, 0f);
        private Vector3 _lastEndPosition = new Vector3(float.PositiveInfinity, 0f, 0f);
        private Vector3 _lastRootPosition = new Vector3(float.PositiveInfinity, 0f, 0f);
        private Quaternion _lastRootRotation = Quaternion.identity;
        private Vector3 _lastRootScale = new Vector3(float.PositiveInfinity, 0f, 0f);

        public ConveyorEndpoint StartEndpoint => _startEndpoint;
        public ConveyorEndpoint EndEndpoint => _endEndpoint;
        public ConveyorOperatingState OperatingState => _operatingState;
        public float Speed => _speed;
        public float Width => _width;
        public float SpanLength => _spanLength;
        public Vector3 Direction => _direction;
        public Vector3 SurfaceUp => _surfaceUp;
        public Vector3 StartPosition => _startEndpoint != null ? _startEndpoint.transform.position : transform.position;
        public Vector3 EndPosition => _endEndpoint != null ? _endEndpoint.transform.position : transform.position;
        public bool HasValidEndpoints => _startEndpoint != null && _endEndpoint != null && _startEndpoint != _endEndpoint;
        public bool IsMoving => _operatingState == ConveyorOperatingState.Online && _speed > 0f;
        public float SignedSpeed => IsMoving ? (_reverseDirection ? -_speed : _speed) : 0f;
        public Vector3 SurfaceVelocity => _direction * SignedSpeed;

        private void OnEnable()
        {
            ClampValues();
            RefreshDerivedValues();
            CacheMovingSlats();

            if (Application.isPlaying && _generatedRoot == null && HasValidEndpoints)
            {
                RebuildNow();
            }
        }

        private void OnValidate()
        {
            ClampValues();
            RefreshDerivedValues();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
            {
                AnimateSlats(Time.deltaTime);
                return;
            }

            if (!_autoRebuild || !HasValidEndpoints)
            {
                return;
            }

            Vector3 startPosition = StartPosition;
            Vector3 endPosition = EndPosition;
            bool rootChanged = (transform.position - _lastRootPosition).sqrMagnitude > 0.000001f ||
                               Quaternion.Angle(transform.rotation, _lastRootRotation) > 0.001f ||
                               (transform.lossyScale - _lastRootScale).sqrMagnitude > 0.000001f;
            if ((startPosition - _lastStartPosition).sqrMagnitude > 0.000001f ||
                (endPosition - _lastEndPosition).sqrMagnitude > 0.000001f ||
                rootChanged)
            {
                RebuildNow();
            }
        }

        public void Configure(
            ConveyorEndpoint startEndpoint,
            ConveyorEndpoint endEndpoint,
            Material beltMaterial,
            Material frameMaterial,
            Material accentMaterial)
        {
            _startEndpoint = startEndpoint;
            _endEndpoint = endEndpoint;
            _beltMaterial = beltMaterial;
            _frameMaterial = frameMaterial;
            _accentMaterial = accentMaterial;
            RebuildNow();
        }

        public void SetEndpoints(ConveyorEndpoint startEndpoint, ConveyorEndpoint endEndpoint)
        {
            _startEndpoint = startEndpoint;
            _endEndpoint = endEndpoint;
            RebuildNow();
        }

        public void SetOperatingState(ConveyorOperatingState operatingState)
        {
            _operatingState = operatingState;
        }

        public void SetSpeed(float speed)
        {
            _speed = Mathf.Max(0f, speed);
        }

        public void SetReversed(bool reversed)
        {
            _reverseDirection = reversed;
        }

        public Vector3 GetPathPosition(float normalizedPosition, float surfaceOffset = 0f)
        {
            float clampedPosition = Mathf.Clamp01(normalizedPosition);
            return Vector3.Lerp(StartPosition, EndPosition, clampedPosition) + _surfaceUp * surfaceOffset;
        }

        public void RebuildNow()
        {
            ClampValues();
            RefreshDerivedValues();

            if (!HasValidEndpoints || _spanLength < MinimumSpanLength)
            {
                ClearGeneratedGeometry();
                return;
            }

            EnsureGeneratedRoot();
            ClearChildren(_generatedRoot);

            GameObject assemblyObject = new GameObject(AssemblyName);
            Transform assembly = assemblyObject.transform;
            assembly.SetParent(_generatedRoot, false);
            assembly.position = Vector3.Lerp(StartPosition, EndPosition, 0.5f);
            assembly.rotation = BuildSpanRotation(_direction);
            assembly.localScale = Vector3.one;

            float innerWidth = Mathf.Max(0.2f, _width - (_sideRailWidth * 2f));
            float visualLength = Mathf.Max(0.2f, _spanLength - 0.08f);
            float deckCenterY = -(_deckThickness * 0.5f);
            float beltCenterY = _beltThickness * 0.5f;

            CreateCube(
                "Structural Deck",
                assembly,
                new Vector3(0f, deckCenterY, 0f),
                new Vector3(_width, _deckThickness, visualLength),
                _frameMaterial,
                true);

            CreateCube(
                "Belt Surface",
                assembly,
                new Vector3(0f, beltCenterY, 0f),
                new Vector3(innerWidth, _beltThickness, visualLength),
                _beltMaterial,
                false);

            float railX = (_width * 0.5f) - (_sideRailWidth * 0.5f);
            float railY = _sideRailHeight * 0.5f;
            CreateCube(
                "Left Safety Rail",
                assembly,
                new Vector3(-railX, railY, 0f),
                new Vector3(_sideRailWidth, _sideRailHeight, visualLength),
                _frameMaterial,
                false);
            CreateCube(
                "Right Safety Rail",
                assembly,
                new Vector3(railX, railY, 0f),
                new Vector3(_sideRailWidth, _sideRailHeight, visualLength),
                _frameMaterial,
                false);

            CreateRoller("Start Roller", assembly, -visualLength * 0.5f, innerWidth);
            CreateRoller("End Roller", assembly, visualLength * 0.5f, innerWidth);
            CreateMovingSlats(assembly, innerWidth, visualLength);
            CreateSurfaceZone(assembly, innerWidth, visualLength);

            _lastStartPosition = StartPosition;
            _lastEndPosition = EndPosition;
            _lastRootPosition = transform.position;
            _lastRootRotation = transform.rotation;
            _lastRootScale = transform.lossyScale;
            CacheMovingSlats();
        }

        private void ClampValues()
        {
            _speed = Mathf.Max(0f, _speed);
            _width = Mathf.Max(0.5f, _width);
            _deckThickness = Mathf.Max(0.05f, _deckThickness);
            _beltThickness = Mathf.Max(0.02f, _beltThickness);
            _sideRailWidth = Mathf.Clamp(_sideRailWidth, 0.02f, _width * 0.4f);
            _sideRailHeight = Mathf.Max(0f, _sideRailHeight);
            _slatSpacing = Mathf.Max(0.15f, _slatSpacing);
            _surfaceZoneHeight = Mathf.Max(0.1f, _surfaceZoneHeight);
        }

        private void RefreshDerivedValues()
        {
            if (!HasValidEndpoints)
            {
                _spanLength = MinimumSpanLength;
                _direction = transform.forward;
                _surfaceUp = transform.up;
                return;
            }

            Vector3 span = EndPosition - StartPosition;
            _spanLength = span.magnitude;
            if (_spanLength <= 0.0001f)
            {
                _direction = transform.forward;
                _surfaceUp = transform.up;
                return;
            }

            _direction = span / _spanLength;
            Quaternion rotation = BuildSpanRotation(_direction);
            _surfaceUp = rotation * Vector3.up;
        }

        private static Quaternion BuildSpanRotation(Vector3 direction)
        {
            Vector3 stableUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(direction, stableUp)) > 0.98f)
            {
                stableUp = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(stableUp, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            return Quaternion.LookRotation(direction, up);
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

            _movingSlats.Clear();
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = parent.GetChild(index).gameObject;
                child.transform.SetParent(null, false);
                DestroyObject(child);
            }
        }

        private static void DestroyObject(Object target)
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
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;

            MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (!keepCollider)
            {
                DestroyObject(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private void CreateRoller(string objectName, Transform parent, float localZ, float innerWidth)
        {
            GameObject roller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roller.name = objectName;
            roller.transform.SetParent(parent, false);
            roller.transform.localPosition = new Vector3(0f, _beltThickness, localZ);
            roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            float radius = Mathf.Max(0.08f, _beltThickness * 1.5f);
            roller.transform.localScale = new Vector3(radius, innerWidth * 0.5f, radius);
            roller.GetComponent<MeshRenderer>().sharedMaterial = _accentMaterial;
            DestroyObject(roller.GetComponent<Collider>());
        }

        private void CreateMovingSlats(Transform parent, float innerWidth, float visualLength)
        {
            int slatCount = Mathf.Max(2, Mathf.CeilToInt(visualLength / _slatSpacing));
            float actualSpacing = visualLength / slatCount;
            float slatY = _beltThickness + 0.012f;

            for (int index = 0; index < slatCount; index++)
            {
                float z = (-visualLength * 0.5f) + (actualSpacing * (index + 0.5f));
                GameObject slat = CreateCube(
                    $"{SlatPrefix}{index:000}",
                    parent,
                    new Vector3(0f, slatY, z),
                    new Vector3(innerWidth * 0.95f, 0.025f, Mathf.Min(0.08f, actualSpacing * 0.3f)),
                    _accentMaterial,
                    false);
                _movingSlats.Add(slat.transform);
            }
        }

        private void CreateSurfaceZone(Transform parent, float innerWidth, float visualLength)
        {
            GameObject zoneObject = new GameObject("Conveyor Surface Zone");
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.localPosition = new Vector3(0f, _surfaceZoneHeight * 0.5f, 0f);
            zoneObject.transform.localRotation = Quaternion.identity;
            zoneObject.transform.localScale = Vector3.one;

            BoxCollider zoneCollider = zoneObject.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(innerWidth, _surfaceZoneHeight, visualLength);

            ConveyorSurfaceZone zone = zoneObject.AddComponent<ConveyorSurfaceZone>();
            zone.Configure(this);
        }

        private void CacheMovingSlats()
        {
            _movingSlats.Clear();
            if (_generatedRoot == null)
            {
                return;
            }

            Transform assembly = _generatedRoot.Find(AssemblyName);
            if (assembly == null)
            {
                return;
            }

            for (int index = 0; index < assembly.childCount; index++)
            {
                Transform child = assembly.GetChild(index);
                if (child.name.StartsWith(SlatPrefix, System.StringComparison.Ordinal))
                {
                    _movingSlats.Add(child);
                }
            }
        }

        private void AnimateSlats(float deltaTime)
        {
            if (!IsMoving || _movingSlats.Count == 0 || _spanLength <= 0.001f)
            {
                return;
            }

            float halfLength = Mathf.Max(0.1f, (_spanLength - 0.08f) * 0.5f);
            float range = halfLength * 2f;
            float delta = SignedSpeed * deltaTime;

            for (int index = 0; index < _movingSlats.Count; index++)
            {
                Transform slat = _movingSlats[index];
                if (slat == null)
                {
                    continue;
                }

                Vector3 position = slat.localPosition;
                position.z = Mathf.Repeat(position.z + halfLength + delta, range) - halfLength;
                slat.localPosition = position;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasValidEndpoints)
            {
                return;
            }

            Gizmos.color = IsMoving ? new Color(0.2f, 1f, 0.35f, 0.9f) : new Color(1f, 0.55f, 0.15f, 0.9f);
            Gizmos.DrawLine(StartPosition, EndPosition);
            Vector3 midpoint = Vector3.Lerp(StartPosition, EndPosition, 0.5f) + _surfaceUp * 0.35f;
            Vector3 arrowDirection = _reverseDirection ? -_direction : _direction;
            Gizmos.DrawRay(midpoint, arrowDirection * Mathf.Min(1.5f, _spanLength * 0.25f));
        }
    }
}
