using System;

namespace PlatformerUltra.Gameplay
{
    public interface IFactoryProductionReceiver
    {
        int DeliveredCount { get; }
        int RequiredCount { get; }
        event Action<int, int> ProgressChanged;
        void ReceivePortalComponent();
    }
}
