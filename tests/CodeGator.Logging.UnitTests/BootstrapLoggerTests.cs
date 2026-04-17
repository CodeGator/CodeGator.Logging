using Microsoft.Extensions.Logging;

namespace CodeGator.Logging.UnitTests;

/// <summary>
/// This class holds unit tests for the <see cref="BootstrapLogger"/> type.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BootstrapLoggerTests
{
    /// <summary>
    /// This method resets static bootstrap logger state between tests.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        BootstrapLogger._instance = null!;
        BootstrapLogger._loggerFactory?.Dispose();
        BootstrapLogger._loggerFactory = null;
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.Instance"/> returns the same
    /// reference on repeated calls.
    /// </summary>
    [TestMethod]
    public void Instance_ReturnsSameReference()
    {
        using var factory = CreateTestFactory(out _);
        BootstrapLogger._loggerFactory = factory;

        var a = BootstrapLogger.Instance();
        var b = BootstrapLogger.Instance();

        Assert.AreSame(a, b);
    }

    /// <summary>
    /// This method asserts the default factory uses Information and not Trace.
    /// </summary>
    [TestMethod]
    public void Instance_WhenFactoryUnset_UsesInformationMinimumAndCreatesSingleton()
    {
        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
    }

    /// <summary>
    /// This method asserts <see cref="ILogger.Log"/> forwards to the inner logger.
    /// </summary>
    [TestMethod]
    public void Log_ForwardsToInnerLogger()
    {
        using var factory = CreateTestFactory(out var collecting);
        BootstrapLogger._loggerFactory = factory;

        var logger = BootstrapLogger.Instance();
        logger.Log(LogLevel.Warning, new EventId(42), "hello", null, (s, e) => s?.ToString() ?? "");

        Assert.AreEqual(1, collecting.Entries.Count);
        Assert.AreEqual(LogLevel.Warning, collecting.Entries[0].Level);
        StringAssert.Contains(collecting.Entries[0].Message, "hello");
    }

    /// <summary>
    /// This method asserts <see cref="ILogger.IsEnabled"/> reflects the inner logger.
    /// </summary>
    [TestMethod]
    public void IsEnabled_ForwardsToInnerLogger()
    {
        using var factory = CreateSelectiveFactory(LogLevel.Warning, out _);
        BootstrapLogger._loggerFactory = factory;

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
    }

    /// <summary>
    /// This method asserts <see cref="ILogger.BeginScope"/> forwards to the inner
    /// logger.
    /// </summary>
    [TestMethod]
    public void BeginScope_ForwardsToInnerLogger()
    {
        using var factory = CreateTestFactory(out var collecting);
        BootstrapLogger._loggerFactory = factory;

        var logger = BootstrapLogger.Instance();
        using var scope = logger.BeginScope("scope-state");

        Assert.IsNotNull(scope);
        Assert.AreEqual(1, collecting.ScopeBegins);
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToInformation"/> runs
    /// before the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToInformation_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToInformation();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Debug));
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToWarning"/> runs
    /// before the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToWarning_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToWarning();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToError"/> runs before
    /// the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToError_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToError();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Warning));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Error));
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToCritical"/> runs
    /// before the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToCritical_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToCritical();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Error));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Critical));
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToDebug"/> runs before
    /// the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToDebug_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToDebug();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Debug));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
    }

    /// <summary>
    /// This method asserts <see cref="BootstrapLogger.LogLevelToTrace"/> runs before
    /// the first <see cref="BootstrapLogger.Instance"/> call.
    /// </summary>
    [TestMethod]
    public void LogLevelToTrace_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToTrace();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Trace));
    }

    /// <summary>
    /// This method asserts log-level helpers do not replace the factory after
    /// <see cref="BootstrapLogger.Instance"/> has run.
    /// </summary>
    [TestMethod]
    public void LogLevelMethods_AfterInstanceCreated_DoNotChangeExistingFactory()
    {
        BootstrapLogger.LogLevelToInformation();
        var first = BootstrapLogger.Instance();
        Assert.IsTrue(first.IsEnabled(LogLevel.Information));

        BootstrapLogger.LogLevelToError();

        var second = BootstrapLogger.Instance();

        Assert.AreSame(first, second);
        Assert.IsTrue(second.IsEnabled(LogLevel.Information));
        Assert.IsFalse(second.IsEnabled(LogLevel.Trace));
    }

    /// <summary>
    /// This method builds a factory that records log output for assertions.
    /// </summary>
    /// <param name="collecting">The logger that receives entries from the factory.</param>
    /// <returns>A disposable factory configured for tests.</returns>
    private static ILoggerFactory CreateTestFactory(out CollectingLogger collecting)
    {
        var provider = new CollectingLoggerProvider();
        collecting = provider.Logger;
        return LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(LogLevel.Trace));
    }

    /// <summary>
    /// This method builds a factory with a fixed minimum level for assertions.
    /// </summary>
    /// <param name="minimum">The minimum level passed to the factory builder.</param>
    /// <param name="collecting">The logger that receives entries from the factory.</param>
    /// <returns>A disposable factory configured for tests.</returns>
    private static ILoggerFactory CreateSelectiveFactory(LogLevel minimum, out CollectingLogger collecting)
    {
        var provider = new CollectingLoggerProvider();
        collecting = provider.Logger;
        return LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(minimum));
    }

    /// <summary>
    /// This class supplies a test <see cref="ILoggerProvider"/> backed by a collector.
    /// </summary>
    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// This property exposes the collecting logger used in assertions.
        /// </summary>
        public CollectingLogger Logger { get; } = new();

        /// <summary>
        /// This method creates the category logger for the provider.
        /// </summary>
        /// <param name="categoryName">The logger category name (ignored).</param>
        /// <returns>The shared <see cref="CollectingLogger"/> instance.</returns>
        public ILogger CreateLogger(string categoryName) => Logger;

        /// <summary>
        /// This method releases provider resources (no-op for this test type).
        /// </summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// This class implements a test <see cref="ILogger"/> that records calls.
    /// </summary>
    private sealed class CollectingLogger : ILogger
    {
        /// <summary>
        /// This property holds log entries captured during a test.
        /// </summary>
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        /// <summary>
        /// This property gets how many scope begins were observed.
        /// </summary>
        public int ScopeBegins { get; private set; }

        /// <summary>
        /// This method records a scope begin and returns a disposable placeholder.
        /// </summary>
        /// <typeparam name="TState">The type of the scope state.</typeparam>
        /// <param name="state">The scope state from the caller.</param>
        /// <returns>A disposable token that represents the scope.</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ScopeBegins++;
            return NullScope.Instance;
        }

        /// <summary>
        /// This method reports that all levels are enabled for this test logger.
        /// </summary>
        /// <param name="logLevel">The level being checked.</param>
        /// <returns>true for every level in this test double.</returns>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>
        /// This method appends a formatted log entry to <see cref="Entries"/>.
        /// </summary>
        /// <typeparam name="TState">The type of the logged state.</typeparam>
        /// <param name="logLevel">The level for the entry.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="state">The logged state object.</param>
        /// <param name="exception">The related exception, if any.</param>
        /// <param name="formatter">Builds the message from state and exception.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    /// <summary>
    /// This class provides a shared no-op <see cref="IDisposable"/> for test scopes.
    /// </summary>
    private sealed class NullScope : IDisposable
    {
        /// <summary>
        /// This property holds the singleton null-scope instance for tests.
        /// </summary>
        public static readonly NullScope Instance = new();

        /// <summary>
        /// This method performs a no-op dispose for the placeholder scope.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
