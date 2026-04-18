
namespace CodeGator.Logging.Options;

/// <summary>
/// This class contains options for <c>CodeGator</c> logging extensions.
/// </summary>
public sealed class LoggerExtensionOptions
{
    // *******************************************************************
    // Properties.
    // *******************************************************************

    #region Properties

    /// <summary>
    /// This property contains the path to these options in the configuration.
    /// </summary>
    public const string OptionsPath = "CodeGator:Logging";

    /// <summary>
    /// This property contains the key for hashing PII log data.
    /// </summary>
    [Required]
    public string HashKey { get; set; } = null!;

    /// <summary>
    /// This property contains the key id for hashing PII data.
    /// </summary>
    [Required]
    public int HashKeyId { get; set; }

    #endregion
}
