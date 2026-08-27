using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryGantryCraneMover : MonoBehaviour
    {
        [SerializeField] private Transform _movingRoot;
        [SerializeField] private FactoryObjectiveTerminal _generatorTerminal;
        [SerializeField] private Vector3 _worldPointA;
        [SerializeField] private Vector3 _worldPointB = Vector3.forward * 10f;
        [SerializeField, Min(0.1f)] private float _speed = 1.8f;
        [SerializeField, Min(0f)] private float _endpointPause = 0.75f;

        private bool _travellingToPointB = true;
        private float _pauseRemaining;

        public Vector3 PointA => _worldPointA;
        public Vector3 PointB => _worldPointB;
        public bool IsPowered => _generatorTerminal != null && _generatorTerminal.IsOperational;

        private void Awake()
        {
            if (_movingRoot == null)
            {
                _movingRoot = transform;
            }
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0.1f, _speed);
            _endpointPause = Mathf.Max(0f, _endpointPause);
        }

        private void Update()
        {
            AdvanceMovement(Time.deltaTime);
        }

        public void Configure(
            Transform movingRoot,
            FactoryObjectiveTerminal generatorTerminal,
            Vector3 worldPointA,
            Vector3 worldPointB,
            float speed = 1.8f,
            float endpointPause = 0.75f)
        {
            _movingRoot = movingRoot != null ? movingRoot : transform;
            _generatorTerminal = generatorTerminal;
            _worldPointA = worldPointA;
            _worldPointB = worldPointB;
            _speed = Mathf.Max(0.1f, speed);
            _endpointPause = Mathf.Max(0f, endpointPause);
            _travellingToPointB = true;
            _pauseRemaining = 0f;
        }

        public void AdvanceMovement(float deltaTime)
        {
            if (_movingRoot == null || !IsPowered)
            {
                return;
            }

            float remainingTime = Mathf.Max(0f, deltaTime);
            if (_pauseRemaining > 0f)
            {
                float consumedPause = Mathf.Min(_pauseRemaining, remainingTime);
                _pauseRemaining -= consumedPause;
                remainingTime -= consumedPause;
                if (remainingTime <= 0f)
                {
                    return;
                }
            }

            Vector3 target = _travellingToPointB ? _worldPointB : _worldPointA;
            _movingRoot.position = Vector3.MoveTowards(
                _movingRoot.position,
                target,
                _speed * remainingTime);

            if ((_movingRoot.position - target).sqrMagnitude > 0.000001f)
            {
                return;
            }

            _travellingToPointB = !_travellingToPointB;
            _pauseRemaining = _endpointPause;
        }
    }
}
