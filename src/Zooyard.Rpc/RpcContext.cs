using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Utils;

namespace Zooyard.Rpc
{
    /// <summary>
    /// Thread local context. (API, ThreadLocal, ThreadSafe)
    /// 
    /// 注意：RpcContext是一个临时状态记录器，当接收到RPC请求，或发起RPC请求时，RpcContext的状态都会变化。
    /// 比如：A调B，B再调C，则B机器上，在B调C之前，RpcContext记录的是A调B的信息，在B调C之后，RpcContext记录的是B调C的信息。
    /// </summary>
    public class RpcContext
    {
        public const string ASYNC_KEY = "async";
        public const string RETURN_KEY = "return";
        //private static readonly ThreadLocal<RpcContext> LOCAL = new ThreadLocal<RpcContext>(()=> new RpcContext());
        private static readonly AsyncLocal<RpcContext> LOCAL = new AsyncLocal<RpcContext>();
        
        //private Task future;

        private IList<URL> urls;

        private DnsEndPoint localAddress;

        private DnsEndPoint remoteAddress;

        private readonly IDictionary<string, string> attachments = new Dictionary<string, string>();

        private readonly IDictionary<string, object> values = new Dictionary<string, object>();

        protected internal RpcContext()
        {
        }

        public static RpcContext GetContext()
        {
            LOCAL.Value ??= new RpcContext();
            return LOCAL.Value;
        }

        /// <summary>
        /// is provider side.
        /// </summary>
        /// <returns> provider side. </returns>
        public virtual bool ProviderSide
        {
            get
            {
                URL url = Url;
                if (url == null)
                {
                    return false;
                }
                DnsEndPoint address = RemoteAddress;
                if (address == null)
                {
                    return false;
                }
                string host = address.Host;
                return url.Port != address.Port || !NetUtils.FilterLocalHost(url.Ip).Equals(NetUtils.FilterLocalHost(host));
            }
        }

        /// <summary>
        /// is consumer side.
        /// </summary>
        /// <returns> consumer side. </returns>
        public virtual bool ConsumerSide
        {
            get
            {
                URL url = Url;
                if (url == null)
                {
                    return false;
                }
                DnsEndPoint address = RemoteAddress;
                if (address == null)
                {
                    return false;
                }
                string host = address.Host;
                return url.Port == address.Port && NetUtils.FilterLocalHost(url.Ip).Equals(NetUtils.FilterLocalHost(host));
            }
        }

        /// <summary>
        /// get future.
        /// </summary>
        /// @param <T> </param>
        /// <returns> future </returns>
        public virtual Task Future { get; set; }


        public virtual IList<URL> Urls
        {
            get
            {
                return urls == null && Url != null ? new List<URL>() { Url } : urls;
            }
            set
            {
                this.urls = value;
            }
        }


        public virtual URL Url { get; set; }


        /// <summary>
        /// get method name.
        /// </summary>
        /// <returns> method name. </returns>
        public virtual string MethodName { get; set; }


        /// <summary>
        /// get parameter types.
        /// 
        /// @serial
        /// </summary>
        public virtual Type[] ParameterTypes { get; set; }


        /// <summary>
        /// get arguments.
        /// </summary>
        /// <returns> arguments. </returns>
        public virtual object[] Arguments { get; set; }


        /// <summary>
        /// set local address.
        /// </summary>
        /// <param name="address"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetLocalAddress(DnsEndPoint address)
        {
            this.localAddress = address;
            return this;
        }

        /// <summary>
        /// set local address.
        /// </summary>
        /// <param name="host"> </param>
        /// <param name="port"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetLocalAddress(string host, int port)
        {
            if (port < 0)
            {
                port = 0;
            }
            this.localAddress = new DnsEndPoint(host, port);
            return this;
        }

        /// <summary>
        /// get local address.
        /// </summary>
        /// <returns> local address </returns>
        public virtual DnsEndPoint LocalAddress
        {
            get
            {
                return localAddress;
            }
        }

        public virtual string LocalAddressString
        {
            get
            {
                return LocalHost + ":" + LocalPort;
            }
        }

        /// <summary>
        /// get local host name.
        /// </summary>
        /// <returns> local host name </returns>
        public virtual string LocalHostName
        {
            get
            {
                var host = localAddress == null ? null : localAddress.Host;
                if (host == null || host.Length == 0)
                {
                    return LocalHost;
                }
                return host;
            }
        }

        /// <summary>
        /// set remote address.
        /// </summary>
        /// <param name="address"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetRemoteAddress(DnsEndPoint address)
        {
            this.remoteAddress = address;
            return this;
        }

        /// <summary>
        /// set remote address.
        /// </summary>
        /// <param name="host"> </param>
        /// <param name="port"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetRemoteAddress(string host, int port)
        {
            if (port < 0)
            {
                port = 0;
            }
            this.remoteAddress = new DnsEndPoint(host, port);
            return this;
        }

        /// <summary>
        /// get remote address.
        /// </summary>
        /// <returns> remote address </returns>
        public virtual DnsEndPoint RemoteAddress
        {
            get
            {
                return remoteAddress;
            }
            set
            {
                remoteAddress = value;
            }
        }

        /// <summary>
        /// get remote address string.
        /// </summary>
        /// <returns> remote address string. </returns>
        public virtual string RemoteAddressString
        {
            get
            {
                return RemoteHost + ":" + RemotePort;
            }
        }

        /// <summary>
        /// get remote host name.
        /// </summary>
        /// <returns> remote host name </returns>
        public virtual string RemoteHostName
        {
            get
            {
                return remoteAddress?.Host;
            }
        }

