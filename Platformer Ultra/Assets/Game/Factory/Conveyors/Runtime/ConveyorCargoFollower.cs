using UnityEngine;
using UnityEngine.Events;

namespace PlatformerUltra.Factory.Conveyors
{
    [DisallowMultipleComponent]
    public sealed class ConveyorCargoFollower : MonoBehaviour
    {
        [SerializeField] private ConveyorBelt _conveyor;
        [SerializeField, Range(0f, 1f)] private float _normalizedPosition;
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.15f;
        [SerializeField] private bool _followOnEnable = true;
        [SerializeField] private bool _loop;
        [SerializeField] private UnityEvent _reachedEnd = new UnityEvent();

        private bool _isFollowing;

        public ConveyorBelt Conveyor => _conveyor;
        public float NormalizedPosition => _normalizedPosition;
        public bool IsFollowing => _isFollowing;

        private void OnEnable()
        {
            _isFollowing = _followOnEnable && _conveyor != null;
            ApplyPosition();
        }

        private void Update()
        {
            if (!_isFollowing || _conveyor == null || _conveyor.SpanLength <= 0.001f)
            {
                return;
            }

            float signedSpeed = _conveyor.SignedSpeed;
            if (Mathf.Approximately(signedSpeed, 0f))
            {
                return;
            }

            float previousPosition = _normalizedPosition;
            _normalizedPosition += (signedSpeed / _conveyor.SpanLength) * Time.deltaTime;

            bool reachedEnd = signedSpeed > 0f
                ? previousPosition < 1f && _normalizedPosition >= 1f
                : previousPosition > 0f && _normalizedPosition <= 0f;
            if (reachedEnd)
            {
                if (_loop)
                {
                    _normalizedPosition = Mathf.Repeat(_normalizedPosition, 1f);
                }
                else
                {
                    _normalizedPosition = Mathf.Clamp01(_normalizedPosition);
                    _isFollowing = false;
                }

                _reachedEnd.Invoke();
            }

            ApplyPosition();
        }

        public void Configure(ConveyorBelt conveyor, float normalizedPosition = 0f, float surfaceOffset = 0.15f)
        {
            _conveyor = conveyor;
            _normalizedPosition = Mathf.Clamp01(normalizedPosition);
            _surfaceOffset = Mathf.Max(0f, surfaceOffset);
            ApplyPosition();
        }

        public void BeginFollowing(ConveyorBelt conveyor, bool startAtConveyorInput = true)
        {
            _conveyor = conveyor;
            if (_conveyor != null && startAtConveyorInput)
            {
                _normalizedPosition = _conveyor.SignedSpeed < 0f ? 1f : 0f;
            }

            _isFollowing = _conveyor != null;
            ApplyPosition();
        }

        public void StopFollowing()
        {
            _isFollowing = false;
        }

        public void SetNormalizedPosition(float normalizedPosition)
        {
            _normalizedPosition = Mathf.Clamp01(normalizedPosition);
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (_conveyor != null)
            {
                transform.position = _conveyor.GetPathPosition(_normalizedPosition, _surfaceOffset);
            }
        }
    }
}
