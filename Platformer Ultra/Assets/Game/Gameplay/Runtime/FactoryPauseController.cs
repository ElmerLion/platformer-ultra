using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryPauseController : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pauseAction;
        [SerializeField] private PlayerStatusPresenter _statusPresenter;
        [SerializeField] private ThirdPersonPlayerController _playerController;
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private ThirdPersonOrbitCamera _orbitCamera;
        [SerializeField] private FactoryGameOverController _gameOverController;
        [SerializeField] private FactoryVictoryController _victoryController;

        private float _resumeTimeScale = 1f;
        private bool _presenterBound;

        public bool IsPaused { get; private set; }
        public bool PauseAllowed { get; private set; } = true;

        private void OnEnable()
        {
            _pauseAction?.action.Enable();
            BindPresenter();
        }

        private void OnDisable()
        {
            _pauseAction?.action.Disable();
            UnbindPresenter();
            if (IsPaused)
            {
                ResumeGame();
            }
        }

        private void Update()
        {
            if (_pauseAction == null || !_pauseAction.action.WasPressedThisFrame())
            {
                return;
            }

            TogglePause();
        }

        public void Configure(
            InputActionReference pauseAction,
            PlayerStatusPresenter statusPresenter,
            ThirdPersonPlayerController playerController,
            PlayerInteractor playerInteractor,
            ThirdPersonOrbitCamera orbitCamera,
            FactoryGameOverController gameOverController,
            FactoryVictoryController victoryController)
        {
            UnbindPresenter();
            _pauseAction = pauseAction;
            _statusPresenter = statusPresenter;
            _playerController = playerController;
            _playerInteractor = playerInteractor;
            _orbitCamera = orbitCamera;
            _gameOverController = gameOverController;
            _victoryController = victoryController;
            BindPresenter();
        }

        public bool TogglePause()
        {
            if (!PauseAllowed)
            {
                return false;
            }

            if (IsPaused)
            {
                ResumeGame();
                return true;
            }

            if ((_gameOverController != null && _gameOverController.IsGameOver) ||
                (_victoryController != null && _victoryController.IsVictorious))
            {
                return false;
            }

            PauseGame();
            return true;
        }

        public void SetPauseAllowed(bool allowed)
        {
            PauseAllowed = allowed;
        }

        public void PauseGame()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            _resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _playerInteractor?.CancelActiveInteraction();
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = false;
            }

            _playerController?.SetLocomotionLocked(true);
            if (_orbitCamera != null)
            {
                _orbitCamera.enabled = false;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _statusPresenter?.ShowPause();
        }

        public void ResumeGame()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = Mathf.Max(0.0001f, _resumeTimeScale);
            AudioListener.pause = false;
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = true;
            }

            _playerController?.SetLocomotionLocked(false);
            if (_orbitCamera != null)
            {
                _orbitCamera.enabled = true;
            }

            _statusPresenter?.HidePause();
        }

        private void HandleResumeRequested()
        {
            ResumeGame();
        }

        private void HandleRetryRequested()
        {
            RestoreGlobalState();
            if (!Application.isPlaying)
            {
                return;
            }

            FactoryRunSceneLoader.LoadNewRun();
        }

        private void RestoreGlobalState()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void BindPresenter()
        {
            if (_presenterBound || _statusPresenter == null || !isActiveAndEnabled)
            {
                return;
            }

            _statusPresenter.ResumeRequested += HandleResumeRequested;
            _statusPresenter.PauseRetryRequested += HandleRetryRequested;
            _presenterBound = true;
        }

        private void UnbindPresenter()
        {
            if (!_presenterBound || _statusPresenter == null)
            {
                _presenterBound = false;
                return;
            }

            _statusPresenter.ResumeRequested -= HandleResumeRequested;
            _statusPresenter.PauseRetryRequested -= HandleRetryRequested;
            _presenterBound = false;
        }
    }
}
