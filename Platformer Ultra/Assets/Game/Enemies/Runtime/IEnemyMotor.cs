using UnityEngine;

namespace PlatformerUltra.Enemies
{
    public interface IEnemyMotor
    {
        bool IsReady { get; }
        bool IsMoving { get; }
        Vector3 Velocity { get; }
        void Configure(EnemyDefinition definition);
        bool TryPlace(Vector3 position, float searchRadius);
        bool SetDestination(Vector3 position, float stoppingDistance, bool chasingPlayer);
        bool CanReach(Vector3 position, float searchRadius);
        bool TryResolveLanding(Vector3 desiredPosition, float searchRadius, out Vector3 landingPosition);
        void Stop();
        void FaceTarget(Vector3 targetPosition, float deltaTime);
        void BeginScriptedMotion();
        void SetScriptedPosition(Vector3 position);
        void EndScriptedMotion(Vector3 landingPosition);
    }
}
