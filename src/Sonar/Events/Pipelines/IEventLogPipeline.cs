namespace Sonar.Events.Pipelines;

public interface IEventLogPipeline<T>
{
    bool Push(T data);
    
    IAsyncEnumerable<T> ConsumeAsync(CancellationToken cancellationToken);
}