using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Zooyard;

namespace Zooyard.Rpc.LoadBalance
{
    public class ConsistentHashLoadBalance : AbstractLoadBalance
    {
        public override string Name => NAME;
        public const string NAME = "hash";
        private readonly ConcurrentDictionary<string, ConsistentHashSelector> _selectors = new ConcurrentDictionary<String, ConsistentHashSelector>();

        protected override URL DoSelect(IList<URL> urls, IInvocation invocation)
        {
            var key = urls[0].ServiceKey + "." + invocation.MethodInfo.Name;
            int identityHashCode = urls.GetHashCode();// System.identityHashCode(invokers);
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
            public Regex COMMA_SPLIT_PATTERN = new Regex("\\s*[,]+\\s*",RegexOptions.Compiled);
            private readonly IDictionary<long, URL> virtualInvokers;
            private readonly int replicaNumber;

            private readonly int _identityHashCode;

            private readonly int[] argumentIndex;

            public ConsistentHashSelector(IList<URL> invokers, string methodName, int identityHashCode)
            {
               
                this.virtualInvokers = new Dictionary<long, URL>();
                _identityHashCode = identityHashCode;
                URL url = invokers[0];
                this.replicaNumber = url.GetMethodParameter(methodName, "hash.nodes", 160);
                var index = COMMA_SPLIT_PATTERN.Split(url.GetMethodParameter<string>(methodName, "hash.arguments", "0"));
                argumentIndex = new int[index.Length];
                for (int i = 0; i < index.Length; i++)
                {
                    argumentIndex[i] = int.Parse(index[i]);
                }
                foreach (var invoker in invokers)
                {
                    var address = invoker.Address;
                    for (int i = 0; i < replicaNumber / 4; i++)
                    {
                        byte[] digest = Md5(address + i);
                        for (int h = 0; h < 4; h++)
                        {
                            long m = Hash(digest, h);
                            virtualInvokers.Add(m, invoker);
                        }
                    }
                }
            }

            public URL Select(IInvocation invocation)
            {
                var key = ToKey(invocation.Arguments);
                byte[] digest = Md5(key);
                return SelectForKey(Hash(digest, 0));
            }

            private string ToKey(object[] args)
            {
                var buf = new StringBuilder();
                foreach (var i in argumentIndex)
                {
                    if (i >= 0 && i < args.Length)
                    {
                        buf.Append(args[i]);
                    }
                }
                return buf.ToString();
            }

            private URL SelectForKey(long hash)
            {
                URL invoker;
                long key = hash;
                if (!virtualInvokers.ContainsKey(key))
                {
                    var tailMap= virtualInvokers.Where(w=>w.Key>=key)?.OrderBy(o=>o.Key);
                    if (tailMap == null || tailMap.Count() <= 0)
                    {
                        key = virtualInvokers.First().Key;
                    }
                    else {
                        key = tailMap.First().Key;
                    }
                }
                invoker = virtualInvokers[key];
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
                MD5CryptoServiceProvider provider;
                provider = new MD5CryptoServiceProvider();
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                bytes = provider.ComputeHash(bytes);
                return bytes;
            }
        }
    }
}
