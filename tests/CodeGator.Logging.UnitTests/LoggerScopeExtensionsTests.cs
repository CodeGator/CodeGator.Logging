using CodeGator.Logging.Attributes;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;

namespace CodeGator.Logging.UnitTests;

[TestClass]
public sealed class LoggerScopeExtensionsTests
{
    [TestMethod]
    public void BeginSanitizedScope_WrapsAttributedMembersAsClassifiedData()
    {
        var provider = new ScopeCapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder =>
            builder.AddProvider(provider));

        var logger = factory.CreateLogger("test");
        var redactors = new FakeRedactorProvider();
        using var _ = logger.BeginSanitizedScope(
            redactors,
            new TestModel("person@example.com")
            );

        Assert.IsNotNull(provider.Logger.LastScopeState);

        var scopeState = provider.Logger.LastScopeState!;
        var emailEntry = scopeState.FirstOrDefault(x => x.Key == nameof(TestModel.EmailAddress));

        Assert.AreEqual(nameof(TestModel.EmailAddress), emailEntry.Key);
        Assert.IsNotNull(emailEntry.Value);
        Assert.AreEqual("REDACTED", emailEntry.Value);
    }

    private sealed class TestModel
    {
        public TestModel(string emailAddress)
        {
            EmailAddress = emailAddress;
        }

        [ObfuscateForLogging]
        public string EmailAddress { get; }
    }

    private sealed class ScopeCapturingLoggerProvider : ILoggerProvider
    {
        public ScopeCapturingLogger Logger { get; } = new();

        public ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose()
        {
        }
    }

    private sealed class ScopeCapturingLogger : ILogger
    {
        public IReadOnlyList<KeyValuePair<string, object?>>? LastScopeState { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> list)
            {
                LastScopeState = list;
            }

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
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed class FakeRedactorProvider : IRedactorProvider
    {
        public Redactor GetRedactor(DataClassification classification)
        {
            return new ConstantRedactor("REDACTED");
        }

        public Redactor GetRedactor(DataClassificationSet classifications)
        {
            return new ConstantRedactor("REDACTED");
        }
    }

    private sealed class ConstantRedactor : Redactor
    {
        private readonly string _value;

        public ConstantRedactor(string value)
        {
            _value = value;
        }

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (destination.Length < _value.Length)
            {
                throw new ArgumentException("Destination is too small.", nameof(destination));
            }

            _value.AsSpan().CopyTo(destination);
            return _value.Length;
        }

        public override int GetRedactedLength(ReadOnlySpan<char> input)
        {
            return _value.Length;
        }
    }
}

