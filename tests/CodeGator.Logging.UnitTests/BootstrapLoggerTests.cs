using Microsoft.Extensions.Logging;

namespace CodeGator.Logging.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class BootstrapLoggerTests
{
    [TestCleanup]
    public void Cleanup()
    {
        BootstrapLogger._instance = null!;
        BootstrapLogger._loggerFactory?.Dispose();
        BootstrapLogger._loggerFactory = null;
    }

    [TestMethod]
    public void Instance_ReturnsSameReference()
    {
        using var factory = CreateTestFactory(out _);
        BootstrapLogger._loggerFactory = factory;

        var a = BootstrapLogger.Instance();
        var b = BootstrapLogger.Instance();

        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void Instance_WhenFactoryUnset_UsesInformationMinimumAndCreatesSingleton()
    {
        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
    }

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

    [TestMethod]
    public void IsEnabled_ForwardsToInnerLogger()
    {
        using var factory = CreateSelectiveFactory(LogLevel.Warning, out _);
        BootstrapLogger._loggerFactory = factory;

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
    }

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

    [TestMethod]
    public void LogLevelToInformation_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToInformation();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Debug));
    }

    [TestMethod]
    public void LogLevelToWarning_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToWarning();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
    }

    [TestMethod]
    public void LogLevelToError_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToError();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Warning));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Error));
    }

    [TestMethod]
    public void LogLevelToCritical_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToCritical();

        var logger = BootstrapLogger.Instance();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Error));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Critical));
    }

    [TestMethod]
    public void LogLevelToDebug_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToDebug();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Debug));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
    }

    [TestMethod]
    public void LogLevelToTrace_ConfiguresFactoryBeforeFirstInstance()
    {
        BootstrapLogger.LogLevelToTrace();

        var logger = BootstrapLogger.Instance();

        Assert.IsTrue(logger.IsEnabled(LogLevel.Trace));
    }

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

    private static ILoggerFactory CreateTestFactory(out CollectingLogger collecting)
    {
        var provider = new CollectingLoggerProvider();
        collecting = provider.Logger;
        return LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(LogLevel.Trace));
    }

    private static ILoggerFactory CreateSelectiveFactory(LogLevel minimum, out CollectingLogger collecting)
    {
        var provider = new CollectingLoggerProvider();
        collecting = provider.Logger;
        return LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(minimum));
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public CollectingLogger Logger { get; } = new();

        public ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public int ScopeBegins { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ScopeBegins++;
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

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

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
