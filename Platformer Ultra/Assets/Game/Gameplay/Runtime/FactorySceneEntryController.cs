using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class FactorySceneEntryController : MonoBehaviour
    {
        [SerializeField] private ThirdPersonPlayerController _playerController;
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private ThirdPersonOrbitCamera _orbitCamera;
        [SerializeField] private Targetable _playerTargetable;
        [SerializeField] private PlayerStatusPresenter _statusPresenter;
        [SerializeField] private FactoryHudPresenter _factoryHud;
        [SerializeField] private FactoryPauseController _pauseController;
        [SerializeField] private MonoBehaviour _spawnManagerBehaviour;

        private IEnemySpawningController _spawnManager;
        private bool _released;

        public static FactorySceneEntryController Current { get; private set; }
        public bool IsReleased => _released;

        private void Awake()
        {
            Current = this;
            ResolveSpawnManager();
            _factoryHud?.SetStartupTutorialDeferred(true);
            if (FactorySceneTransition.IsTransitioningToFactory)
            {
                HoldGameplay();
            }
        }

        private void Start()
        {
            if (FactorySceneTransition.IsTransitioningToFactory)
            {
                HoldGameplay();
            }
            else
            {
                ReleaseGameplay();
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public void Configure(
            ThirdPersonPlayerController playerController,
            PlayerInteractor playerInteractor,
            ThirdPersonOrbitCamera orbitCamera,
            Targetable playerTargetable,
            PlayerStatusPresenter statusPresenter,
            FactoryHudPresenter factoryHud,
            FactoryPauseController pauseController,
            MonoBehaviour spawnManagerBehaviour)
        {
            _playerController = playerController;
            _playerInteractor = playerInteractor;
            _orbitCamera = orbitCamera;
            _playerTargetable = playerTargetable;
            _statusPresenter = statusPresenter;
            _factoryHud = factoryHud;
            _pauseController = pauseController;
            _spawnManagerBehaviour = spawnManagerBehaviour;
            ResolveSpawnManager();
            _factoryHud?.SetStartupTutorialDeferred(true);
        }

        public void HoldGameplay()
        {
            _released = false;
            _factoryHud?.SetStartupTutorialDeferred(true);
            _statusPresenter?.HideGameplayHud();
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

            _playerTargetable?.SetTargetable(false);
            _pauseController?.SetPauseAllowed(false);
            _spawnManager?.SetSpawningEnabled(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseGameplay()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = true;
            }

            _playerController?.SetLocomotionLocked(false);
            if (_orbitCamera != null)
            {
                _orbitCamera.enabled = true;
            }

            _playerTargetable?.SetTargetable(true);
            _pauseController?.SetPauseAllowed(true);
            _spawnManager?.SetSpawningEnabled(true);
            _statusPresenter?.ShowGameplayHud();
            _factoryHud?.BeginStartupTutorial();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ResolveSpawnManager()
        {
            _spawnManager = _spawnManagerBehaviour as IEnemySpawningController;
        }
    }
}
