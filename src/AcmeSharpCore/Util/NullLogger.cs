using System;
using Microsoft.Extensions.Logging;

namespace AcmeSharpCore.Util
{
    /// <summary>
    /// Implementation of the logger interface that does nothing.
    /// </summary>
    /// <remarks>
    /// Provides a default, do-nothing implementation.
    /// </remarks> 
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        public bool IsEnabled(LogLevel logLevel) => false;

        public IDisposable BeginScope<TState>(TState state) => new NullLoggerScope<TState>();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception exception, Func<TState, Exception, string> formatter)
        { }

        public class NullLoggerScope<TState> : IDisposable
        {
            public void Dispose()
            { }
        }
    }
}