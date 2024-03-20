using System.Buffers;

namespace Zooyard.Realtime.Internal;

internal static class TextMessageParser
{
    public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
    {
        var position = buffer.PositionOf(TextMessageFormatter.RecordSeparator);
        if (position == null)
        {
            payload = default;
            return false;
        }

        payload = buffer.Slice(0, position.Value);

        // Skip record separator
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }
}
