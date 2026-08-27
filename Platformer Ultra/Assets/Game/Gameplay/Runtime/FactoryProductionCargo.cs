using System;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryProductionCargo : MonoBehaviour
    {
        [SerializeField] private FactoryCargoKind _kind;
        [SerializeField] private ConveyorBelt[] _route = Array.Empty<ConveyorBelt>();
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.24f;

        private int _segmentIndex;
        private float _normalizedPosition;
        private bool _isMoving;

        public FactoryCargoKind Kind => _kind;
        public bool IsMoving => _isMoving;

        public event Action<FactoryProductionCargo, FactoryCargoKind> Arrived;

        private void Update()
        {
            if (!_isMoving || _route == null || _segmentIndex >= _route.Length)
            {
                return;
            }

            ConveyorBelt conveyor = _route[_segmentIndex];
            if (conveyor == null)
            {
                AdvanceSegment();
                return;
            }

            float speed = Mathf.Max(0f, conveyor.SignedSpeed);
            if (speed <= 0f || conveyor.SpanLength <= 0.001f)
            {
                return;
            }

            _normalizedPosition += speed / conveyor.SpanLength * Time.deltaTime;
            if (_normalizedPosition >= 1f)
            {
                AdvanceSegment();
                return;
            }

            ApplyPosition();
        }

        public void Configure(
            FactoryCargoKind kind,
            ConveyorBelt[] route,
            float surfaceOffset = 0.24f)
        {
            _kind = kind;
            _route = route ?? Array.Empty<ConveyorBelt>();
            _surfaceOffset = Mathf.Max(0f, surfaceOffset);
            _segmentIndex = 0;
            _normalizedPosition = 0f;
            _isMoving = false;
            ApplyPosition();
        }

        public void Begin()
        {
            _isMoving = _route != null && _route.Length > 0;
            if (!_isMoving)
            {
                Complete();
                return;
            }

            ApplyPosition();
        }

        private void AdvanceSegment()
        {
            _segmentIndex++;
            _normalizedPosition = 0f;
            if (_segmentIndex >= _route.Length)
            {
                Complete();
                return;
            }

            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (_route == null || _segmentIndex < 0 || _segmentIndex >= _route.Length)
            {
                return;
            }

            ConveyorBelt conveyor = _route[_segmentIndex];
            if (conveyor != null)
            {
                transform.position = conveyor.GetPathPosition(_normalizedPosition, _surfaceOffset);
            }
        }

        private void Complete()
        {
            if (!_isMoving && (_route == null || _route.Length > 0))
            {
                return;
            }

            _isMoving = false;
            Arrived?.Invoke(this, _kind);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }
}
