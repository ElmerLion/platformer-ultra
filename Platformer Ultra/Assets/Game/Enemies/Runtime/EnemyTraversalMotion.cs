using UnityEngine;

namespace PlatformerUltra.Enemies
{
    public static class EnemyTraversalMotion
    {
        public static Vector3 EvaluateLadder(Vector3 start, Vector3 end, float progress)
        {
            return Vector3.LerpUnclamped(start, end, Smooth01(progress));
        }

        public static Vector3 EvaluateJump(Vector3 start, Vector3 end, float progress, float arcHeight)
        {
            float normalized = Mathf.Clamp01(progress);
            Vector3 position = Vector3.LerpUnclamped(start, end, normalized);
            position.y += Mathf.Sin(normalized * Mathf.PI) * Mathf.Max(0f, arcHeight);
            return position;
        }

        public static Vector3 GetNearestEndpoint(Vector3 position, Vector3 start, Vector3 end)
        {
            return (position - start).sqrMagnitude <= (position - end).sqrMagnitude
                ? start
                : end;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
