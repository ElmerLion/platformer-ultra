using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ConveyorConnectionPoint : MonoBehaviour, IInteractable, IInteractionFeedback
    {
        [SerializeField] private FactoryConveyorConnection _connection;
        [SerializeField] private bool _isSource;

        private string _lastInteractionFeedback = string.Empty;

        public string InteractionPrompt
        {
            get
            {
                if (_connection == null)
                {
                    return "Unavailable Conveyor Socket";
                }

                if (_isSource)
                {
                    return _connection.IsAwaitingDestination
                        ? "Cancel " + _connection.ConnectionName
                        : "Connect " + _connection.ConnectionName;
                }

                return "Build " + _connection.ConnectionName;
            }
        }

        public string LastInteractionFeedback => _lastInteractionFeedback;

        public void Configure(FactoryConveyorConnection connection, bool isSource)
        {
            _connection = connection;
            _isSource = isSource;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_connection == null || _connection.IsBuilt)
            {
                return false;
            }

            return _isSource || _connection.IsAwaitingDestination;
        }

        public void Interact(GameObject interactor)
        {
            if (_connection == null)
            {
                _lastInteractionFeedback = "This conveyor socket is not configured.";
                return;
            }

            if (_isSource)
            {
                _connection.SelectSource(out _lastInteractionFeedback);
            }
            else
            {
                _connection.BuildFromDestination(out _lastInteractionFeedback);
            }
        }
    }
}
