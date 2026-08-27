using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ConveyorDestinationMarker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _bobHeight = 0.22f;
        [SerializeField, Min(0f)] private float _bobSpeed = 2.2f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 55f;

        private Vector3 _baseLocalPosition;

        private void OnEnable()
        {
            _baseLocalPosition = transform.localPosition;
        }

        private void OnDisable()
        {
            transform.localPosition = _baseLocalPosition;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.localPosition = _baseLocalPosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
