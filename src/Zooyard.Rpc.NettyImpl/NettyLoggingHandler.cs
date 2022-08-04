using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net;
using System.Text;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// INTERNAL API
/// 
/// Used for adding additional debug logging to the DotNetty transport
/// </summary>
public class NettyLoggingHandler : ChannelHandlerAdapter
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyLoggingHandler));

    public override void ChannelRegistered(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} registered");
        ctx.FireChannelRegistered();
    }

    public override void ChannelUnregistered(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} unregistered");
        ctx.FireChannelUnregistered();
    }

    public override void ChannelActive(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} active");
        ctx.FireChannelActive();
    }

    public override void ChannelInactive(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} inactive");
        ctx.FireChannelInactive();
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
    {
        Logger().LogError(cause, $"Channel {ctx.Channel} caught exception");
        ctx.FireExceptionCaught(cause);
    }

    public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
    {
        Logger().LogDebug($"Channel {ctx.Channel} triggered user event [{evt}]");
        ctx.FireUserEventTriggered(evt);
    }

    public override async Task BindAsync(IChannelHandlerContext ctx, EndPoint localAddress)
    {
        Logger().LogInformation($"Channel {ctx.Channel} bind to address {localAddress}");
        await ctx.BindAsync(localAddress);
    }

    public override async Task ConnectAsync(IChannelHandlerContext ctx, EndPoint remoteAddress, EndPoint localAddress)
    {
        Logger().LogInformation($"Channel {ctx.Channel} connect (remote: {remoteAddress}, local: {localAddress})");
        await ctx.ConnectAsync(remoteAddress, localAddress);
    }

    public override async Task DisconnectAsync(IChannelHandlerContext ctx)
    {
        Logger().LogInformation($"Channel {ctx.Channel} disconnect");
        await ctx.DisconnectAsync();
    }

    public override async Task CloseAsync(IChannelHandlerContext ctx)
    {
        Logger().LogInformation($"Channel {ctx.Channel} close");
        await ctx.CloseAsync();
    }

    public override async Task DeregisterAsync(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} deregister");
        await ctx.DeregisterAsync();
    }

    public override void ChannelRead(IChannelHandlerContext ctx, object message)
    {
        Logger().LogDebug($"Channel {ctx.Channel} received a message ({message}) of type [{(message == null ? "NULL" : message.GetType().TypeQualifiedName())}]");
        ctx.FireChannelRead(message);
    }

    public override async Task WriteAsync(IChannelHandlerContext ctx, object message)
    {
        Logger().LogDebug($"Channel {ctx.Channel} writing a message ({message}) of type [{(message == null ? "NULL" : message.GetType().TypeQualifiedName())}]");
        await ctx.WriteAsync(message);
    }

    public override void Flush(IChannelHandlerContext ctx)
    {
        Logger().LogDebug($"Channel {ctx.Channel} flushing");
        ctx.Flush();
    }

    protected string Format(IChannelHandlerContext ctx, string eventName)
    {
        string chStr = ctx.Channel.ToString();
        return new StringBuilder(chStr.Length + 1 + eventName.Length)
            .Append(chStr)
            .Append(' ')
            .Append(eventName)
            .ToString();
    }

    protected string Format(IChannelHandlerContext ctx, string eventName, object arg)
    {
        if (arg is IByteBuffer buffer)
        {
            return this.FormatByteBuffer(ctx, eventName, buffer);
        }
        else if (arg is IByteBufferHolder holder)
        {
            return this.FormatByteBufferHolder(ctx, eventName, holder);
        }
        else
        {
            return this.FormatSimple(ctx, eventName, arg);
        }
    }

    protected string Format(IChannelHandlerContext ctx, string eventName, object firstArg, object secondArg)
    {
        if (secondArg == null)
        {
            return this.FormatSimple(ctx, eventName, firstArg);
        }
        string chStr = ctx.Channel.ToString();
        string arg1Str = firstArg.ToString();
        string arg2Str = secondArg.ToString();

        var buf = new StringBuilder(
            chStr.Length + 1 + eventName.Length + 2 + arg1Str.Length + 2 + arg2Str.Length);
        buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
            .Append(arg1Str).Append(", ").Append(arg2Str);
        return buf.ToString();
    }

    string FormatByteBuffer(IChannelHandlerContext ctx, string eventName, IByteBuffer msg)
    {
        string chStr = ctx.Channel.ToString();
        int length = msg.ReadableBytes;
        if (length == 0)
        {
            var buf = new StringBuilder(chStr.Length + 1 + eventName.Length + 4);
            buf.Append(chStr).Append(' ').Append(eventName).Append(": 0B");
            return buf.ToString();
        }
        else
        {
            int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
            var buf = new StringBuilder(chStr.Length + 1 + eventName.Length + 2 + 10 + 1 + 2 + rows * 80);

            buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(length).Append('B').Append('\n');
            ByteBufferUtil.AppendPrettyHexDump(buf, msg);

            return buf.ToString();
        }
    }

    string FormatByteBufferHolder(IChannelHandlerContext ctx, string eventName, IByteBufferHolder msg)
    {
        string chStr = ctx.Channel.ToString();
        string msgStr = msg.ToString();
        IByteBuffer content = msg.Content;
        int length = content.ReadableBytes;
        if (length == 0)
        {
            var buf = new StringBuilder(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 4);
            buf.Append(chStr).Append(' ').Append(eventName).Append(", ").Append(msgStr).Append(", 0B");
            return buf.ToString();
        }
        else
        {
            int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
            var buf = new StringBuilder(
                chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 2 + 10 + 1 + 2 + rows * 80);

            buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
                .Append(msgStr).Append(", ").Append(length).Append('B').Append('\n');
            ByteBufferUtil.AppendPrettyHexDump(buf, content);

            return buf.ToString();
        }
    }

    string FormatSimple(IChannelHandlerContext ctx, string eventName, object msg)
    {
        string chStr = ctx.Channel.ToString();
        string msgStr = msg.ToString();
        var buf = new StringBuilder(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length);
        return buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(msgStr).ToString();
    }
}
