using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Zooyard.Rpc.Cache.Support;

public class LruCacheData<TKey, TValue> 
    where TValue : class
    where TKey : notnull
{
    private readonly Dictionary<TKey, NodeInfo> cachedNodesDictionary = new ();
    private readonly LinkedList<NodeInfo> lruLinkedList = new ();
    private static readonly ReaderWriterLockSlim rwl = new();
    private readonly Timer _cleanupTimer;

    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    private int _maxSize => _zooyard.CurrentValue.Meta.GetValue("cache.size", 1000);
    private TimeSpan _timeOut=> TimeSpan.FromMilliseconds(_zooyard.CurrentValue.Meta.GetValue("cache.timeout", 60000));

    

 
    public LruCacheData(IOptionsMonitor<ZooyardOption> zooyard)
    {
        //int itemExpiryTimeout, int maxCacheSize = 100, int memoryRefreshInterval = 1000
        _zooyard = zooyard;
        
        var memoryRefreshInterval = zooyard.CurrentValue.Meta.GetValue("cache.interval", 1000);

        var autoEvent = new AutoResetEvent(false);
        TimerCallback tcb = this.RemoveExpiredElements;
        _cleanupTimer = new Timer(tcb, autoEvent, 0, memoryRefreshInterval);
    }

    public void AddObject(TKey key, TValue cacheObject)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Trace.WriteLine(string.Format("Adding a cache object with key: {0}", key.ToString()));
        rwl.EnterWriteLock();
        try
        {
            if (this.cachedNodesDictionary.TryGetValue(key, out NodeInfo? node))
            {
                this.Delete(node);
            }

            this.ShrinkToSize(this._maxSize - 1);
            this.CreateNodeandAddtoList(key, cacheObject);
        }
        finally
        {
            rwl.ExitWriteLock();
        }
    }

    public TValue? GetObject(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        TValue? data = null;
        rwl.EnterReadLock();
        try
        {
            if (this.cachedNodesDictionary.TryGetValue(key, out NodeInfo? node))
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

    private void RemoveExpiredElements(object? stateInfo)
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
        var node = new NodeInfo(userKey, 
            cacheObject, 
            (this._timeOut > DateTime.MaxValue.Subtract(DateTime.UtcNow) ? DateTime.MaxValue : DateTime.UtcNow.Add(this._timeOut))
            );

        node.LLNode = this.lruLinkedList.AddFirst(node);
        this.cachedNodesDictionary[userKey] = node;
    }

    private void AddBeforeFirstNode(object? stateinfo)
    {
        if (stateinfo == null) { return; }
        rwl.EnterWriteLock();
        try
        {
            var key = (TKey)stateinfo;
            if (this.cachedNodesDictionary.TryGetValue(key, out NodeInfo? nodeInfo))
            {
                if (nodeInfo != null && !nodeInfo.IsExpired() && nodeInfo.AccessCount > 20)
                {
                    if (nodeInfo.LLNode != this.lruLinkedList.First)
                    {
                        this.lruLinkedList.Remove(nodeInfo.LLNode!);
                        nodeInfo.LLNode = this.lruLinkedList.AddBefore(this.lruLinkedList.First!, nodeInfo);
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
        Trace.WriteLine($"Evicting object from cache for key: {node.Key}");
        this.lruLinkedList.Remove(node.LLNode!);
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

        internal LinkedListNode<NodeInfo>? LLNode { get; set; }

        internal bool IsExpired()
        {
            return DateTime.UtcNow >= this.timeOutTime;
        }
    }
}
