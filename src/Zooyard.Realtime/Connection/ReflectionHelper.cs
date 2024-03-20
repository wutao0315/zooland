using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Zooyard.Realtime.Connection;

internal static class ReflectionHelper
{
    // mustBeDirectType - Hub methods must use the base 'stream' type and not be a derived class that just implements the 'stream' type
    // and 'stream' types from the client are allowed to inherit from accepted 'stream' types
    public static bool IsStreamingType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type, bool mustBeDirectType = false)
    {
        // TODO #2594 - add Streams here, to make sending files easy

        if (IsIAsyncEnumerable(type))
        {
            return true;
        }

        Type? nullableType = type;

        do
        {
            if (nullableType.IsGenericType && nullableType.GetGenericTypeDefinition() == typeof(ChannelReader<>))
            {
                return true;
            }

            nullableType = nullableType.BaseType;
        } while (mustBeDirectType == false && nullableType != null);

        return false;
    }

    public static bool IsIAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(t =>
        {
            if (t.IsGenericType)
            {
                return t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
            }
            else
            {
                return false;
            }
        });
    }
}
