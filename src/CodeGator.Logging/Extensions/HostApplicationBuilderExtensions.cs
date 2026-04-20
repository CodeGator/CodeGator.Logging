
// Thanks to Nick Chapsas for this idea. You 'da man, Nick!
// https://www.youtube.com/watch?v=rK3-tO7K6i8&t=478s

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// This class contains extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class contains extension methods related to the
/// <see cref="IHostApplicationBuilder"/> type.
/// </para>
/// </remarks>
public static partial class HostApplicationBuilderExtensions
{

    /// <summary>
    /// This method adds CodeGator logging services to a host application builder.
    /// </summary>
    /// <typeparam name="T">The host application builder type.</typeparam>
    /// <param name="builder">The builder to use for the operation.</param>
    /// <param name="bootstrapLogger">The optional bootstrap logger to use.</param>
    /// <returns>The builder for chaining method calls together.</returns>
    /// <exception cref="InvalidOperationException">
    /// This exception is thrown whenever the logger extension options cannot be bound.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <c>CodeGator</c> logging extensions include:
    /// <list type="bullet">
    /// <item>Ensuring the JSON logger is configured to log pretty, human 
    /// readable JSON log data.</item>
    /// <item>Ensuring sensitive data is redacted.</item>
    /// <item>Ensuring PII data is hashed.</item>
    /// </list>
    /// </para>
    /// <para>
    /// NOTE: When calling this method inside an <c>Aspire</c> application,
    /// call it BEFORE the AddServiceDefaults method. 
    /// </para>
    /// </remarks>
    public static T AddCodeGatorLoggingExtensions<T>(
        [NotNull] this T builder,
        [AllowNull] ILogger? bootstrapLogger = null
        ) where T : IHostApplicationBuilder
    {
        if (bootstrapLogger is not null)
        {
            ZAddCodeGatorLoggingExtensionsEntry(bootstrapLogger);
        }

        builder.Services.AddOptions<LoggerExtensionOptions>()
            .BindConfiguration(LoggerExtensionOptions.OptionsPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Logging.ClearProviders();

        builder.Logging.AddJsonConsole(options =>
            options.JsonWriterOptions = new()
            {
                Indented = true
            }
        );

        builder.Logging.EnableRedaction();

        builder.Services.AddRedaction(x =>
        {
            var loggerOptions = builder.Configuration.GetSection(
                LoggerExtensionOptions.OptionsPath
                ).Get<LoggerExtensionOptions>();

            if (loggerOptions is null)
            {
                throw new InvalidOperationException(
                    "Failed to bind logger extension options!"
                    );
            }

            x.SetRedactor<ErasingRedactor>(
                new DataClassificationSet(DataTaxonomy.RedactedData)
                );

            var keyBytes = Encoding.UTF8.GetBytes(
                loggerOptions.HashKey
                );

            x.SetHmacRedactor(hmacOptions =>
            {
                hmacOptions.Key = Convert.ToBase64String(
                    keyBytes
                    );

                hmacOptions.KeyId = loggerOptions.HashKeyId;

            }, new DataClassificationSet(DataTaxonomy.ObfuscatedData));

            x.SetRedactor<StarRedactor>(
                new DataClassificationSet(DataTaxonomy.HashedData)
                );
        });

        if (bootstrapLogger is not null)
        {
            ZAddCodeGatorLoggingExtensionsExit(bootstrapLogger);
        }

        return builder;
    }



    /// <summary>
    /// This method logs entry into the AddCodeGatorLoggingExtensions workflow.
    /// </summary>
    /// <param name="logger">The logger to use for the operation.</param>
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Entering AddCodeGatorLoggingExtensions")]
    private static partial void ZAddCodeGatorLoggingExtensionsEntry(
        ILogger logger
        );

    /// <summary>
    /// This method logs exit from the AddCodeGatorLoggingExtensions workflow.
    /// </summary>
    /// <param name="logger">The logger to use for the operation.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Exiting AddCodeGatorLoggingExtensions")]
    private static partial void ZAddCodeGatorLoggingExtensionsExit(
        ILogger logger
        );
}
