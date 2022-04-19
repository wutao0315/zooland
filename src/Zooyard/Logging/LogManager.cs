using System.Diagnostics;
using Zooyard.Utils;

namespace Zooyard.Logging;

public static class LogManager
{
    private static LogLevel _minimumLevel;
    private static readonly Action<LogLevel, string, Exception> Noop = (level, msg, ex) => { };
    internal static Func<string, Action<LogLevel, string, Exception>> LogFactory { get; set; } = name => Noop;
    internal static void SetLevel(LogLevel minimumLevel)
    {
        _minimumLevel = minimumLevel;
    }
    internal static void UseConsoleLogging(LogLevel minimumLevel = LogLevel.Debug)
    {
        SetLevel(minimumLevel);
        LogFactory = name => (level, message, exception) =>
        {
            if (level < _minimumLevel) return;

            Console.WriteLine(exception == null
                ? $"{DateTime.Now:HH:mm:ss} [{level}] {message}"
                : $"{DateTime.Now:HH:mm:ss} [{level}] {message} - {exception.GetDetailMessage()}");
        };
    }
    internal static Action<LogLevel, string, Exception> CreateLogger(Type type)
    {
        try
        {
            return LogFactory(type.FullName);
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());

            return Noop;
        }
    }

    internal static void LogError(this Action<LogLevel, string, Exception> logger, string message) =>
        logger(LogLevel.Error, message, null);

    internal static void LogError(this Action<LogLevel, string, Exception> logger, Exception exception) =>
        logger(LogLevel.Error, exception.Message, exception);

    internal static void LogError(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
        logger(LogLevel.Error, message, exception);

    internal static void LogWarning(this Action<LogLevel, string, Exception> logger, Exception exception) =>
        logger(LogLevel.Warning, exception.Message, exception);

    internal static void LogWarning(this Action<LogLevel, string, Exception> logger, string message) =>
        logger(LogLevel.Warning, message, null);

    internal static void LogWarning(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
        logger(LogLevel.Warning, message, exception);

    internal static void LogInformation(this Action<LogLevel, string, Exception> logger, Exception exception) =>
        logger(LogLevel.Information, exception.Message, exception);

    internal static void LogInformation(this Action<LogLevel, string, Exception> logger, string message) =>
        logger(LogLevel.Information, message, null);

    internal static void LogInformation(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
        logger(LogLevel.Information, message, exception);

    internal static void LogDebug(this Action<LogLevel, string, Exception> logger, string message) =>
        logger(LogLevel.Debug, message, null);
    internal static bool IsEnabled(this Action<LogLevel, string, Exception> _, LogLevel level) => level >= _minimumLevel;
}
