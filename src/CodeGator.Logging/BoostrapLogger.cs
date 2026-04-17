
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Logging;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// This class provides a minimal bootstrap <see cref="ILogger"/> before DI wiring.
/// </summary>
public sealed class BootstrapLogger : ILogger
{

    /// <summary>
    /// This field contains the factory for this bootstrap logger.
    /// </summary>
    internal static ILoggerFactory? _loggerFactory;

    /// <summary>
    /// This field contains the singleton instance for this bootstrap logger.
    /// </summary>
    internal static BootstrapLogger _instance = null!;

    /// <summary>
    /// This field contains the inner logger for this bootstrap logger.
    /// </summary>
    internal readonly ILogger? _innerLogger;

    /// <summary>
    /// This constructor initializes a new instance of the BootstrapLogger class.
    /// </summary>
    [DebuggerStepThrough]
    private BootstrapLogger()
    {
        _innerLogger = _loggerFactory?.CreateLogger<BootstrapLogger>();
    }

    /// <summary>
    /// This method returns the shared bootstrap <see cref="ILogger"/> instance.
    /// </summary>
    /// <returns>The singleton bootstrap logger.</returns>
    [DebuggerStepThrough]
    public static ILogger Instance()
    {
        if (_instance is null)
        {
            if (_loggerFactory is null)
            {
                LogLevelToInformation();
            }

            _instance = new BootstrapLogger();
        }
        return _instance;
    }

    /// <summary>
    /// This method sets the minimum logger level to information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToInformation()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    /// <summary>
    /// This method sets the minimum logger level to warning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToWarning()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Warning);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    /// <summary>
    /// This method sets the minimum logger level to error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToError()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Error);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    /// <summary>
    /// This method sets the minimum logger level to critical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToCritical()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Critical);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    /// <summary>
    /// This method sets the minimum logger level to debug.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToDebug()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    /// <summary>
    /// This method sets the minimum logger level to trace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called before the <see cref="BootstrapLogger.Instance"/>
    /// method is called, for it to have any effect.
    /// </para>
    /// </remarks>
    public static void LogLevelToTrace()
    {
        if (_instance is null)
        {
            _loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });
        }
    }

    // ILogger methods.

    /// <summary>
    /// This method begins a logical operation scope on the inner logger.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the scope on dispose, or null
    /// when the inner logger is absent.</returns>
    [DebuggerStepThrough]
    IDisposable ILogger.BeginScope<TState>(
        TState state
        )
    {
#pragma warning disable CS8603 // Possible null reference return.
        return _innerLogger?.BeginScope(state);
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// This method reports whether the inner logger enables a log level.
    /// </summary>
    /// <param name="logLevel">The level to check.</param>
    /// <returns>true when the inner logger enables the level; otherwise false.</returns>
    [DebuggerStepThrough]
    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return _innerLogger?.IsEnabled(logLevel) ?? false;
    }

    /// <summary>
    /// This method writes a log entry through the inner logger.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">The level for the entry.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="state">The entry payload.</param>
    /// <param name="exception">The related exception, if any.</param>
    /// <param name="formatter">Builds the message from state and exception.</param>
    [DebuggerStepThrough]
    void ILogger.Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
        )
    {
        _innerLogger?.Log<TState>(
            logLevel,
            eventId,
            state,
            exception,
            formatter
            );
    }
}
