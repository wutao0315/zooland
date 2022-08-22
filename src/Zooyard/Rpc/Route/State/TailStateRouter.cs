﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State
{
    public class TailStateRouter<T> : IStateRouter<T>
    {
        private static readonly TailStateRouter<T> INSTANCE = new TailStateRouter<T>();
        private TailStateRouter()
        {

        }
        public static TailStateRouter<T> getInstance()
        {
            return INSTANCE;
        }



        public void SetNextRouter(IStateRouter<T> nextRouter)
        {

        }

        public URL Url => null;


        public BitList<IInvoker> Route(BitList<IInvoker> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder)
        {
            return invokers;
        }
        public bool Runtime => false;

        public bool Force => false;

        public void Notify(BitList<IInvoker> invokers)
        {

        }

        public string BuildSnapshot()
        {
            return "TailStateRouter End";
        }

       
    }
}