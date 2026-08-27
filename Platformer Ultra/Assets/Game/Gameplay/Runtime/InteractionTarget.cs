using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class InteractionTarget : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _interactableBehaviour;

        private IInteractable _interactable;

        public IInteractable Interactable => _interactable;

        private void Awake()
        {
            ResolveInteractable();
        }

        private void OnValidate()
        {
            ResolveInteractable();
        }

        public void Configure(MonoBehaviour interactableBehaviour)
        {
            _interactableBehaviour = interactableBehaviour;
            ResolveInteractable();
        }

        private void ResolveInteractable()
        {
            _interactable = _interactableBehaviour as IInteractable;
        }
    }
}
