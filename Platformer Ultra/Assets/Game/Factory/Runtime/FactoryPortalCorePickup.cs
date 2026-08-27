using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryPortalCorePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private FactoryPortalGate _gate;
        [SerializeField] private int _socketIndex;
        [SerializeField] private GameObject _coreVisual;
        [SerializeField] private Renderer _pedestalIndicator;
        [SerializeField] private string _coreName = "Portal Core";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _propertyBlock;
        private bool _collected;

        public bool IsCollected => _collected;
        public string InteractionPrompt => _collected ? _coreName + " Installed" : "Collect " + _coreName;

        private void Awake()
        {
            ApplyVisualState();
        }

        public void Configure(
            FactoryPortalGate gate,
            int socketIndex,
            GameObject coreVisual,
            Renderer pedestalIndicator,
            string coreName)
        {
            _gate = gate;
            _socketIndex = Mathf.Max(0, socketIndex);
            _coreVisual = coreVisual;
            _pedestalIndicator = pedestalIndicator;
            _coreName = string.IsNullOrWhiteSpace(coreName) ? "Portal Core" : coreName;
            ApplyVisualState();
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_collected && _gate != null;
        }

public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            _collected = true;
            _gate.CollectCore(_socketIndex);
            ApplyVisualState();

            if (_coreVisual == gameObject)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyVisualState()
        {
            if (_coreVisual != null && _coreVisual != gameObject)
            {
                _coreVisual.SetActive(!_collected);
            }

            if (_pedestalIndicator == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Color color = _collected
                ? new Color(0.07f, 0.9f, 0.42f)
                : new Color(0.06f, 0.62f, 0.82f);
            _pedestalIndicator.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * (_collected ? 2.5f : 1.35f));
            _pedestalIndicator.SetPropertyBlock(_propertyBlock);
        }
    }
}
