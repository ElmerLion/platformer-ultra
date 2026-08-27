using System.Collections.Generic;
using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors
{
    [DisallowMultipleComponent]
    public sealed class ConveyorPassenger : MonoBehaviour
    {
        [Header("Motor")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private bool _moveCharacterControllerAutomatically = true;
        [SerializeField] private bool _moveTransformWithoutMotor;
        [SerializeField, Min(0f)] private float _maximumAcceleration = 30f;

        private readonly HashSet<ConveyorSurfaceZone> _activeZones = new HashSet<ConveyorSurfaceZone>();
        private Vector3 _lastAppliedSurfaceVelocity;

        public Vector3 CurrentSurfaceVelocity { get; private set; }
        public ConveyorBelt CurrentConveyor { get; private set; }

        public void Configure(
            Rigidbody rigidbodyMotor,
            CharacterController characterController,
            bool moveCharacterControllerAutomatically)
        {
            _rigidbody = rigidbodyMotor;
            _characterController = characterController;
            _moveCharacterControllerAutomatically = moveCharacterControllerAutomatically;
        }

        private void Awake()
        {
            if (_rigidbody == null)
            {
                TryGetComponent(out _rigidbody);
            }

            if (_characterController == null)
            {
                TryGetComponent(out _characterController);
            }
        }

        private void OnDisable()
        {
            _activeZones.Clear();
            CurrentConveyor = null;
            CurrentSurfaceVelocity = Vector3.zero;
            _lastAppliedSurfaceVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            RefreshCurrentConveyor();
            CurrentSurfaceVelocity = CurrentConveyor != null
                ? CurrentConveyor.SurfaceVelocity
                : Vector3.zero;

            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                Vector3 change = CurrentSurfaceVelocity - _lastAppliedSurfaceVelocity;
                float maximumChange = _maximumAcceleration * Time.fixedDeltaTime;
                Vector3 appliedChange = Vector3.ClampMagnitude(change, maximumChange);
                _rigidbody.linearVelocity += appliedChange;
                _lastAppliedSurfaceVelocity += appliedChange;
                return;
            }

            _lastAppliedSurfaceVelocity = Vector3.zero;

            if (CurrentSurfaceVelocity.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            if (_characterController != null && _moveCharacterControllerAutomatically)
            {
                _characterController.Move(CurrentSurfaceVelocity * Time.fixedDeltaTime);
                return;
            }

            if (_moveTransformWithoutMotor)
            {
                transform.position += CurrentSurfaceVelocity * Time.fixedDeltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out ConveyorSurfaceZone zone))
            {
                _activeZones.Add(zone);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out ConveyorSurfaceZone zone))
            {
                _activeZones.Remove(zone);
            }
        }

        private void RefreshCurrentConveyor()
        {
            CurrentConveyor = null;
            _activeZones.RemoveWhere(zone => zone == null || !zone.isActiveAndEnabled || zone.Owner == null);

            foreach (ConveyorSurfaceZone zone in _activeZones)
            {
                CurrentConveyor = zone.Owner;
                break;
            }
        }
    }
}
