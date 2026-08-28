using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    public interface IMaintenanceTimedInteractable : ITimedInteractable
    {
        Vector3 MaintenanceEffectPosition { get; }
    }
}
