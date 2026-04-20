
namespace CodeGator.Logging.Redactors;

/// <summary>
/// This class obfuscates sensitive data using '*' characters.
/// </summary>
internal sealed class StarRedactor : Redactor
{
    // *******************************************************************
    // Public methods.
    // *******************************************************************

    #region Public methods

    /// <inheritdoc/>
    public override int GetRedactedLength(
        [NotNull] ReadOnlySpan<char> input
        )
    {
        return input.Length;
    }

    // *******************************************************************

    /// <inheritdoc/>
    public override int Redact(
        [NotNull] ReadOnlySpan<char> source, 
        [NotNull] Span<char> destination
        )
    {
        destination.Fill('*');
        return destination.Length;
    }

    #endregion
}
