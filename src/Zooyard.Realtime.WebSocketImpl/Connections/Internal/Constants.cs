using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;

namespace Zooyard.WebSocketsImpl.Connections.Internal;

internal static class Constants
{
    public static readonly ProductInfoHeaderValue UserAgentHeader;

    static Constants()
    {
        var userAgent = "Zooyard.WebSocketsImpl";

        var assemblyVersion = typeof(Constants)
            .Assembly
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();

        Debug.Assert(assemblyVersion != null);

        // assembly version attribute should always be present
        // but in case it isn't then don't include version in user-agent
        if (assemblyVersion != null)
        {
            userAgent += "/" + assemblyVersion.InformationalVersion;
        }

        UserAgentHeader = ProductInfoHeaderValue.Parse(userAgent);
    }
}
