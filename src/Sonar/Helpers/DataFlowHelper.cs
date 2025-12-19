using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace Sonar.Helpers;

internal static class DataFlowHelper
{
    internal abstract class PeriodicBlock<T> : IAsyncDisposable
    {
        public abstract ValueTask DisposeAsync();
        public abstract bool Post(T item);
        public abstract Task<bool> SendAsync(T item, CancellationToken cancellationToken);
        public abstract IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions);
    }

    private sealed class BatchPeriodicBlock<T> : PeriodicBlock<T>, IPropagatorBlock<T, T[]>, IReceivableSourceBlock<T[]>
    {
        private readonly BatchBlock<T> source;
        private readonly Timer timer;

        public BatchPeriodicBlock(TimeSpan timeSpan, int count)
        {
            source = new BatchBlock<T>(count, new GroupingDataflowBlockOptions
            {
                BoundedCapacity = count * 4,
                Greedy = true,
                EnsureOrdered = true
            });

            timer = new Timer(_ => source.TriggerBatch(), state: null, TimeSpan.Zero, timeSpan);
        }

        public Task Completion => source.Completion;
        public void Complete() => source.Complete();
        void IDataflowBlock.Fault(Exception exception) => ((IDataflowBlock)source).Fault(exception);
        public override bool Post(T item) => source.Post(item);
        public override Task<bool> SendAsync(T item, CancellationToken cancellationToken) => source.SendAsync(item, cancellationToken);
        public override IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions) => source.LinkTo(target, linkOptions);
        public bool TryReceive(Predicate<T[]>? filter, [NotNullWhen(true)] out T[]? item) => source.TryReceive(filter, out item);
        public bool TryReceiveAll([NotNullWhen(true)] out IList<T[]>? items) => source.TryReceiveAll(out items);
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept) => ((ITargetBlock<T>)this.source).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        T[]? ISourceBlock<T[]>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target, out bool messageConsumed) => ((ISourceBlock<T[]>)source).ConsumeMessage(messageHeader, target, out messageConsumed);
        bool ISourceBlock<T[]>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target) => ((ISourceBlock<T[]>)source).ReserveMessage(messageHeader, target);
        void ISourceBlock<T[]>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target) => ((ISourceBlock<T[]>)source).ReleaseReservation(messageHeader, target);

        public override async ValueTask DisposeAsync()
        {
            Complete();
            await timer.DisposeAsync();
        }
    }

    public static PeriodicBlock<TIn> CreatePeriodicBlock<TIn>(TimeSpan timeSpan, int count)
    {
        return new BatchPeriodicBlock<TIn>(timeSpan, count);
    }
}