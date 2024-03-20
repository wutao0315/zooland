using Microsoft.Extensions.Options;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zooyard.Realtime.Internal;
using Zooyard.Realtime.Protocol;

namespace Zooyard.Realtime.TextJson;

/// <summary>
/// Implements the SignalR Hub Protocol using System.Text.Json.
/// </summary>
public sealed class TextJsonProtocol : IRpcProtocol
{
    private const string ResultPropertyName = "result";
    private static readonly JsonEncodedText ResultPropertyNameBytes = JsonEncodedText.Encode(ResultPropertyName);
    private const string ItemPropertyName = "item";
    private static readonly JsonEncodedText ItemPropertyNameBytes = JsonEncodedText.Encode(ItemPropertyName);
    private const string InvocationIdPropertyName = "invocationId";
    private static readonly JsonEncodedText InvocationIdPropertyNameBytes = JsonEncodedText.Encode(InvocationIdPropertyName);
    private const string StreamIdsPropertyName = "streamIds";
    private static readonly JsonEncodedText StreamIdsPropertyNameBytes = JsonEncodedText.Encode(StreamIdsPropertyName);
    private const string TypePropertyName = "type";
    private static readonly JsonEncodedText TypePropertyNameBytes = JsonEncodedText.Encode(TypePropertyName);
    private const string ErrorPropertyName = "error";
    private static readonly JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);
    private const string AllowReconnectPropertyName = "allowReconnect";
    private static readonly JsonEncodedText AllowReconnectPropertyNameBytes = JsonEncodedText.Encode(AllowReconnectPropertyName);
    private const string TargetPropertyName = "target";
    private static readonly JsonEncodedText TargetPropertyNameBytes = JsonEncodedText.Encode(TargetPropertyName);
    private const string ArgumentsPropertyName = "arguments";
    private static readonly JsonEncodedText ArgumentsPropertyNameBytes = JsonEncodedText.Encode(ArgumentsPropertyName);
    private const string HeadersPropertyName = "headers";
    private static readonly JsonEncodedText HeadersPropertyNameBytes = JsonEncodedText.Encode(HeadersPropertyName);
    private const string SequenceIdPropertyName = "sequenceId";
    private static readonly JsonEncodedText SequenceIdPropertyNameBytes = JsonEncodedText.Encode(SequenceIdPropertyName);

    private const string ProtocolName = "json";
    private const int ProtocolVersion = 2;

    /// <summary>
    /// Gets the serializer used to serialize invocation arguments and return values.
    /// </summary>
    private readonly JsonSerializerOptions _payloadSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextJsonProtocol"/> class.
    /// </summary>
    public TextJsonProtocol() : this(Options.Create(new TextJsonProtocolOptions()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextJsonProtocol"/> class.
    /// </summary>
    /// <param name="options">The options used to initialize the protocol.</param>
    public TextJsonProtocol(IOptions<TextJsonProtocolOptions> options)
    {
        _payloadSerializerOptions = options.Value.PayloadSerializerOptions;
    }

    /// <inheritdoc />
    public string Name => ProtocolName;

    /// <inheritdoc />
    public int Version => ProtocolVersion;

    /// <inheritdoc />
    public TransferFormat TransferFormat => TransferFormat.Text;

    /// <inheritdoc />
    public bool IsVersionSupported(int version)
    {
        return version <= Version;
    }

    /// <inheritdoc />
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out RpcMessage? message)
    {
        if (!TextMessageParser.TryParseMessage(ref input, out var payload))
        {
            message = null;
            return false;
        }

        message = ParseMessage(payload, binder);

        return message != null;
    }

    /// <inheritdoc />
    public void WriteMessage(RpcMessage message, IBufferWriter<byte> output)
    {
        WriteMessageCore(message, output);
        TextMessageFormatter.WriteRecordSeparator(output);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(RpcMessage message)
    {
        return RpcProtocolExtensions.GetMessageBytes(this, message);
    }

    private RpcMessage? ParseMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
    {
        try
        {
            // We parse using the Utf8JsonReader directly but this has a problem. Some of our properties are dependent on other properties
            // and since reading the json might be unordered, we need to store the parsed content as JsonDocument to re-parse when true types are known.
            // if we're lucky and the state we need to directly parse is available, then we'll use it.

            int? type = null;
            string? invocationId = null;
            string? target = null;
            string? error = null;
            var hasItem = false;
            object? item = null;
            var hasResult = false;
            object? result = null;
            var hasArguments = false;
            object?[]? arguments = null;
            string[]? streamIds = null;
            bool hasArgumentsToken = false;
            Utf8JsonReader argumentsToken = default;
            bool hasItemsToken = false;
            Utf8JsonReader itemsToken = default;
            bool hasResultToken = false;
            Utf8JsonReader resultToken = default;
            ExceptionDispatchInfo? argumentBindingException = null;
            Dictionary<string, string>? headers = null;
            var completed = false;
            var allowReconnect = false;
            long? sequenceId = null;

            var reader = new Utf8JsonReader(input, isFinalBlock: true, state: default);

            reader.CheckRead();

            // We're always parsing a JSON object
            reader.EnsureObjectStart();

            do
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.ValueTextEquals(TypePropertyNameBytes.EncodedUtf8Bytes))
                        {
                            type = reader.ReadAsInt32(TypePropertyName);

                            if (type == null)
                            {
                                throw new InvalidDataException($"Expected '{TypePropertyName}' to be of type {JsonTokenType.Number}.");
                            }
                        }
                        else if (reader.ValueTextEquals(InvocationIdPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            invocationId = reader.ReadAsString(InvocationIdPropertyName);
                        }
                        else if (reader.ValueTextEquals(StreamIdsPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            reader.CheckRead();

                            if (reader.TokenType != JsonTokenType.StartArray)
                            {
                                throw new InvalidDataException(
                                    $"Expected '{StreamIdsPropertyName}' to be of type {SystemTextJsonExtensions.GetTokenString(JsonTokenType.StartArray)}.");
                            }

                            List<string>? newStreamIds = null;
                            reader.Read();
                            while (reader.TokenType != JsonTokenType.EndArray)
                            {
                                newStreamIds ??= new();
                                newStreamIds.Add(reader.GetString() ?? throw new InvalidDataException($"Null value for '{StreamIdsPropertyName}' is not valid."));
                                reader.Read();
                            }

                            streamIds = newStreamIds?.ToArray() ?? Array.Empty<string>();
                        }
                        else if (reader.ValueTextEquals(TargetPropertyNameBytes.EncodedUtf8Bytes))
                        {
#if NETCOREAPP
                            reader.Read();

                            if (reader.TokenType != JsonTokenType.String)
                            {
                                throw new InvalidDataException($"Expected '{TargetPropertyName}' to be of type {JsonTokenType.String}.");
                            }

                            if (!reader.HasValueSequence)
                            {
                                target = binder.GetTarget(reader.ValueSpan) ?? reader.GetString();
                            }
                            else
                            {
                                target = reader.GetString();
                            }
#else
                            target = reader.ReadAsString(TargetPropertyName);
#endif
                        }
                        else if (reader.ValueTextEquals(ErrorPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            error = reader.ReadAsString(ErrorPropertyName);
                        }
                        else if (reader.ValueTextEquals(AllowReconnectPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            allowReconnect = reader.ReadAsBoolean(AllowReconnectPropertyName);
                        }
                        else if (reader.ValueTextEquals(ResultPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            hasResult = true;

                            if (string.IsNullOrEmpty(invocationId))
                            {
                                // If we don't have an invocation id then we need to value copy the reader so we can parse it later
                                hasResultToken = true;
                                resultToken = reader;
                                reader.Skip();
                            }
                            else
                            {
                                // If we have an invocation id already we can parse the end result
                                var returnType = ProtocolHelper.TryGetReturnType(binder, invocationId);
                                if (returnType is null)
                                {
                                    reader.Skip();
                                    result = null;
                                }
                                else
                                {
                                    try
                                    {
                                        result = BindType(ref reader, input, returnType);
                                    }
                                    catch (Exception ex)
                                    {
                                        error = $"Error trying to deserialize result to {returnType.Name}. {ex.Message}";
                                        hasResult = false;
                                    }
                                }
                            }
                        }
                        else if (reader.ValueTextEquals(ItemPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            reader.CheckRead();

                            hasItem = true;

                            string? id = null;
                            if (!string.IsNullOrEmpty(invocationId))
                            {
                                id = invocationId;
                            }
                            else
                            {
                                // If we don't have an id yet then we need to value copy the reader so we can parse it later
                                hasItemsToken = true;
                                itemsToken = reader;
                                reader.Skip();
                                continue;
                            }

                            try
                            {
                                var itemType = binder.GetStreamItemType(id);
                                item = BindType(ref reader, itemType);
                            }
                            catch (Exception ex)
                            {
                                return new StreamBindingFailureMessage(id, ExceptionDispatchInfo.Capture(ex));
                            }
                        }
                        else if (reader.ValueTextEquals(ArgumentsPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            reader.CheckRead();

                            int initialDepth = reader.CurrentDepth;
                            if (reader.TokenType != JsonTokenType.StartArray)
                            {
                                throw new InvalidDataException($"Expected '{ArgumentsPropertyName}' to be of type {SystemTextJsonExtensions.GetTokenString(JsonTokenType.StartArray)}.");
                            }

                            hasArguments = true;

                            if (string.IsNullOrEmpty(target))
                            {
                                // We don't know the method name yet so just value copy the reader so we can parse it later
                                hasArgumentsToken = true;
                                argumentsToken = reader;
                                reader.Skip();
                            }
                            else
                            {
                                try
                                {
                                    var paramTypes = binder.GetParameterTypes(target);
                                    arguments = BindTypes(ref reader, paramTypes);
                                }
                                catch (Exception ex)
                                {
                                    argumentBindingException = ExceptionDispatchInfo.Capture(ex);

                                    // Could be at any point in argument array JSON when an error is thrown
                                    // Read until the end of the argument JSON array
                                    while (reader.CurrentDepth == initialDepth && reader.TokenType == JsonTokenType.StartArray ||
                                            reader.CurrentDepth > initialDepth)
                                    {
                                        reader.CheckRead();
                                    }
                                }
                            }
                        }
                        else if (reader.ValueTextEquals(HeadersPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            reader.CheckRead();
                            headers = ReadHeaders(ref reader);
                        }
                        else if (reader.ValueTextEquals(SequenceIdPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            sequenceId = reader.ReadAsInt64(SequenceIdPropertyName);
                        }
                        else
                        {
                            reader.CheckRead();
                            reader.Skip();
                        }
                        break;
                    case JsonTokenType.EndObject:
                        completed = true;
                        break;
                }
            }
            while (!completed && reader.CheckRead());

            RpcMessage message;

            switch (type)
            {
                case RpcProtocolConstants.InvocationMessageType:
                    {
                        if (target is null)
                        {
                            throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
                        }

                        if (hasArgumentsToken)
                        {
                            // We weren't able to bind the arguments because they came before the 'target', so try to bind now that we've read everything.
                            try
                            {
                                var paramTypes = binder.GetParameterTypes(target);
                                arguments = BindTypes(ref argumentsToken, paramTypes);
                            }
                            catch (Exception ex)
                            {
                                argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                            }
                        }

                        message = argumentBindingException != null
                            ? new InvocationBindingFailureMessage(invocationId, target, argumentBindingException)
                            : BindInvocationMessage(invocationId, target, arguments, hasArguments, streamIds);
                    }
                    break;
                case RpcProtocolConstants.StreamInvocationMessageType:
                    {
                        if (target is null)
                        {
                            throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
                        }

                        if (hasArgumentsToken)
                        {
                            // We weren't able to bind the arguments because they came before the 'target', so try to bind now that we've read everything.
                            try
                            {
                                var paramTypes = binder.GetParameterTypes(target);
                                arguments = BindTypes(ref argumentsToken, paramTypes);
                            }
                            catch (Exception ex)
                            {
                                argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                            }
                        }

                        message = argumentBindingException != null
                            ? new InvocationBindingFailureMessage(invocationId, target, argumentBindingException)
                            : BindStreamInvocationMessage(invocationId, target, arguments, hasArguments, streamIds);
                    }
                    break;
                case RpcProtocolConstants.StreamItemMessageType:
                    if (invocationId is null)
                    {
                        throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
                    }

                    if (hasItemsToken)
                    {
                        try
                        {
                            var returnType = binder.GetStreamItemType(invocationId);
                            item = BindType(ref itemsToken, returnType);
                        }
                        catch (JsonException ex)
                        {
                            message = new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
                            break;
                        }
                    }

                    message = BindStreamItemMessage(invocationId, item, hasItem);
                    break;
                case RpcProtocolConstants.CompletionMessageType:
                    if (invocationId is null)
                    {
                        throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
                    }

                    if (hasResultToken)
                    {
                        var returnType = ProtocolHelper.TryGetReturnType(binder, invocationId);
                        if (returnType is null)
                        {
                            result = null;
                        }
                        else
                        {
                            try
                            {
                                result = BindType(ref resultToken, input, returnType);
                            }
                            catch (Exception ex)
                            {
                                error = $"Error trying to deserialize result to {returnType.Name}. {ex.Message}";
                                hasResult = false;
                            }
                        }
                    }

                    message = BindCompletionMessage(invocationId, error, result, hasResult);
                    break;
                case RpcProtocolConstants.CancelInvocationMessageType:
                    message = BindCancelInvocationMessage(invocationId);
                    break;
                case RpcProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                case RpcProtocolConstants.CloseMessageType:
                    return BindCloseMessage(error, allowReconnect);
                case RpcProtocolConstants.AckMessageType:
                    return BindAckMessage(sequenceId);
                case RpcProtocolConstants.SequenceMessageType:
                    return BindSequenceMessage(sequenceId);
                case null:
                    throw new InvalidDataException($"Missing required property '{TypePropertyName}'.");
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    return null;
            }

            return ApplyHeaders(message, headers);
        }
        catch (JsonException jrex)
        {
            throw new InvalidDataException("Error reading JSON.", jrex);
        }
    }

    private static Dictionary<string, string> ReadHeaders(ref Utf8JsonReader reader)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidDataException($"Expected '{HeadersPropertyName}' to be of type {JsonTokenType.StartObject}.");
        }

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString()!;

                    reader.CheckRead();

                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new InvalidDataException($"Expected header '{propertyName}' to be of type {JsonTokenType.String}.");
                    }

                    headers[propertyName] = reader.GetString()!;
                    break;
                case JsonTokenType.Comment:
                    break;
                case JsonTokenType.EndObject:
                    return headers;
            }
        }

        throw new InvalidDataException("Unexpected end when reading message headers");
    }

    private void WriteMessageCore(RpcMessage message, IBufferWriter<byte> stream)
    {
        var reusableWriter = ReusableUtf8JsonWriter.Get(stream);

        try
        {
            var writer = reusableWriter.GetJsonWriter();
            writer.WriteStartObject();
            switch (message)
            {
                case InvocationMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.InvocationMessageType);
                    WriteHeaders(writer, m);
                    WriteInvocationMessage(m, writer);
                    break;
                case StreamInvocationMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.StreamInvocationMessageType);
                    WriteHeaders(writer, m);
                    WriteStreamInvocationMessage(m, writer);
                    break;
                case StreamItemMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.StreamItemMessageType);
                    WriteHeaders(writer, m);
                    WriteStreamItemMessage(m, writer);
                    break;
                case CompletionMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.CompletionMessageType);
                    WriteHeaders(writer, m);
                    WriteCompletionMessage(m, writer);
                    break;
                case CancelInvocationMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.CancelInvocationMessageType);
                    WriteHeaders(writer, m);
                    WriteCancelInvocationMessage(m, writer);
                    break;
                case PingMessage _:
                    WriteMessageType(writer, RpcProtocolConstants.PingMessageType);
                    break;
                case CloseMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.CloseMessageType);
                    WriteCloseMessage(m, writer);
                    break;
                case AckMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.AckMessageType);
                    WriteAckMessage(m, writer);
                    break;
                case SequenceMessage m:
                    WriteMessageType(writer, RpcProtocolConstants.SequenceMessageType);
                    WriteSequenceMessage(m, writer);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported message type: {message.GetType().FullName}");
            }
            writer.WriteEndObject();
            writer.Flush();
            Debug.Assert(writer.CurrentDepth == 0);
        }
        finally
        {
            ReusableUtf8JsonWriter.Return(reusableWriter);
        }
    }

    private static void WriteHeaders(Utf8JsonWriter writer, RpcInvocationMessage message)
    {
        if (message.Headers != null && message.Headers.Count > 0)
        {
            writer.WriteStartObject(HeadersPropertyNameBytes);
            foreach (var value in message.Headers)
            {
                writer.WriteString(value.Key, value.Value);
            }
            writer.WriteEndObject();
        }
    }

    private void WriteCompletionMessage(CompletionMessage message, Utf8JsonWriter writer)
    {
        WriteInvocationId(message, writer);
        if (!string.IsNullOrEmpty(message.Error))
        {
            writer.WriteString(ErrorPropertyNameBytes, message.Error);
        }
        else if (message.HasResult)
        {
            writer.WritePropertyName(ResultPropertyNameBytes);
            if (message.Result == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                if (message.Result is RawResult result)
                {
                    writer.WriteRawValue(result.RawSerializedData, skipInputValidation: true);
                }
                else
                {
                    JsonSerializer.Serialize(writer, message.Result, _payloadSerializerOptions);
                }
            }
        }
    }

    private static void WriteCancelInvocationMessage(CancelInvocationMessage message, Utf8JsonWriter writer)
    {
        WriteInvocationId(message, writer);
    }

    private void WriteStreamItemMessage(StreamItemMessage message, Utf8JsonWriter writer)
    {
        WriteInvocationId(message, writer);

        writer.WritePropertyName(ItemPropertyNameBytes);
        if (message.Item == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, message.Item, _payloadSerializerOptions);
        }
    }

    private void WriteInvocationMessage(InvocationMessage message, Utf8JsonWriter writer)
    {
        WriteInvocationId(message, writer);
        writer.WriteString(TargetPropertyNameBytes, message.Target);

        WriteArguments(message.Arguments, writer);

        WriteStreamIds(message.StreamIds, writer);
    }

    private void WriteStreamInvocationMessage(StreamInvocationMessage message, Utf8JsonWriter writer)
    {
        WriteInvocationId(message, writer);
        writer.WriteString(TargetPropertyNameBytes, message.Target);

        WriteArguments(message.Arguments, writer);

        WriteStreamIds(message.StreamIds, writer);
    }

    private static void WriteCloseMessage(CloseMessage message, Utf8JsonWriter writer)
    {
        if (message.Error != null)
        {
            writer.WriteString(ErrorPropertyNameBytes, message.Error);
        }

        if (message.AllowReconnect)
        {
            writer.WriteBoolean(AllowReconnectPropertyNameBytes, true);
        }
    }

    private static void WriteAckMessage(AckMessage message, Utf8JsonWriter writer)
    {
        writer.WriteNumber(SequenceIdPropertyName, message.SequenceId);
    }

    private static void WriteSequenceMessage(SequenceMessage message, Utf8JsonWriter writer)
    {
        writer.WriteNumber(SequenceIdPropertyName, message.SequenceId);
    }

    private void WriteArguments(object?[] arguments, Utf8JsonWriter writer)
    {
        writer.WriteStartArray(ArgumentsPropertyNameBytes);
        foreach (var argument in arguments)
        {
            if (argument == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, argument, _payloadSerializerOptions);
            }
        }
        writer.WriteEndArray();
    }

    private static void WriteStreamIds(string[]? streamIds, Utf8JsonWriter writer)
    {
        if (streamIds == null)
        {
            return;
        }

        writer.WriteStartArray(StreamIdsPropertyNameBytes);
        foreach (var streamId in streamIds)
        {
            writer.WriteStringValue(streamId);
        }
        writer.WriteEndArray();
    }

    private static void WriteInvocationId(RpcInvocationMessage message, Utf8JsonWriter writer)
    {
        if (!string.IsNullOrEmpty(message.InvocationId))
        {
            writer.WriteString(InvocationIdPropertyNameBytes, message.InvocationId);
        }
    }

    private static void WriteMessageType(Utf8JsonWriter writer, int type)
    {
        writer.WriteNumber(TypePropertyNameBytes, type);
    }

    private static RpcMessage BindCancelInvocationMessage(string? invocationId)
    {
        if (string.IsNullOrEmpty(invocationId))
        {
            throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
        }

        return new CancelInvocationMessage(invocationId);
    }

    private static RpcMessage BindCompletionMessage(string invocationId, string? error, object? result, bool hasResult)
    {
        if (string.IsNullOrEmpty(invocationId))
        {
            throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
        }

        if (error != null && hasResult)
        {
            throw new InvalidDataException("The 'error' and 'result' properties are mutually exclusive.");
        }

        if (hasResult)
        {
            return new CompletionMessage(invocationId, error, result, hasResult: true);
        }

        return new CompletionMessage(invocationId, error, result: null, hasResult: false);
    }

    private static RpcMessage BindStreamItemMessage(string invocationId, object? item, bool hasItem)
    {
        if (string.IsNullOrEmpty(invocationId))
        {
            throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
        }

        if (!hasItem)
        {
            throw new InvalidDataException($"Missing required property '{ItemPropertyName}'.");
        }

        return new StreamItemMessage(invocationId, item);
    }

    private static RpcMessage BindStreamInvocationMessage(string? invocationId, string target,
        object?[]? arguments, bool hasArguments, string[]? streamIds)
    {
        if (string.IsNullOrEmpty(invocationId))
        {
            throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
        }

        if (!hasArguments)
        {
            throw new InvalidDataException($"Missing required property '{ArgumentsPropertyName}'.");
        }

        if (string.IsNullOrEmpty(target))
        {
            throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
        }

        Debug.Assert(arguments != null);

        return new StreamInvocationMessage(invocationId, target, arguments, streamIds);
    }

    private static RpcMessage BindInvocationMessage(string? invocationId, string target,
        object?[]? arguments, bool hasArguments, string[]? streamIds)
    {
        if (string.IsNullOrEmpty(target))
        {
            throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
        }

        if (!hasArguments)
        {
            throw new InvalidDataException($"Missing required property '{ArgumentsPropertyName}'.");
        }

        Debug.Assert(arguments != null);

        return new InvocationMessage(invocationId, target, arguments, streamIds);
    }

    private object? BindType(ref Utf8JsonReader reader, ReadOnlySequence<byte> input, Type type)
    {
        if (type == typeof(RawResult))
        {
            var start = reader.BytesConsumed;
            reader.Skip();
            var end = reader.BytesConsumed;
            var sequence = input.Slice(start, end - start);
            // Review: Technically the sequence doesn't need to be copied to a new array in RawResult
            // but in the future we could break this if we dispatched the CompletionMessage and the underlying Pipe read would be advanced
            // instead we could try pooling in RawResult, but it would need release/dispose semantics
            return new RawResult(sequence);
        }
        return BindType(ref reader, type);
    }

    private object? BindType(ref Utf8JsonReader reader, Type type)
    {
        return JsonSerializer.Deserialize(ref reader, type, _payloadSerializerOptions);
    }

    private object?[] BindTypes(ref Utf8JsonReader reader, IReadOnlyList<Type> paramTypes)
    {
        object?[]? arguments = null;
        var paramIndex = 0;
        var paramCount = paramTypes.Count;

        var depth = reader.CurrentDepth;
        reader.CheckRead();

        while (reader.TokenType != JsonTokenType.EndArray && reader.CurrentDepth > depth)
        {
            if (paramIndex < paramCount)
            {
                arguments ??= new object?[paramCount];

                try
                {
                    arguments[paramIndex] = BindType(ref reader, paramTypes[paramIndex]);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
                }
            }
            else
            {
                // Skip extra arguments and throw error after reading them all
                reader.Skip();
            }
            reader.CheckRead();
            paramIndex++;
        }

        if (paramIndex != paramCount)
        {
            throw new InvalidDataException($"Invocation provides {paramIndex} argument(s) but target expects {paramCount}.");
        }

        return arguments ?? Array.Empty<object>();
    }

    private static CloseMessage BindCloseMessage(string? error, bool allowReconnect)
    {
        // An empty string is still an error
        if (error == null && !allowReconnect)
        {
            return CloseMessage.Empty;
        }

        return new CloseMessage(error, allowReconnect);
    }

    private static AckMessage BindAckMessage(long? sequenceId)
    {
        if (sequenceId is null)
        {
            throw new InvalidDataException("Missing required property 'sequenceId'.");
        }

        return new AckMessage(sequenceId.Value);
    }

    private static SequenceMessage BindSequenceMessage(long? sequenceId)
    {
        if (sequenceId is null)
        {
            throw new InvalidDataException("Missing required property 'sequenceId'.");
        }

        return new SequenceMessage(sequenceId.Value);
    }

    private static RpcMessage ApplyHeaders(RpcMessage message, Dictionary<string, string>? headers)
    {
        if (headers != null && message is RpcInvocationMessage invocationMessage)
        {
            invocationMessage.Headers = headers;
        }

        return message;
    }

    internal static JsonSerializerOptions CreateDefaultSerializerSettings()
    {
        return new JsonSerializerOptions()
        {
            WriteIndented = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreReadOnlyProperties = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 64,
            DictionaryKeyPolicy = null,
            DefaultBufferSize = 16 * 1024,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
}
