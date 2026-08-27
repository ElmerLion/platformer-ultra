using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    public interface ITimedInteractable : IInteractable
    {
        float InteractionDuration { get; }
        string InteractionActionLabel { get; }
        bool BeginTimedInteraction(GameObject interactor);
        void CancelTimedInteraction(GameObject interactor);
        bool CompleteTimedInteraction(GameObject interactor);
    }
}
