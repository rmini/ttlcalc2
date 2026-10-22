namespace TtlCalcSim.Testing;

/// <summary>A value written to an I/O address.</summary>
/// <param name="Step">Machine step count at the time of the write (1 = first instruction).</param>
public readonly record struct IoWrite(long Step, byte Addr, byte Value);

/// <summary>
/// I/O that records every write and supplies reads from queued values or handlers.
/// Reading an address with nothing queued and no handler throws.
/// </summary>
public class TestInputOutput(Func<long> currentStep) : InputOutput
{
    private readonly Dictionary<byte, Queue<byte>> _queues = [];
    private readonly Dictionary<byte, Func<byte>> _handlers = [];
    private readonly List<IoWrite> _writes = [];

    public IReadOnlyList<IoWrite> Writes => _writes;

    public void QueueInput(byte addr, params byte[] values)
    {
        if (!_queues.TryGetValue(addr, out var queue))
            _queues[addr] = queue = new Queue<byte>();
        foreach (var value in values)
            queue.Enqueue(value);
    }

    public void OnInput(byte addr, Func<byte> handler) => _handlers[addr] = handler;

    public override Nybble Read(byte addr)
    {
        if (_queues.TryGetValue(addr, out var queue) && queue.Count > 0)
            return queue.Dequeue();
        if (_handlers.TryGetValue(addr, out var handler))
            return handler();
        throw new InvalidOperationException($"Program read IO[0x{addr:X2}] but no input was queued or handled for it");
    }

    public override void Write(byte addr, Nybble d)
    {
        _writes.Add(new IoWrite(currentStep(), addr, d));
        base.Write(addr, d);
    }
}
