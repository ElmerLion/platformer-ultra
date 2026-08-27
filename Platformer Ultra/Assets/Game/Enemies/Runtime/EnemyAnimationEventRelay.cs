using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private EnemyAttackController _attackController;

        public void Configure(EnemyAttackController attackController)
        {
            _attackController = attackController;
        }

        public void OnAttackImpact()
        {
            _attackController?.OnAttackImpact();
        }

        public void OnSpecialImpact()
        {
            _attackController?.OnSpecialImpact();
        }
    }
}
