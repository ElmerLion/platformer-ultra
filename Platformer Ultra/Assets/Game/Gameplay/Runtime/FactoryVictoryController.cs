using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryVictoryController : MonoBehaviour
    {
        [SerializeField] private Transform _playerRoot;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private ThirdPersonPlayerController _playerController;
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private ThirdPersonOrbitCamera _orbitCamera;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _portalDestination;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private PlayerStatusPresenter _statusPresenter;
        [SerializeField] private MonoBehaviour _spawnManagerBehaviour;
        [SerializeField] private Renderer[] _playerRenderers = Array.Empty<Renderer>();
        [SerializeField, Min(0.25f)] private float _cinematicDuration = 2.6f;

        private Vector3 _playerStartPosition;
        private Quaternion _playerStartRotation;
        private Vector3 _playerStartScale;
        private Vector3 _cameraStartPosition;
        private Quaternion _cameraStartRotation;
        private float _cinematicElapsed;
        private bool _buttonBound;
        private IEnemySpawningController _spawnManager;

        public bool IsVictorious { get; private set; }
        public bool IsCinematicComplete { get; private set; }
        public float CinematicProgress => IsVictorious
            ? Mathf.Clamp01(_cinematicElapsed / Mathf.Max(0.25f, _cinematicDuration))
            : 0f;

        public event Action VictoryStarted;
        public event Action VictoryCompleted;

        private void Awake()
        {
            ResolveSpawnManager();
            RefreshPlayerRenderers();
        }

        private void OnEnable()
        {
            ResolveSpawnManager();
            RefreshPlayerRenderers();
            BindButton();
        }

        private void OnDisable()
        {
            UnbindButton();
            if (IsVictorious)
            {
                RestoreGlobalState();
            }
        }

        private void Update()
        {
            if (IsVictorious && !IsCinematicComplete)
            {
                AdvanceCinematic(Time.unscaledDeltaTime);
            }
        }

        public void Configure(
            Transform playerRoot,
            CharacterController characterController,
            PlayerHealth playerHealth,
            ThirdPersonPlayerController playerController,
            PlayerInteractor playerInteractor,
            ThirdPersonOrbitCamera orbitCamera,
            Transform cameraTransform,
            Transform portalDestination,
            Transform cameraAnchor,
            PlayerStatusPresenter statusPresenter,
            MonoBehaviour spawnManagerBehaviour,
            Renderer[] playerRenderers,
            float cinematicDuration = 2.6f)
        {
            UnbindButton();
            _playerRoot = playerRoot;
            _characterController = characterController;
            _playerHealth = playerHealth;
            _playerController = playerController;
            _playerInteractor = playerInteractor;
            _orbitCamera = orbitCamera;
            _cameraTransform = cameraTransform;
            _portalDestination = portalDestination;
            _cameraAnchor = cameraAnchor;
            _statusPresenter = statusPresenter;
            _spawnManagerBehaviour = spawnManagerBehaviour;
            ResolveSpawnManager();
            _playerRenderers = playerRenderers ?? Array.Empty<Renderer>();
            RefreshPlayerRenderers();
            _cinematicDuration = Mathf.Max(0.25f, cinematicDuration);
            BindButton();
        }

        public bool BeginVictory()
        {
            if (IsVictorious || _playerRoot == null || _portalDestination == null ||
                _cameraTransform == null || _cameraAnchor == null)
            {
                return false;
            }

            IsVictorious = true;
            IsCinematicComplete = false;
            _cinematicElapsed = 0f;
            _playerStartPosition = _playerRoot.position;
            _playerStartRotation = _playerRoot.rotation;
            _playerStartScale = _playerRoot.localScale;
            _cameraStartPosition = _cameraTransform.position;
            _cameraStartRotation = _cameraTransform.rotation;

            _spawnManager?.SetSpawningEnabled(false);
            _playerHealth?.Targetable?.SetTargetable(false);
            _playerInteractor?.CancelActiveInteraction();
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = false;
            }

            _playerController?.SetLocomotionLocked(true);
            if (_playerController != null)
            {
                _playerController.enabled = false;
            }

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            if (_orbitCamera != null)
            {
                _orbitCamera.enabled = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _statusPresenter?.HideGameplayHud();
            VictoryStarted?.Invoke();
            return true;
        }

        public void AdvanceCinematic(float unscaledDeltaTime)
        {
            if (!IsVictorious || IsCinematicComplete)
            {
                return;
            }

            _cinematicElapsed += Mathf.Max(0f, unscaledDeltaTime);
            float normalized = Mathf.Clamp01(_cinematicElapsed / _cinematicDuration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            _playerRoot.position = Vector3.Lerp(_playerStartPosition, _portalDestination.position, eased);
            _playerRoot.rotation = Quaternion.Slerp(_playerStartRotation, _portalDestination.rotation, eased);
            _playerRoot.localScale = Vector3.Lerp(_playerStartScale, _playerStartScale * 0.08f, eased);
            _cameraTransform.position = Vector3.Lerp(_cameraStartPosition, _cameraAnchor.position, eased);
            _cameraTransform.rotation = Quaternion.Slerp(_cameraStartRotation, _cameraAnchor.rotation, eased);

            if (normalized >= 1f)
            {
                CompleteVictory();
            }
        }

        public void CompleteVictory()
        {
            if (!IsVictorious || IsCinematicComplete)
            {
                return;
            }

            IsCinematicComplete = true;
            RefreshPlayerRenderers();
            for (int index = 0; index < _playerRenderers.Length; index++)
            {
                if (_playerRenderers[index] != null)
                {
                    _playerRenderers[index].enabled = false;
                }
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _statusPresenter?.ShowVictory();
            VictoryCompleted?.Invoke();
        }

        private void HandleReplayRequested()
        {
            RestoreGlobalState();
            if (!Application.isPlaying)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrWhiteSpace(activeScene.path))
            {
                SceneManager.LoadSceneAsync(activeScene.path, LoadSceneMode.Single);
            }
            else if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);
            }
        }

        private void RestoreGlobalState()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void ResolveSpawnManager()
        {
            _spawnManager = _spawnManagerBehaviour as IEnemySpawningController;
        }

        private void RefreshPlayerRenderers()
        {
            if (_playerRoot != null)
            {
                _playerRenderers = _playerRoot.GetComponentsInChildren<Renderer>(true);
            }
        }

        private void BindButton()
        {
            if (_buttonBound || _statusPresenter == null || !isActiveAndEnabled)
            {
                return;
            }

            _statusPresenter.VictoryRetryRequested += HandleReplayRequested;
            _buttonBound = true;
        }

        private void UnbindButton()
        {
            if (_buttonBound && _statusPresenter != null)
            {
                _statusPresenter.VictoryRetryRequested -= HandleReplayRequested;
            }

            _buttonBound = false;
        }
    }
}
