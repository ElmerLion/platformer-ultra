using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryCrusherVisual : MonoBehaviour
    {
        [SerializeField] private Transform _crushingPlate;
        [SerializeField] private float _topHeight = 3.15f;
        [SerializeField] private float _bottomHeight = 0.85f;
        [SerializeField, Min(0.1f)] private float _cycleDuration = 1.4f;
        [SerializeField] private bool _animate = true;

        public void Configure(Transform crushingPlate, float topHeight, float bottomHeight, float cycleDuration)
        {
            _crushingPlate = crushingPlate;
            _topHeight = topHeight;
            _bottomHeight = bottomHeight;
            _cycleDuration = Mathf.Max(0.1f, cycleDuration);
        }

        public void SetAnimating(bool animate)
        {
            _animate = animate;
        }

        private void Update()
        {
            if (!_animate || _crushingPlate == null)
            {
                return;
            }

            float phase = Mathf.Repeat(Time.time / _cycleDuration, 1f);
            float descent = phase < 0.42f
                ? Mathf.SmoothStep(0f, 1f, phase / 0.42f)
                : phase < 0.58f
                    ? 1f
                    : Mathf.SmoothStep(1f, 0f, (phase - 0.58f) / 0.42f);

            Vector3 position = _crushingPlate.localPosition;
            position.y = Mathf.Lerp(_topHeight, _bottomHeight, descent);
            _crushingPlate.localPosition = position;
        }
    }
}