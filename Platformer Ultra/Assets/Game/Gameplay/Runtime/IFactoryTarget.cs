using PlatformerUltra.Combat;

namespace PlatformerUltra.Gameplay
{
    public interface IFactoryTarget
    {
        bool IsEligibleTarget { get; }
        Targetable Targetable { get; }
    }
}
