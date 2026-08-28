using UnityEngine;

namespace PlatformerUltra.FactoryDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TurretLaserTracer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField, Min(0.02f)] private float _lifetime = 0.12f;
        [SerializeField, Min(0.001f)] private float _startWidth = 0.075f;
        [SerializeField, Min(0.001f)] private float _endWidth = 0.02f;

        private float _remainingLifetime;
        private bool _initialized;

        public float Lifetime => _lifetime;
        public Vector3 StartPosition { get; private set; }
        public Vector3 EndPosition { get; private set; }
        public bool IsInitialized => _initialized;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;
            float visibility = Mathf.Clamp01(_remainingLifetime / _lifetime);
            if (_lineRenderer != null)
            {
                _lineRenderer.startWidth = _startWidth * visibility;
                _lineRenderer.endWidth = _endWidth * visibility;
            }

            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(
            LineRenderer lineRenderer,
            float lifetime = 0.12f,
            float startWidth = 0.075f,
            float endWidth = 0.02f)
        {
            _lineRenderer = lineRenderer;
            _lifetime = Mathf.Max(0.02f, lifetime);
            _startWidth = Mathf.Max(0.001f, startWidth);
            _endWidth = Mathf.Max(0.001f, endWidth);
            ResolveReferences();
            ConfigureRenderer();
        }

        public void Initialize(Vector3 startPosition, Vector3 endPosition)
        {
            ResolveReferences();
            StartPosition = startPosition;
            EndPosition = endPosition;
            _remainingLifetime = _lifetime;
            _initialized = true;
            ConfigureRenderer();
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.startWidth = _startWidth;
            _lineRenderer.endWidth = _endWidth;
            _lineRenderer.SetPosition(0, startPosition);
            _lineRenderer.SetPosition(1, endPosition);
            _lineRenderer.enabled = true;
        }

        private void ConfigureRenderer()
        {
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = _startWidth;
            _lineRenderer.endWidth = _endWidth;
        }

        private void ResolveReferences()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }
        }
    }
}
