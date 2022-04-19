using System.Diagnostics;

namespace Zooyard.Rpc.Cache.Support;

public class LruCacheData<TKey, TValue> where TValue : class
{
    private readonly Dictionary<TKey, NodeInfo> cachedNodesDictionary = new ();
    private readonly LinkedList<NodeInfo> lruLinkedList = new ();

    private readonly int maxSize;
    private readonly TimeSpan timeOut;

    private static readonly ReaderWriterLockSlim rwl = new ();

    private readonly Timer _cleanupTimer;

    public LruCacheData(int itemExpiryTimeout, int maxCacheSize = 100, int memoryRefreshInterval = 1000)
    {
        this.timeOut = TimeSpan.FromMilliseconds(itemExpiryTimeout) ;
        this.maxSize = maxCacheSize;
        var autoEvent = new AutoResetEvent(false);
        TimerCallback tcb = this.RemoveExpiredElements;
        _cleanupTimer = new Timer(tcb, autoEvent, 0, memoryRefreshInterval);
    }

    public void AddObject(TKey key, TValue cacheObject)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        Trace.WriteLine(string.Format("Adding a cache object with key: {0}", key.ToString()));
        rwl.EnterWriteLock();
        try
        {
            NodeInfo node;
            if (this.cachedNodesDictionary.TryGetValue(key, out node))
            {
                this.Delete(node);
            }

            this.ShrinkToSize(this.maxSize - 1);
            this.CreateNodeandAddtoList(key, cacheObject);
        }
        finally
        {
            rwl.ExitWriteLock();
        }
    }

    public TValue GetObject(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TValue data = null;
        NodeInfo node;
        rwl.EnterReadLock();
        try
        {
            if (this.cachedNodesDictionary.TryGetValue(key, out node))
            {
                if (node != null && !node.IsExpired())
                {
                    Trace.WriteLine(string.Format("Cache hit for key: {0}", key.ToString()));
                    node.AccessCount++;
                    data = node.Value;

                    if (node.AccessCount > 20)
                    {
                        ThreadPool.QueueUserWorkItem(this.AddBeforeFirstNode, key);
                    }
                }
            }
            else
            {
                Trace.WriteLine(string.Format("Cache miss for key: {0}", key.ToString()));
            }

            return data;
        }
        finally
        {
            rwl.ExitReadLock();
        }
    }

    public void Clear()
    {
        rwl.EnterWriteLock();
        try
        {
            while (this.lruLinkedList.Last != null)
            {
                var node = this.lruLinkedList.Last.Value;
                if (node != null)
                {
                    this.Delete(node);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            rwl.ExitWriteLock();
        }
    }

    private void RemoveExpiredElements(object stateInfo)
    {
        rwl.EnterWriteLock();
        try
        {
            while (this.lruLinkedList.Last != null)
            {
                var node = this.lruLinkedList.Last.Value;
                if (node != null && node.IsExpired())
                {
                    this.Delete(node);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            rwl.ExitWriteLock();
        }
    }

    private void CreateNodeandAddtoList(TKey userKey, TValue cacheObject)
    {
        var node = new NodeInfo(userKey, cacheObject, (this.timeOut > DateTime.MaxValue.Subtract(DateTime.UtcNow) ? DateTime.MaxValue : DateTime.UtcNow.Add(this.timeOut)));

        node.LLNode = this.lruLinkedList.AddFirst(node);
        this.cachedNodesDictionary[userKey] = node;
    }

    private void AddBeforeFirstNode(object stateinfo)
    {
        rwl.EnterWriteLock();
        try
        {
            var key = (TKey)stateinfo;
            NodeInfo nodeInfo;
            if (this.cachedNodesDictionary.TryGetValue(key, out nodeInfo))
            {
                if (nodeInfo != null && !nodeInfo.IsExpired() && nodeInfo.AccessCount > 20)
                {
                    if (nodeInfo.LLNode != this.lruLinkedList.First)
                    {
                        this.lruLinkedList.Remove(nodeInfo.LLNode);
                        nodeInfo.LLNode = this.lruLinkedList.AddBefore(this.lruLinkedList.First, nodeInfo);
                        nodeInfo.AccessCount = 0;
                    }
                }
            }
        }
        finally
        {
            rwl.ExitWriteLock();
        }
    }

    private void ShrinkToSize(int desiredSize)
    {
        while (this.cachedNodesDictionary.Count > desiredSize)
        {
            this.RemoveLeastValuableNode();
        }
    }

    private void RemoveLeastValuableNode()
    {
        if (this.lruLinkedList.Last != null)
        {
            var node = this.lruLinkedList.Last.Value;
            this.Delete(node);
        }
    }

    private void Delete(NodeInfo node)
    {
        Trace.WriteLine(string.Format("Evicting object from cache for key: {0}", node.Key.ToString()));
        this.lruLinkedList.Remove(node.LLNode);
        this.cachedNodesDictionary.Remove(node.Key);
    }

    ////This class represents data stored in the LinkedList Node and Dictionary
    private class NodeInfo
    {
        private readonly DateTime timeOutTime;

        internal NodeInfo(TKey key, TValue value, DateTime timeouttime)
        {
            this.Key = key;
            this.Value = value;
            this.timeOutTime = timeouttime;
        }

        internal TKey Key { get; private set; }

        internal TValue Value { get; private set; }

        internal int AccessCount { get; set; }

        internal LinkedListNode<NodeInfo> LLNode { get; set; }

        internal bool IsExpired()
        {
            return DateTime.UtcNow >= this.timeOutTime;
        }
    }
}
