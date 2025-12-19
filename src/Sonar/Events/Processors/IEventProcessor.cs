namespace Sonar.Events.Processors;

internal interface IEventProcessor : IDisposable
{
    void Initialize();
}