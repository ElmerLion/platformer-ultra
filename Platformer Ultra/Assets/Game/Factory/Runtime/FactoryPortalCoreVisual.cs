using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryPortalCoreVisual : MonoBehaviour
    {
        [SerializeField] private Transform _orbitRootA;
        [SerializeField] private Transform _orbitRootB;
        [SerializeField] private Transform _core;
        [SerializeField] private float _orbitSpeedA = 55f;
        [SerializeField] private float _orbitSpeedB = -38f;
        [SerializeField] private float _pulseSpeed = 3.2f;
        [SerializeField] private float _pulseAmount = 0.08f;

        private Vector3 _coreBaseScale = Vector3.one;

        public void Configure(Transform orbitRootA, Transform orbitRootB, Transform core)
        {
            _orbitRootA = orbitRootA;
            _orbitRootB = orbitRootB;
            _core = core;
            _coreBaseScale = core != null ? core.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            if (_core != null)
            {
                _coreBaseScale = _core.localScale;
            }
        }

        private void Update()
        {
            if (_orbitRootA != null)
            {
                _orbitRootA.Rotate(0f, _orbitSpeedA * Time.deltaTime, 0f, Space.Self);
            }

            if (_orbitRootB != null)
            {
                _orbitRootB.Rotate(_orbitSpeedB * Time.deltaTime, 0f, 0f, Space.Self);
            }

            if (_core != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
                _core.localScale = _coreBaseScale * pulse;
            }
        }
    }
}