        /// <summary>
        /// get local host.
        /// </summary>
        /// <returns> local host </returns>
        public virtual string LocalHost
        {
            get
            {
                string host = localAddress == null ? null : NetUtils.FilterLocalHost(localAddress.Host);
                if (host == null || host.Length == 0)
                {
                    return NetUtils.LocalHost;
                }
                return host;
            }
        }

        /// <summary>
        /// get local port.
        /// </summary>
        /// <returns> port </returns>
        public virtual int LocalPort
        {
            get
            {
                return localAddress?.Port??0;
            }
        }

        /// <summary>
        /// get remote host.
        /// </summary>
        /// <returns> remote host </returns>
        public virtual string RemoteHost
        {
            get
            {
                return remoteAddress == null ? null : NetUtils.FilterLocalHost(remoteAddress.Host);
            }
        }

        /// <summary>
        /// get remote port.
        /// </summary>
        /// <returns> remote port </returns>
        public virtual int RemotePort
        {
            get
            {
                return remoteAddress?.Port??0;
            }
        }

        /// <summary>
        /// get attachment.
        /// </summary>
        /// <param name="key"> </param>
        /// <returns> attachment </returns>
        public virtual string GetAttachment(string key)
        {
            return attachments[key];
        }

        /// <summary>
        /// set attachment.
        /// </summary>
        /// <param name="key"> </param>
        /// <param name="value"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetAttachment(string key, string value)
        {
            if (value == null)
            {
                attachments.Remove(key);
            }
            else
            {
                attachments[key] = value;
            }
            return this;
        }

        /// <summary>
        /// remove attachment.
        /// </summary>
        /// <param name="key"> </param>
        /// <returns> context </returns>
        public virtual RpcContext RemoveAttachment(string key)
        {
            attachments.Remove(key);
            return this;
        }

        /// <summary>
        /// get attachments.
        /// </summary>
        /// <returns> attachments </returns>
        public virtual IDictionary<string, string> Attachments
        {
            get
            {
                return attachments;
            }
        }

        /// <summary>
        /// set attachments
        /// </summary>
        /// <param name="attachment"> </param>
        /// <returns> context </returns>
        public virtual RpcContext SetAttachments(IDictionary<string, string> attachment)
        {
            this.attachments.Clear();
            if (attachment != null && attachment.Count > 0)
            {
                this.attachments.PutAll(attachment);
            }
            return this;
        }

        public virtual void ClearAttachments()
        {
            this.attachments.Clear();
        }

        /// <summary>
        /// get values.
        /// </summary>
        /// <returns> values </returns>
        public virtual IDictionary<string, object> Get()
        {
            return values;
        }

        /// <summary>
        /// set value.
        /// </summary>
        /// <param name="key"> </param>
        /// <param name="value"> </param>
        /// <returns> context </returns>
        public virtual RpcContext Set(string key, object value)
        {
            if (value == null)
            {
                values.Remove(key);
            }
            else
            {
                values[key] = value;
            }
            return this;
        }

        /// <summary>
        /// remove value.
        /// </summary>
        /// <param name="key"> </param>
        /// <returns> value </returns>
        public virtual RpcContext Remove(string key)
        {
            values.Remove(key);
            return this;
        }

        /// <summary>
        /// get value.
        /// </summary>
        /// <param name="key"> </param>
        /// <returns> value </returns>
        public virtual object Get(string key)
        {
            return values[key];
        }

        public virtual RpcContext SetInvokers(IList<URL> invokers)
        {
            //this.invokers = invokers;
            if (invokers != null && invokers.Count > 0)
            {
                IList<URL> urls = new List<URL>(invokers.Count);
                foreach (var invoker in invokers)
                {
                    urls.Add(invoker);
                }
                Urls = urls;
            }
            return this;
        }

        public virtual RpcContext SetInvoker<T>(URL invoker)
        {
            //this.invoker = invoker;
            if (invoker != null)
            {
                Url = invoker;
            }
            return this;
        }

        public virtual RpcContext SetInvocation<T>(IInvocation invocation)
        {
            //this.invocation = invocation;
            if (invocation != null)
            {
                MethodName = invocation.MethodInfo.Name;
                ParameterTypes = (from item in invocation.Arguments select item.GetType()).ToArray();
                Arguments = invocation.Arguments;
            }
            return this;
        }

        /// <summary>
        /// 异步调用 ，需要返回值，即使步调用Future.get方法，也会处理调用超时问题.
        /// </summary>
        /// <param name="callable"> </param>
        /// <returns> 通过future.get()获取返回结果. </returns>
        public virtual async Task<T> AsyncCall<T>(Func<T> callable)
        {
            try
            {
                try
                {
                    SetAttachment(ASYNC_KEY, true.ToString());

                    var result = await Task.Run<T>(callable);
                    return result;
                }
                catch (Exception e)
                {
                    throw new RpcException(e);
                }
                finally
                {
                    RemoveAttachment(ASYNC_KEY);
                }
            }
            catch (RpcException e)
            {
                throw e;
            }
        }
        
        /// <summary>
        /// oneway调用，只发送请求，不接收返回结果.
        /// </summary>
        /// <param name="callable"> </param>
        public virtual void AsyncCall(Task runable)
        {
            try
            {
                SetAttachment(RETURN_KEY, false.ToString());
                runable.Start();
            }
            catch (Exception e)
            {
                //FIXME 异常是否应该放在future中？
                throw new RpcException("oneway call error ." + e.Message, e);
            }
            finally
            {
                RemoveAttachment(RETURN_KEY);
            }
        }
    }
}