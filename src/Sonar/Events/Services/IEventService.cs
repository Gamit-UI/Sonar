namespace Sonar.Events.Services;

internal interface IEventService : IAsyncDisposable
{
    Task ConsumeAsync(CancellationToken cancellationToken);
}