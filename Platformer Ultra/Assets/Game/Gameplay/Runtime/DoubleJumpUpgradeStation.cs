using System;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DoubleJumpUpgradeStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private string _prompt = "Install Double Jump Module";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Color _readyColor = new Color(0.08f, 0.75f, 1f);
        private readonly Color _installedColor = new Color(0.08f, 1f, 0.48f);
        private MaterialPropertyBlock _propertyBlock;
        private bool _installed;

        public bool IsInstalled => _installed;
        public string InteractionPrompt => _installed ? "Double Jump Online" : _prompt;

        public event Action Installed;

        private void Awake()
        {
            UpdateIndicator();
        }

        public void Configure(Renderer indicatorRenderer, string prompt = "Install Double Jump Module")
        {
            _indicatorRenderer = indicatorRenderer;
            _prompt = string.IsNullOrWhiteSpace(prompt) ? "Install Double Jump Module" : prompt;
            UpdateIndicator();
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_installed &&
                   interactor != null &&
                   interactor.GetComponentInParent<ThirdPersonPlayerController>() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            ThirdPersonPlayerController controller =
                interactor.GetComponentInParent<ThirdPersonPlayerController>();
            controller.UnlockDoubleJump();
            _installed = true;
            UpdateIndicator();
            Installed?.Invoke();
        }

        private void UpdateIndicator()
        {
            if (_indicatorRenderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Color color = _installed ? _installedColor : _readyColor;
            _indicatorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * (_installed ? 3f : 1.8f));
            _indicatorRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
