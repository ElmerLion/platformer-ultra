using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors
{
    [DisallowMultipleComponent]
    public sealed class ConveyorSurfaceZone : MonoBehaviour
    {
        [SerializeField] private ConveyorBelt _owner;

        public ConveyorBelt Owner => _owner;

        public void Configure(ConveyorBelt owner)
        {
            _owner = owner;
        }
    }
}
