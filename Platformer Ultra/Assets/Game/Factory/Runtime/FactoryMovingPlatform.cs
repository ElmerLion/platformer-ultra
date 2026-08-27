using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FactoryMovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 _localPointA;
        [SerializeField] private Vector3 _localPointB = new Vector3(0f, 3f, 0f);
        [SerializeField, Min(0.5f)] private float _travelTime = 3.5f;
        [SerializeField, Min(0f)] private float _pauseTime = 0.6f;
        [SerializeField] private bool _startPowered = true;

        private Rigidbody _body;
        private Transform _passenger;
        private Transform _passengerOriginalParent;
        private float _elapsed;
        private bool _powered;

        public bool IsPowered => _powered;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _powered = _startPowered;
            transform.localPosition = _localPointA;
        }

        private void FixedUpdate()
        {
            if (!_powered)
            {
                return;
            }

            _elapsed += Time.fixedDeltaTime;
            float legDuration = _travelTime + _pauseTime;
            float cycle = legDuration * 2f;
            float phase = Mathf.Repeat(_elapsed, cycle);
            bool returning = phase > legDuration;
            float legTime = returning ? phase - legDuration : phase;
            float normalized = Mathf.Clamp01((legTime - _pauseTime * 0.5f) / _travelTime);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            Vector3 target = returning
                ? Vector3.Lerp(_localPointB, _localPointA, eased)
                : Vector3.Lerp(_localPointA, _localPointB, eased);

            Transform parent = transform.parent;
            Vector3 worldTarget = parent != null ? parent.TransformPoint(target) : target;
            _body.MovePosition(worldTarget);
        }

        private void OnDisable()
        {
            ReleasePassenger();
        }

        private void OnTriggerEnter(Collider other)
        {
            CharacterController controller = other.GetComponent<CharacterController>();
            if (controller == null || _passenger != null)
            {
                return;
            }

            _passenger = controller.transform;
            _passengerOriginalParent = _passenger.parent;
            _passenger.SetParent(transform, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_passenger != null && other.transform == _passenger)
            {
                ReleasePassenger();
            }
        }

        public void Configure(
            Vector3 localPointA,
            Vector3 localPointB,
            float travelTime,
            float pauseTime = 0.6f,
            bool startPowered = true)
        {
            _localPointA = localPointA;
            _localPointB = localPointB;
            _travelTime = Mathf.Max(0.5f, travelTime);
            _pauseTime = Mathf.Max(0f, pauseTime);
            _startPowered = startPowered;
        }

        public void SetPowered(bool powered)
        {
            _powered = powered;
        }

        private void ReleasePassenger()
        {
            if (_passenger == null)
            {
                return;
            }

            _passenger.SetParent(_passengerOriginalParent, true);
            _passenger = null;
            _passengerOriginalParent = null;
        }
    }
}
