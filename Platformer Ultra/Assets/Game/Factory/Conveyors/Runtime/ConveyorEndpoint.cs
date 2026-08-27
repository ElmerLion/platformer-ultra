using UnityEngine;

namespace PlatformerUltra.Factory.Conveyors
{
    public enum ConveyorEndpointKind
    {
        Bidirectional = 0,
        Output = 1,
        Input = 2
    }

    [DisallowMultipleComponent]
    public sealed class ConveyorEndpoint : MonoBehaviour
    {
        [SerializeField] private ConveyorEndpointKind _kind = ConveyorEndpointKind.Bidirectional;
        [SerializeField, Min(0.05f)] private float _gizmoRadius = 0.2f;

        public ConveyorEndpointKind Kind => _kind;
        public float GizmoRadius => _gizmoRadius;

        public bool CanFeed(ConveyorEndpoint destination)
        {
            if (destination == null || destination == this)
            {
                return false;
            }

            bool canOutput = _kind != ConveyorEndpointKind.Input;
            bool canReceive = destination._kind != ConveyorEndpointKind.Output;
            return canOutput && canReceive;
        }

        public void Configure(ConveyorEndpointKind kind, float gizmoRadius = 0.2f)
        {
            _kind = kind;
            _gizmoRadius = Mathf.Max(0.05f, gizmoRadius);
        }

        private void OnValidate()
        {
            _gizmoRadius = Mathf.Max(0.05f, _gizmoRadius);
        }

        private void OnDrawGizmos()
        {
            Color color = _kind switch
            {
                ConveyorEndpointKind.Output => new Color(0.2f, 0.9f, 0.35f, 0.95f),
                ConveyorEndpointKind.Input => new Color(0.95f, 0.3f, 0.25f, 0.95f),
                _ => new Color(0.2f, 0.75f, 1f, 0.95f)
            };

            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * (_gizmoRadius * 2f));
        }
    }
}
