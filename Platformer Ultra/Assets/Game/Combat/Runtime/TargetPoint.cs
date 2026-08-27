using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class TargetPoint : MonoBehaviour
    {
        public Transform AimTransform => transform;
    }
}
