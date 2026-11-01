namespace YimyWalk.Core.Simulation;

/// <summary>Platform-neutral pending work queue used by the responsive UI batcher.</summary>
public sealed class BatchRequestQueue<T>
{
    private readonly Queue<Request> _requests = new();

    public int PendingCount { get; private set; }

    public void Enqueue(Func<T> factory, int count)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (count <= 0) return;
        if (_requests.Count >= 1024) throw new InvalidOperationException("Too many pending creation requests.");
        PendingCount = checked(PendingCount + count);
        _requests.Enqueue(new Request(factory, count));
    }

    public int Drain(int maximum, Action<T> consume)
    {
        ArgumentNullException.ThrowIfNull(consume);
        var count = 0;
        for (; count < Math.Max(0, maximum) && _requests.Count > 0; count++)
        {
            var request = _requests.Peek();
            request.Remaining--;
            PendingCount--;
            if (request.Remaining == 0) _requests.Dequeue();
            consume(request.Factory());
        }
        return count;
    }

    public void Cancel() { _requests.Clear(); PendingCount = 0; }

    private sealed class Request(Func<T> factory, int remaining)
    {
        public Func<T> Factory { get; } = factory;
        public int Remaining { get; set; } = remaining;
    }
}
