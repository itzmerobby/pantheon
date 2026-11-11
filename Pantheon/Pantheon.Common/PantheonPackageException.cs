namespace Pantheon.Common;

/// <summary>
/// Represents a Pantheon package error with a stable identification code.
/// </summary>
public sealed class PantheonPackageException : Exception
{
    /// <summary>
    /// Creates a Pantheon package exception.
    /// </summary>
    /// <param name="message">A description of what went wrong.</param>
    /// <param name="code">The stable package error code.</param>
    /// <param name="innerException">An exception thrown by user-provided code, if any.</param>
    public PantheonPackageException(string message, string code, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ValidateCode(code);
        Code = code;
    }

    /// <summary>Gets the stable identification code for this error.</summary>
    public string Code { get; }

    private static void ValidateCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var prefixLength = 0;
        while (prefixLength < code.Length && char.IsAsciiLetter(code[prefixLength]))
        {
            prefixLength++;
        }

        var codeLength = code.Length - prefixLength;
        var hasNonDigit = false;
        for (var index = prefixLength; index < code.Length; index++)
        {
            hasNonDigit |= !char.IsAsciiDigit(code[index]);
        }

        if (prefixLength is < 3 or > 12 ||
            codeLength is < 1 or > 6 ||
            hasNonDigit ||
            !int.TryParse(code.AsSpan(prefixLength), out var numericCode) ||
            numericCode is < 1 or > 999999)
        {
            throw new ArgumentException(
                "The code must contain a 3-12 letter prefix followed by a number from 1 to 999999.",
                nameof(code));
        }
    }
}
