using System;

namespace PlatformerUltra.Combat
{
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaximumHealth { get; }
        bool IsAlive { get; }
        event Action<DamageInfo> Damaged;
        event Action<DamageInfo> Died;
        bool TakeDamage(DamageInfo damageInfo);
    }
}
