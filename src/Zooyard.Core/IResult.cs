using System;
using System.Collections.Generic;

namespace Zooyard.Core
{
    public interface IResult
    {
        object Value { get; }
        bool HasException { get; }
        Exception Exception { get; }
    }

    public class RpcResult : IResult
    {
        public object Value { get; private set; }

        public Exception Exception { get; private set; }

        public RpcResult()
        {
        }

        public RpcResult(object result)
        {
            this.Value = result;
        }

        public RpcResult(Exception exception)
        {
            this.Exception = exception;
        }

        public bool HasException => Exception != null;
    }

    public interface IClusterResult
    {
        IResult Result { get; }
        IList<URL> Urls { get; }
        IList<BadUrl> BadUrls { get; }
        Exception ClusterException { get; }
        bool IsThrow { get; }
    }

    public class ClusterResult : IClusterResult
    {
        public IResult Result { get; private set; }

        public IList<URL> Urls { get; private set; }

        public IList<BadUrl> BadUrls { get; private set; }

        public Exception ClusterException { get; private set; }
        public bool IsThrow { get; private set; }



        public ClusterResult(IResult result, IList<URL> urls, IList<BadUrl> badUrls, Exception clusterException, bool isThrow)
        {
            this.Result = result;
            this.Urls = urls;
            this.BadUrls = badUrls;
            this.ClusterException = clusterException;
            this.IsThrow = isThrow;
        }
    }

}
