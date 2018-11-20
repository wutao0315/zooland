using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Zooyard.Core;

namespace Zooyard.Rpc.LoadBalance
{
    public class ConsistentHashLoadBalance : AbstractLoadBalance
    {
        public override string Name => NAME;
        public const string NAME = "hash";
        private ConcurrentDictionary<string, ConsistentHashSelector> selectors = new ConcurrentDictionary<String, ConsistentHashSelector>();

        protected override URL doSelect(IList<URL> urls, IInvocation invocation)
        {
            var key = urls[0].ServiceKey + "." + invocation.MethodInfo.Name;
            int identityHashCode = urls.GetHashCode();// System.identityHashCode(invokers);
            var selector = selectors[key];
            if (selector == null || selector.GetHashCode() != identityHashCode)
            {
                selectors.TryAdd(key, new ConsistentHashSelector(urls, invocation.MethodInfo.Name, identityHashCode));
                selector = selectors[key];
            }
            return selector.select(invocation);
        }
        private sealed class ConsistentHashSelector
        {
            public Regex COMMA_SPLIT_PATTERN = new Regex("\\s*[,]+\\s*",RegexOptions.Compiled);
            private readonly IDictionary<long, URL> virtualInvokers;
            private readonly int replicaNumber;

            private readonly int identityHashCode;

            private readonly int[] argumentIndex;

            public ConsistentHashSelector(IList<URL> invokers, string methodName, int identityHashCode)
            {
               
                this.virtualInvokers = new Dictionary<long, URL>();
                this.identityHashCode = identityHashCode;
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
                        byte[] digest = md5(address + i);
                        for (int h = 0; h < 4; h++)
                        {
                            long m = hash(digest, h);
                            virtualInvokers.Add(m, invoker);
                        }
                    }
                }
            }

            public URL select(IInvocation invocation)
            {
                var key = toKey(invocation.Arguments);
                byte[] digest = md5(key);
                return selectForKey(hash(digest, 0));
            }

            private string toKey(object[] args)
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

            private URL selectForKey(long hash)
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
                    //SortedMap<Long, Invoker<T>> tailMap = virtualInvokers.tailMap(key);
                    //if (tailMap.isEmpty())
                    //{
                    //    key = virtualInvokers.firstKey();
                    //}
                    //else
                    //{
                    //    key = tailMap.firstKey();
                    //}
                }
                invoker = virtualInvokers[key];
                return invoker;
            }

            private long hash(byte[] digest, int number)
            {
                #pragma warning disable CS0675
                return (((long)(digest[3 + number * 4] & 0xFF) << 24)
                        | ((long)(digest[2 + number * 4] & 0xFF) << 16)
                        | ((long)(digest[1 + number * 4] & 0xFF) << 8)
                        | (digest[number * 4] & 0xFF))
                        & 0xFFFFFFFFL;
            }
            private byte[] md5(string value)
            {
                MD5CryptoServiceProvider provider;
                provider = new MD5CryptoServiceProvider();
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                bytes = provider.ComputeHash(bytes);
                return bytes;
            }
            //private byte[] md5(string value)
            //{
            //    MessageDigest md5;
            //    try
            //    {
            //        md5 = MessageDigest.getInstance("MD5");
            //    }
            //    catch (NoSuchAlgorithmException e)
            //    {
            //        throw new IllegalStateException(e.getMessage(), e);
            //    }
            //    md5.reset();
            //    byte[] bytes;
            //    try
            //    {
            //        bytes = value.getBytes("UTF-8");
            //    }
            //    catch (UnsupportedEncodingException e)
            //    {
            //        throw new IllegalStateException(e.getMessage(), e);
            //    }
            //    md5.update(bytes);
            //    return md5.digest();
            //}

        }
    }
}
