using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Zooyard.Rpc.LoadBalance;

public class ConsistentHashLoadBalance : AbstractLoadBalance
{
    public override string Name => NAME;
    public const string NAME = "hash";
    private readonly ConcurrentDictionary<string, ConsistentHashSelector> _selectors = new ();

    protected override URL DoSelect(IList<URL> urls, IInvocation invocation)
    {
        var key = urls[0].ServiceKey + "." + invocation.MethodInfo.Name;
        int identityHashCode = urls.GetHashCode();
        var selector = _selectors[key];
        if (selector == null || selector.GetHashCode() != identityHashCode)
        {
            _selectors.TryAdd(key, new ConsistentHashSelector(urls, invocation.MethodInfo.Name, identityHashCode));
            selector = _selectors[key];
        }
        return selector.Select(invocation);
    }
    private sealed class ConsistentHashSelector
    {
        public Regex COMMA_SPLIT_PATTERN = new ("\\s*[,]+\\s*",RegexOptions.Compiled);
        // 创建TreeMap 来保存结点
        private readonly IDictionary<long, URL> _virtualInvokers;
        // 副本数 
        private readonly int _replicaNumber;
        // 生成调用结点HashCode
        private readonly int _identityHashCode;
        // 参数索引数组
        private readonly int[] _argumentIndex;

        public ConsistentHashSelector(IList<URL> invokers, string methodName, int identityHashCode)
        {
            // 创建TreeMap 来保存结点  
            _virtualInvokers = new Dictionary<long, URL>();
            // 生成调用结点HashCode
            _identityHashCode = identityHashCode;
            // 获取Url
            // zooyard://169.254.90.37:20880/service.DemoService?anyhost=true&application=srcAnalysisClient&check=false&dubbo=2.8.4&generic=false&interface=service.DemoService&loadbalance=consistenthash&methods=sayHello,retMap&pid=14648&sayHello.timeout=20000&side=consumer&timestamp=1493522325563
            URL url = invokers[0];
            // 获取所配置的结点数，如没有设置则使用默认值160
            _replicaNumber = url.GetMethodParameter(methodName, "hash.nodes", 160);
            // 获取需要进行hash的参数数组索引，默认对第一个参数进行hash
            var index = COMMA_SPLIT_PATTERN.Split(url.GetMethodParameter<string>(methodName, "hash.arguments", "0"));
            _argumentIndex = new int[index.Length];
            for (int i = 0; i < index.Length; i++)
            {
                _argumentIndex[i] = int.Parse(index[i]);
            }
            // 创建虚拟结点
            // 对每个invoker生成replicaNumber个虚拟结点，并存放于TreeMap中
            foreach (var invoker in invokers)
            {
                var address = invoker.Address;
                for (int i = 0; i < _replicaNumber / 4; i++)
                {
                    // 根据md5算法为每4个结点生成一个消息摘要，摘要长为16字节128位。
                    byte[] digest = Md5(address + i);
                    // 随后将128位分为4部分，0-31,32-63,64-95,95-128，并生成4个32位数，存于long中，long的高32位都为0
                    // 并作为虚拟结点的key。
                    for (int h = 0; h < 4; h++)
                    {
                        long m = Hash(digest, h);
                        _virtualInvokers.Add(m, invoker);
                    }
                }
            }
        }

        //选择结点
        public URL Select(IInvocation invocation)
        {
            // 根据调用参数来生成Key
            var key = ToKey(invocation.Arguments);
            // 根据这个参数生成消息摘要
            byte[] digest = Md5(key);
            //调用hash(digest, 0)，将消息摘要转换为hashCode，这里仅取0-31位来生成HashCode
            //调用sekectForKey方法选择结点。
            return SelectForKey(Hash(digest, 0));
        }

        private string ToKey(object[] args)
        {
            var buf = new StringBuilder();
            // 由于hash.arguments没有进行配置，因为只取方法的第1个参数作为key
            foreach (var i in _argumentIndex)
            {
                if (i >= 0 && i < args.Length)
                {
                    buf.Append(args[i]);
                }
            }
            return buf.ToString();
        }

        //根据hashCode选择结点
        private URL SelectForKey(long hash)
        {
            URL invoker;
            long key = hash;
            // 若HashCode直接与某个虚拟结点的key一样，则直接返回该结点
            if (!_virtualInvokers.ContainsKey(key))
            {
                // 若不一致，找到一个最小上届的key所对应的结点。
                var tailMap = _virtualInvokers.Where(w=>w.Key>=key)?.OrderBy(o=>o.Key);
                // 若存在则返回，例如hashCode落在图中[1]的位置
                // 若不存在，例如hashCode落在[2]的位置，那么选择treeMap中第一个结点
                // 使用TreeMap的firstKey方法，来选择最小上界。
                if (tailMap == null || tailMap.Count() <= 0)
                {
                    key = _virtualInvokers.First().Key;
                }
                else {
                    key = tailMap.First().Key;
                }
            }
            invoker = _virtualInvokers[key];
            return invoker;
        }

        private long Hash(byte[] digest, int number)
        {
            #pragma warning disable CS0675
            return (((long)(digest[3 + number * 4] & 0xFF) << 24)
                    | ((long)(digest[2 + number * 4] & 0xFF) << 16)
                    | ((long)(digest[1 + number * 4] & 0xFF) << 8)
                    | (digest[number * 4] & 0xFF))
                    & 0xFFFFFFFFL;
        }
        private byte[] Md5(string value)
        {
           var provider = MD5.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            bytes = provider.ComputeHash(bytes);
            return bytes;
        }
    }
}
