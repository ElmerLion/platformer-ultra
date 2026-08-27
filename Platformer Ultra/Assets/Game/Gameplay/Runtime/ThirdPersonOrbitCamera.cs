using PlatformerUltra.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private InputActionReference _lookAction;
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField, Min(1f)] private float _distance = 5.5f;
        [SerializeField] private float _mouseSensitivity = 0.13f;
        [SerializeField] private float _gamepadDegreesPerSecond = 150f;
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-35f, 70f);
        [SerializeField, Min(0f)] private float _positionSmoothTime = 0.055f;
        [SerializeField, Min(0.01f)] private float _collisionRadius = 0.25f;
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField] private CameraShakeController _shakeController;

        private Vector3 _positionVelocity;
        private Vector3 _lastShakeWorldOffset;
        private float _yaw;
        private float _pitch = 18f;

        private void OnEnable()
        {
            _shakeController ??= GetComponent<CameraShakeController>();
            _lookAction?.action.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            _shakeController?.Clear();
            _lastShakeWorldOffset = Vector3.zero;
            _lookAction?.action.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            UpdateLook();
            Vector3 pivot = _target.position + _pivotOffset;
            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = pivot - orbitRotation * Vector3.forward * _distance;
            Vector3 castDirection = desiredPosition - pivot;
            float allowedDistance = _distance;

            if (Physics.SphereCast(
                    pivot,
                    _collisionRadius,
                    castDirection.normalized,
                    out RaycastHit hit,
                    _distance,
                    _collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                allowedDistance = Mathf.Max(0.4f, hit.distance - _collisionRadius);
                desiredPosition = pivot - orbitRotation * Vector3.forward * allowedDistance;
            }

            Vector3 basePosition = transform.position - _lastShakeWorldOffset;
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                basePosition,
                desiredPosition,
                ref _positionVelocity,
                _positionSmoothTime);
            Vector3 localShakePosition = Vector3.zero;
            Vector3 localShakeEuler = Vector3.zero;
            _shakeController?.Sample(Time.unscaledDeltaTime, out localShakePosition, out localShakeEuler);
            _lastShakeWorldOffset = orbitRotation * localShakePosition;
            transform.position = smoothedPosition + _lastShakeWorldOffset;
            transform.rotation = orbitRotation * Quaternion.Euler(localShakeEuler);
        }

        public void Configure(
            Transform target,
            InputActionReference lookAction,
            LayerMask collisionMask,
            CameraShakeController shakeController = null)
        {
            _target = target;
            _lookAction = lookAction;
            _collisionMask = collisionMask;
            _shakeController = shakeController;
        }

        private void UpdateLook()
        {
            if (_lookAction == null)
            {
                return;
            }

            Vector2 look = _lookAction.action.ReadValue<Vector2>();
            bool isGamepad = _lookAction.action.activeControl?.device is Gamepad;
            float multiplier = isGamepad
                ? _gamepadDegreesPerSecond * Time.unscaledDeltaTime
                : _mouseSensitivity;

            _yaw += look.x * multiplier;
            _pitch = Mathf.Clamp(_pitch - look.y * multiplier, _pitchLimits.x, _pitchLimits.y);
        }
    }
}
