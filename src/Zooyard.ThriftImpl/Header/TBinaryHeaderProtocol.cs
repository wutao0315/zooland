﻿using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Thrift.Transport.Client;
using Zooyard.Rpc;

namespace Zooyard.ThriftImpl.Header;

public class TBinaryHeaderProtocol : TBinaryProtocol
{
    public const string ActivityName = "Zooyard.ThriftImpl.TBinaryHeaderProtocol.RequestOut";
    private Activity? activity;
    private bool isNewActivity = false;
    private IDictionary<string, string> HEAD_INFO;

    public TBinaryHeaderProtocol(TTransport transport) : base(transport)
    {
        HEAD_INFO = new Dictionary<string, string>();
    }

    public override async Task WriteMessageBeginAsync(TMessage message, CancellationToken cancellationToken)
    {
        var attachments = RpcContext.GetContext().Attachments;
        foreach (var item in attachments)
        {
            HEAD_INFO.Add(item.Key, item.Value.ToString()!);
        }

        //trace start
        activity = Activity.Current;
        if (activity == null)
        {
            activity = new Activity(ActivityName);
            activity.Start();
            isNewActivity = true;
        }

        activity?.SetTag("rpc.thrift", "start");
        HEAD_INFO.Add("rpc.thrift", "start");

        string methodName = message.Name;
        activity?.SetTag("rpc.thrift.method", methodName);
        HEAD_INFO.Add("rpc.thrift.method", methodName);
        TTransport transport = this.Transport;


        if (transport is TSocketTransport socket)
        {
            string hostAddress = socket.Host.MapToIPv4().ToString();
            activity?.SetTag("rpc.thrift.server", hostAddress);
            HEAD_INFO.Add("rpc.thrift.server", hostAddress);
        }

        this.InjectHeaders(activity!, HEAD_INFO);

        await base.WriteMessageBeginAsync(message, cancellationToken);

        //write trace header to field0
        await WriteFieldZero(cancellationToken);
    }

    public async Task WriteFieldZero(CancellationToken cancellationToken)
    {
        TField TRACE_HEAD = new TField("traceHeader", TType.Map, 0);
        await this.WriteFieldBeginAsync(TRACE_HEAD, cancellationToken);
        {
            IDictionary<string, string> traceInfo = GenTraceInfo();
            await this.WriteMapBeginAsync(new TMap(TType.String, TType.String, traceInfo.Count), cancellationToken);
            foreach (var entry in traceInfo) {
                await this.WriteStringAsync(entry.Key, cancellationToken);
                await this.WriteStringAsync(entry.Value, cancellationToken);
            }
            await this.WriteMapEndAsync(cancellationToken);
        }
        await this.WriteFieldEndAsync(cancellationToken);
    }

    private IDictionary<string, string> GenTraceInfo()
    {
        //gen trace info
        return HEAD_INFO;
    }

    public override async ValueTask<TMessage> ReadMessageBeginAsync(CancellationToken cancellationToken)
    {
        TMessage tMessage = await base.ReadMessageBeginAsync(cancellationToken);
        if (tMessage.Type == TMessageType.Exception)
        {
            TApplicationException x = await TApplicationException.ReadAsync(this, cancellationToken);

            activity!.SetTag("rpc.thrift.exception", x.StackTrace);

            if (isNewActivity)
            {
                activity!.Stop();
            }
            //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_EXCEPTION, StringUtil.trimNewlineSymbolAndRemoveExtraSpace(x.getMessage()));
            //TraceUtils.endAndSendLocalTracer();
        }
        else if (tMessage.Type == TMessageType.Reply)
        {
            //TraceUtils.endAndSendLocalTracer();
            activity!.Stop();
        }
        return tMessage;
    }
}
