﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Zooyard.Core.Utils;

namespace Zooyard.Core.Logging
{
    public static class LogManager
    {
        private static readonly Action<LogLevel, string, Exception> Noop = (level, msg, ex) => { };
        public static Func<string, Action<LogLevel, string, Exception>> LogFactory { get; set; } = name => Noop;

        public static void UseConsoleLogging(LogLevel minimumLevel) =>
            LogFactory = name => (level, message, exception) =>
            {
                if (level < minimumLevel) return;

                Console.WriteLine(exception == null
                    ? $"{DateTime.Now:HH:mm:ss} [{level}] {message}"
                    : $"{DateTime.Now:HH:mm:ss} [{level}] {message} - {exception.GetDetailMessage()}");
            };

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

        internal static void Error(this Action<LogLevel, string, Exception> logger, string message) =>
            logger(LogLevel.Error, message, null);

        internal static void Error(this Action<LogLevel, string, Exception> logger, Exception exception) =>
            logger(LogLevel.Error, exception.Message, exception);

        internal static void Error(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
            logger(LogLevel.Error, message, exception);

        internal static void Warn(this Action<LogLevel, string, Exception> logger, Exception exception) =>
            logger(LogLevel.Warn, exception.Message, exception);

        internal static void Warn(this Action<LogLevel, string, Exception> logger, string message) =>
            logger(LogLevel.Warn, message, null);

        internal static void Warn(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
            logger(LogLevel.Warn, message, exception);

        internal static void Information(this Action<LogLevel, string, Exception> logger, Exception exception) =>
            logger(LogLevel.Info, exception.Message, exception);

        internal static void Information(this Action<LogLevel, string, Exception> logger, string message) =>
            logger(LogLevel.Info, message, null);

        internal static void Information(this Action<LogLevel, string, Exception> logger, Exception exception, string message) =>
            logger(LogLevel.Info, message, exception);

        internal static void Debug(this Action<LogLevel, string, Exception> logger, string message) =>
            logger(LogLevel.Debug, message, null);
    }
}
