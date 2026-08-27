using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class CameraShakeController : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)] private float _positionMultiplier = 1f;
        [SerializeField, Range(0f, 8f)] private float _rotationMultiplier = 3.2f;

        private float _amplitude;
        private float _duration;
        private float _remaining;
        private float _frequency;
        private float _noiseTime;
        private float _seed = 13.7f;

        public int ImpulseCount { get; private set; }
        public bool IsShaking => _remaining > 0f && _amplitude > 0f;

        public void Play(float amplitude, float duration, float frequency = 24f)
        {
            float clampedAmplitude = Mathf.Max(0f, amplitude);
            float clampedDuration = Mathf.Max(0f, duration);
            if (clampedAmplitude <= 0f || clampedDuration <= 0f)
            {
                return;
            }

            _amplitude = Mathf.Max(_amplitude, clampedAmplitude);
            _duration = Mathf.Max(_duration, clampedDuration);
            _remaining = Mathf.Max(_remaining, clampedDuration);
            _frequency = Mathf.Max(1f, frequency);
            _noiseTime = 0f;
            _seed += 19.31f;
            ImpulseCount++;
        }

        public void PlayAt(
            Vector3 worldPosition,
            float amplitude,
            float duration,
            float frequency,
            float maximumDistance)
        {
            float distance = Vector3.Distance(transform.position, worldPosition);
            float attenuation = maximumDistance <= 0f
                ? 1f
                : 1f - Mathf.Clamp01(distance / maximumDistance);
            Play(amplitude * attenuation * attenuation, duration, frequency);
        }

        public void Sample(float deltaTime, out Vector3 localPosition, out Vector3 localEulerAngles)
        {
            if (!IsShaking)
            {
                localPosition = Vector3.zero;
                localEulerAngles = Vector3.zero;
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Mathf.Max(0f, deltaTime));
            _noiseTime += Mathf.Max(0f, deltaTime) * _frequency;
            float envelope = _duration > 0f ? Mathf.Clamp01(_remaining / _duration) : 0f;
            envelope *= envelope;
            float strength = _amplitude * envelope;

            float x = SignedPerlin(_seed, _noiseTime);
            float y = SignedPerlin(_seed + 31.7f, _noiseTime * 1.07f);
            float roll = SignedPerlin(_seed + 67.1f, _noiseTime * 0.91f);
            localPosition = new Vector3(x, y, 0f) * (strength * _positionMultiplier);
            localEulerAngles = new Vector3(y, x, roll) * (strength * _rotationMultiplier);

            if (_remaining <= 0f)
            {
                _amplitude = 0f;
                _duration = 0f;
            }
        }

        public void Clear()
        {
            _amplitude = 0f;
            _duration = 0f;
            _remaining = 0f;
            _noiseTime = 0f;
        }

        private static float SignedPerlin(float seed, float time)
        {
            return Mathf.PerlinNoise(seed, time) * 2f - 1f;
        }
    }
}
