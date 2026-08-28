using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FactoryPortalCompletionTrigger : MonoBehaviour
    {
        [SerializeField] private FactoryPortalGate _portalGate;
        [SerializeField] private Transform _playerRoot;
        [SerializeField] private FactoryVictoryController _victoryController;
        [SerializeField] private Collider _triggerCollider;

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryComplete(other);
        }

        public void Configure(
            FactoryPortalGate portalGate,
            Transform playerRoot,
            FactoryVictoryController victoryController,
            Collider triggerCollider)
        {
            _portalGate = portalGate;
            _playerRoot = playerRoot;
            _victoryController = victoryController;
            _triggerCollider = triggerCollider;
            EnsureTriggerCollider();
        }

        public bool TryComplete(Collider other)
        {
            if (_portalGate == null || !_portalGate.IsOpen || _playerRoot == null ||
                _victoryController == null || other == null)
            {
                return false;
            }

            Transform otherTransform = other.transform;
            if (otherTransform != _playerRoot && !otherTransform.IsChildOf(_playerRoot))
            {
                return false;
            }

            return _victoryController.BeginVictory();
        }

        private void EnsureTriggerCollider()
        {
            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<Collider>();
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }
    }
}
