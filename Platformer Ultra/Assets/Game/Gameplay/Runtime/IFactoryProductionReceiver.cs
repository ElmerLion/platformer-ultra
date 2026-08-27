namespace PlatformerUltra.Gameplay
{
    public interface IFactoryProductionReceiver
    {
        int DeliveredCount { get; }
        int RequiredCount { get; }
        void ReceivePortalComponent();
    }
}
