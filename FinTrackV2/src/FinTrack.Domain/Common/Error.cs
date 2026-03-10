namespace FinTrack.Domain.Common;

/// <summary>
/// Represents a named error with a code and human-readable message.
/// Errors are values — not exceptions.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>Used as a "no error" placeholder — like null but explicit.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
