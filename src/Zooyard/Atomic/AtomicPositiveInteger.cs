namespace Zooyard.Atomic;

internal sealed class AtomicPositiveInteger
{
    private readonly AtomicInteger i;

    public AtomicPositiveInteger()
    {
        i = new AtomicInteger();
    }

    public AtomicPositiveInteger(int initialValue)
    {
        i = new AtomicInteger(initialValue);
    }

    public int GetAndIncrement()
    {
        for (; ; )
        {
            int current = i.Value;
            int next = (current >= int.MaxValue ? 0 : current + 1);

            if (i.CompareAndSet(current, next))
            {
                return current;
            }
        }
    }

    public int GetAndDecrement()
    {
        for (; ; )
        {
            int current = i.Value;
            int next = (current <= 0 ? int.MaxValue : current - 1);

            if (i.CompareAndSet(current, next))
            {
                return current;
            }
        }
    }

    public int IncrementAndGet()
    {
        for (; ; )
        {
            int current = i.Value;
            int next = (current >= int.MaxValue ? 0 : current + 1);

            if (i.CompareAndSet(current, next))
            {
                return next;
            }
        }
    }

    public int DecrementAndGet()
    {
        for (; ; )
        {
            int current = i.Value;
            int next = (current <= 0 ? int.MaxValue : current - 1);
            
            if (i.CompareAndSet(current, next))
            {
                return next;
            }
        }
    }

    public int Get()
    {
        return i.Value;
    }

    public void Set(int newValue)
    {
        if (newValue < 0)
        {
            throw new ArgumentException("new value " + newValue + " < 0");
        }
        i.Value = newValue;
    }

    public int GetAndSet(int newValue)
    {
        if (newValue < 0)
        {
            throw new ArgumentException("new value " + newValue + " < 0");
        }
        return i.GetAndSet(newValue);
    }

    public int GetAndAdd(int delta)
    {
        if (delta < 0)
        {
            throw new ArgumentException("delta " + delta + " < 0");
        }
        for (; ; )
        {
            int current = i.Value;
            int next = (current >= int.MaxValue - delta + 1 ? delta - 1 : current + delta);

            if (i.CompareAndSet(current, next))
            {
                return current;
            }
            
        }
    }

    public int AddAndGet(int delta)
    {
        if (delta < 0)
        {
            throw new ArgumentException("delta " + delta + " < 0");
        }
        for (; ; )
        {
            int current = i.Value;
            int next = (current >= int.MaxValue - delta + 1 ? delta - 1 : current + delta);

            if (i.CompareAndSet(current, next))
            {
                return next;
            }
        }
    }

    public bool CompareAndSet(int expect, int update)
    {
        if (update < 0)
        {
            throw new ArgumentException("update value " + update + " < 0");
        }
        return i.CompareAndSet(expect, update);
    }

    public byte ByteValue()
    {
        return (byte)i.Value;
    }

    public short ShortValue()
    {
        return (short)i.Value;
    }

    public int IntValue()
    {
        return i.Value;
    }

    public long LongValue()
    {
        return i.Value;
    }

    public float FloatValue()
    {
        return i.Value;
    }

    public double DoubleValue()
    {
        return i.Value;
    }

    public override string? ToString()
    {
        return i.ToString();
    }

    public override int GetHashCode()
    {
        const int prime = 31;
        int result = 1;
        result = prime * result + i.GetHashCode();
        return result;
    }

    public override bool Equals(object? obj)
    {
        if (this == obj)
        {
            return true;
        }
        if (obj is not AtomicPositiveInteger other)
        {
            return false;
        }
        return i.Value == other.IntValue();
    }
}
