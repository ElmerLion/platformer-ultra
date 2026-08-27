using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform _viewTransform;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private InteractionPromptPresenter _promptPresenter;
        [SerializeField, Min(0.5f)] private float _range = 4f;
        [SerializeField] private LayerMask _interactionMask = ~0;

        private CharacterController _characterController;
        private ThirdPersonPlayerController _playerController;
        private PlayerHealth _playerHealth;
        private Collider _cachedCollider;
        private InteractionTarget _cachedTarget;
        private readonly TimedInteractionSession _timedSession = new TimedInteractionSession();

        public bool IsHoldingInteraction => _timedSession.IsActive;
        public float TimedInteractionProgress => _timedSession.Progress;

        public event Action<ITimedInteractable> TimedInteractionStarted;
        public event Action<ITimedInteractable, bool> TimedInteractionEnded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<ThirdPersonPlayerController>();
            _playerHealth = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            _interactAction?.action.Enable();
            if (_playerHealth != null)
            {
                _playerHealth.Damaged += HandlePlayerInterrupted;
                _playerHealth.Died += HandlePlayerInterrupted;
            }
        }

        private void OnDisable()
        {
            _interactAction?.action.Disable();
            if (_playerHealth != null)
            {
                _playerHealth.Damaged -= HandlePlayerInterrupted;
                _playerHealth.Died -= HandlePlayerInterrupted;
            }

            CancelActiveInteraction();
            ClearTarget();
        }

        private void Update()
        {
            RefreshTarget();
            IInteractable interactable = _cachedTarget != null ? _cachedTarget.Interactable : null;
            bool canInteract = interactable != null && interactable.CanInteract(gameObject);

            if (_timedSession.IsActive)
            {
                UpdateTimedInteraction(interactable, canInteract);
                return;
            }

            _promptPresenter?.SetPrompt(canInteract
                ? $"[E] {interactable.InteractionPrompt}"
                : string.Empty);

            if (canInteract && _interactAction != null && _interactAction.action.WasPressedThisFrame())
            {
                ITimedInteractable timedInteractable = interactable as ITimedInteractable;
                if (timedInteractable != null && timedInteractable.InteractionDuration > 0f)
                {
                    TryBeginTimedInteraction(timedInteractable);
                    return;
                }

                interactable.Interact(gameObject);
                PresentFeedback(interactable);
            }
        }

        public void CancelActiveInteraction()
        {
            if (!_timedSession.IsActive)
            {
                return;
            }

            ITimedInteractable target = _timedSession.Target;
            _timedSession.Cancel();
            SetInteractionCommitment(false);
            TimedInteractionEnded?.Invoke(target, false);
        }

        public bool TryBeginTimedInteraction(ITimedInteractable interactable)
        {
            if (_playerController == null)
            {
                _playerController = GetComponent<ThirdPersonPlayerController>();
            }

            if (!_timedSession.TryBegin(interactable, gameObject))
            {
                return false;
            }

            SetInteractionCommitment(true);
            _promptPresenter?.SetPrompt("Release [E] to cancel");
            _promptPresenter?.ShowTimedProgress(interactable.InteractionActionLabel, 0f);
            TimedInteractionStarted?.Invoke(interactable);
            return true;
        }

        private void UpdateTimedInteraction(IInteractable currentInteractable, bool canInteract)
        {
            bool held = _interactAction != null && _interactAction.action.IsPressed();
            bool sameTarget = ReferenceEquals(currentInteractable, _timedSession.Target);
            ITimedInteractable activeTarget = _timedSession.Target;
            TimedInteractionTickResult result = _timedSession.Tick(
                Time.deltaTime,
                held && sameTarget && canInteract);

            if (result == TimedInteractionTickResult.InProgress)
            {
                _promptPresenter?.SetPrompt("Release [E] to cancel");
                _promptPresenter?.ShowTimedProgress(
                    ((ITimedInteractable)currentInteractable).InteractionActionLabel,
                    _timedSession.Progress);
                return;
            }

            SetInteractionCommitment(false);
            TimedInteractionEnded?.Invoke(
                activeTarget,
                result == TimedInteractionTickResult.Completed);
            if (result == TimedInteractionTickResult.Completed && currentInteractable != null)
            {
                PresentFeedback(currentInteractable);
            }
        }

        private void SetInteractionCommitment(bool committed)
        {
            _playerController?.SetLocomotionLocked(committed);
            if (!committed)
            {
                _promptPresenter?.HideTimedProgress();
            }
        }

        private void PresentFeedback(IInteractable interactable)
        {
            IInteractionFeedback feedback = interactable as IInteractionFeedback;
            string status = feedback != null && !string.IsNullOrWhiteSpace(feedback.LastInteractionFeedback)
                ? feedback.LastInteractionFeedback
                : interactable.InteractionPrompt;
            _promptPresenter?.SetStatus(status);
        }

        private void HandlePlayerInterrupted(PlatformerUltra.Combat.DamageInfo damageInfo)
        {
            CancelActiveInteraction();
        }

        public void Configure(
            Transform viewTransform,
            InputActionReference interactAction,
            InteractionPromptPresenter promptPresenter,
            LayerMask interactionMask)
        {
            _viewTransform = viewTransform;
            _interactAction = interactAction;
            _promptPresenter = promptPresenter;
            _interactionMask = interactionMask;
        }

        private void RefreshTarget()
        {
            Vector3 interactionOrigin = GetInteractionOrigin();
            float castDistance = Vector3.Distance(_viewTransform != null
                ? _viewTransform.position
                : interactionOrigin, interactionOrigin) + _range;

            if (_viewTransform == null || !Physics.Raycast(
                    _viewTransform.position,
                    _viewTransform.forward,
                    out RaycastHit hit,
                    castDistance,
                    _interactionMask,
                    QueryTriggerInteraction.Collide))
            {
                ClearTarget();
                return;
            }

            Vector3 closestPoint = hit.collider.ClosestPoint(interactionOrigin);
            if ((closestPoint - interactionOrigin).sqrMagnitude > _range * _range)
            {
                ClearTarget();
                return;
            }

            if (hit.collider == _cachedCollider)
            {
                return;
            }

            _cachedCollider = hit.collider;
            if (!_cachedCollider.TryGetComponent(out _cachedTarget))
            {
                _cachedTarget = _cachedCollider.GetComponentInParent<InteractionTarget>();
            }
        }

        private Vector3 GetInteractionOrigin()
        {
            return _characterController != null
                ? transform.TransformPoint(_characterController.center)
                : transform.position;
        }

        private void ClearTarget()
        {
            _cachedCollider = null;
            _cachedTarget = null;
            _promptPresenter?.SetPrompt(string.Empty);
        }
    }
}
