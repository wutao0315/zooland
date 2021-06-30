﻿using System.Reflection;

namespace Zooyard.Core.DynamicProxy
{
    internal class ProxyMethodResolverContext
    {
        public PackedArgs Packed { get; }
        public MethodInfo Method { get; }

        public ProxyMethodResolverContext(PackedArgs packed, MethodInfo method)
        {
            Packed = packed;
            Method = method;
        }
    }
}