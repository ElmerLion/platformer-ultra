using PlatformerUltra.Factory.Conveyors;
using UnityEngine;
using UnityEngine.Events;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ConveyorRouteTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private ConveyorBelt _conveyor;
        [SerializeField] private ConveyorEndpoint _startEndpoint;
        [SerializeField] private ConveyorEndpoint[] _routeEndpoints;
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private string _terminalName = "Conveyor Route";
        [SerializeField] private UnityEvent _routeChanged = new UnityEvent();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _propertyBlock;
        private int _routeIndex = -1;

        public string InteractionPrompt => _routeIndex < 0
            ? $"Generate {_terminalName}"
            : $"Switch {_terminalName} ({_routeIndex + 1}/{ValidRouteCount})";

        private int ValidRouteCount => _routeEndpoints != null ? _routeEndpoints.Length : 0;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            UpdateIndicator(false);
        }

        public void Configure(
            ConveyorBelt conveyor,
            ConveyorEndpoint startEndpoint,
            ConveyorEndpoint[] routeEndpoints,
            Renderer indicatorRenderer)
        {
            _conveyor = conveyor;
            _startEndpoint = startEndpoint;
            _routeEndpoints = routeEndpoints;
            _indicatorRenderer = indicatorRenderer;
            _routeIndex = -1;
            if (_conveyor != null)
            {
                _conveyor.gameObject.SetActive(false);
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return _conveyor != null && _startEndpoint != null && ValidRouteCount > 0;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            _routeIndex = (_routeIndex + 1) % ValidRouteCount;
            ConveyorEndpoint destination = _routeEndpoints[_routeIndex];
            if (destination == null)
            {
                return;
            }

            _conveyor.gameObject.SetActive(true);
            _conveyor.SetEndpoints(_startEndpoint, destination);
            _conveyor.SetOperatingState(ConveyorOperatingState.Online);
            UpdateIndicator(true);
            _routeChanged.Invoke();
        }

        private void UpdateIndicator(bool active)
        {
            if (_indicatorRenderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Color color = active
                ? Color.HSVToRGB(Mathf.Repeat(_routeIndex * 0.29f, 1f), 0.75f, 1f)
                : new Color(0.12f, 0.16f, 0.18f);
            _indicatorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(EmissionColorId, active ? color * 2f : Color.black);
            _indicatorRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
