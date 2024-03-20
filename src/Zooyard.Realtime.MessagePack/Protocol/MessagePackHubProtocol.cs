using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Zooyard.Realtime;
using Zooyard.Realtime.Protocol;

namespace Zooyard.Protocols.MessagePack.Protocol;

/// <summary>
/// Implements the SignalR Hub Protocol using MessagePack.
/// </summary>
public class MessagePackHubProtocol : IRpcProtocol
{
    private const string ProtocolName = "messagepack";
    private const int ProtocolVersion = 2;
    private readonly DefaultMessagePackHubProtocolWorker _worker;

    /// <inheritdoc />
    public string Name => ProtocolName;

    /// <inheritdoc />
    public int Version => ProtocolVersion;

    /// <inheritdoc />
    public TransferFormat TransferFormat => TransferFormat.Binary;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    public MessagePackHubProtocol()
        : this(Options.Create(new MessagePackHubProtocolOptions()))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    /// <param name="options">The options used to initialize the protocol.</param>
    public MessagePackHubProtocol(IOptions<MessagePackHubProtocolOptions> options)
    {
        if (options == null) 
        {
            throw new ArgumentNullException($"{nameof(options)} is null");
        }

        _worker = new DefaultMessagePackHubProtocolWorker(options.Value.SerializerOptions);
    }

    /// <inheritdoc />
    public bool IsVersionSupported(int version)
    {
        return version <= Version;
    }

    /// <inheritdoc />
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out RpcMessage? message)
        => _worker.TryParseMessage(ref input, binder, out message);

    /// <inheritdoc />
    public void WriteMessage(RpcMessage message, IBufferWriter<byte> output)
        => _worker.WriteMessage(message, output);

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(RpcMessage message)
        => _worker.GetMessageBytes(message);

    internal static MessagePackSerializerOptions CreateDefaultMessagePackSerializerOptions() =>
        MessagePackSerializerOptions
            .Standard
            .WithResolver(SignalRResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);

    internal sealed class SignalRResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new SignalRResolver();

        public static readonly IReadOnlyList<IFormatterResolver> Resolvers = new IFormatterResolver[]
        {
                DynamicEnumAsStringResolver.Instance,
                ContractlessStandardResolver.Instance,
        };

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter = ResolveFormatter();

            private static IMessagePackFormatter<T>? ResolveFormatter()
            {
                foreach (var resolver in Resolvers)
                {
                    var formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        return formatter;
                    }
                }

                return null;
            }
        }
    }
}
