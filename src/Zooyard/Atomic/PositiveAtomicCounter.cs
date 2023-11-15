namespace Zooyard.Atomic;

/// <summary>
/// 计数器，从0开始，保证正数。
/// </summary>
internal sealed record PositiveAtomicCounter
{
    private const int MASK = 0x7FFFFFFF;
    private readonly AtomicInteger atom;

    public PositiveAtomicCounter()
    {
        atom = new AtomicInteger(0);
    }

    public int IncrementAndGet()
    {
        return atom.IncrementAndGet() & MASK;
    }

    public int GetAndIncrement()
    {
        return atom.GetAndIncrement() & MASK;
    }

    public int Value
    {
        get
        {
            return atom.Value & MASK;
        }
    }
}
