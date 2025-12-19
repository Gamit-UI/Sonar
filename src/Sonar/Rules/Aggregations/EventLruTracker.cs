using System.Collections.Concurrent;
using BitFaster.Caching;
using BitFaster.Caching.Lru;

namespace Sonar.Rules.Aggregations;

internal sealed class EventLruTracker
{
    private readonly ICache<long, byte> lru;
    private readonly ConcurrentQueue<long> deletedEventIds = new();

    public EventLruTracker(TimeSpan expiration, int maxEvents)
    {
        lru = BuildLru(expiration, maxEvents);
        if (lru.Events.Value is not null)
        {
            lru.Events.Value.ItemRemoved += OnItemRemoved;
        }
    }
    
    public void OnWinEventInsert(long id)
    {
        lru.AddOrUpdate(id, 0);
    }

    public void TrimExpired()
    {
        lru.Policy.ExpireAfterWrite.Value?.TrimExpired();
    }

    public void Clear() => lru.Clear();

    public ISet<long> GetDeletedEventIds()
    {
        var result = new HashSet<long>();
        while (deletedEventIds.TryDequeue(out var id))
        {
            result.Add(id);
        }

        return result;
    }

    private void OnItemRemoved(object? sender, ItemRemovedEventArgs<long, byte> args)
    {
        deletedEventIds.Enqueue(args.Key);
    }

    private static ICache<long, byte> BuildLru(TimeSpan timeframe, int capacity)
    {
        if (timeframe == Constants.DefaultTimeFrame)
        {
            return new ConcurrentLruBuilder<long, byte>()
                .WithCapacity(capacity)
                .WithMetrics()
                .Build();
        }

        return new ConcurrentLruBuilder<long, byte>()
            .WithCapacity(capacity)
            .WithExpireAfterWrite(timeframe)
            .WithMetrics()
            .Build();
    }
}