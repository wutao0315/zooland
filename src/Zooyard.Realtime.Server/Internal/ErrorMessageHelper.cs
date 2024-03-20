namespace Zooyard.Realtime.Server.Internal;

internal static class ErrorMessageHelper
{
    internal static string BuildErrorMessage(string message, Exception exception, bool includeExceptionDetails)
    {
        if (exception is RpcException || includeExceptionDetails)
        {
            return $"{message} {exception.GetType().Name}: {exception.Message}";
        }

        return message;
    }
}
