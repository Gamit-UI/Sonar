using System.Collections;

namespace Sonar.Helpers;

internal sealed class ConcurrentCircularBuffer<T> : IEnumerable<T>
{
    private readonly Lock locker = new();
    private readonly int capacity;
    private Node? head;
    private Node? tail;
    private int count;

    private class Node(T item)
    {
        public readonly T Item = item;
        public Node? Next;
    }

    public ConcurrentCircularBuffer(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity;
    }

    public int Count => Volatile.Read(ref count);

    public void Enqueue(T item)
    {
        Node node = new(item);
        lock (locker)
        {
            if (head is null) head = node;
            if (tail is not null) tail.Next = node;
            tail = node;
            if (count < capacity) count++;
            else head = head.Next;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        Node? node;
        int currentCount;
        lock (locker)
        {
            node = head;
            currentCount = count;
        }

        for (int i = 0; i < currentCount && node is not null; i++, node = node.Next)
            yield return node.Item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}