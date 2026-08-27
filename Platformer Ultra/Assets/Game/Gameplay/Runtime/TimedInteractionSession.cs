using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    public enum TimedInteractionTickResult
    {
        Inactive,
        InProgress,
        Completed,
        Cancelled
    }

    public sealed class TimedInteractionSession
    {
        private ITimedInteractable _target;
        private GameObject _interactor;
        private float _elapsedTime;

        public bool IsActive => _target != null;
        public ITimedInteractable Target => _target;
        public float ElapsedTime => _elapsedTime;
        public float Progress => IsActive
            ? Mathf.Clamp01(_elapsedTime / Mathf.Max(0.01f, _target.InteractionDuration))
            : 0f;

        public bool TryBegin(ITimedInteractable target, GameObject interactor)
        {
            Cancel();
            if (target == null || interactor == null || target.InteractionDuration <= 0f ||
                !target.CanInteract(interactor) || !target.BeginTimedInteraction(interactor))
            {
                return false;
            }

            _target = target;
            _interactor = interactor;
            _elapsedTime = 0f;
            return true;
        }

        public TimedInteractionTickResult Tick(float deltaTime, bool canContinue)
        {
            if (!IsActive)
            {
                return TimedInteractionTickResult.Inactive;
            }

            if (!canContinue || !_target.CanInteract(_interactor))
            {
                Cancel();
                return TimedInteractionTickResult.Cancelled;
            }

            _elapsedTime += Mathf.Max(0f, deltaTime);
            if (_elapsedTime + Mathf.Epsilon < _target.InteractionDuration)
            {
                return TimedInteractionTickResult.InProgress;
            }

            ITimedInteractable completedTarget = _target;
            GameObject completedInteractor = _interactor;
            Clear();
            return completedTarget.CompleteTimedInteraction(completedInteractor)
                ? TimedInteractionTickResult.Completed
                : TimedInteractionTickResult.Cancelled;
        }

        public void Cancel()
        {
            if (_target != null)
            {
                _target.CancelTimedInteraction(_interactor);
            }

            Clear();
        }

        private void Clear()
        {
            _target = null;
            _interactor = null;
            _elapsedTime = 0f;
        }
    }
}
