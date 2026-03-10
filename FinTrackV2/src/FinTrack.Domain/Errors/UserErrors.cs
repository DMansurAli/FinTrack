using FinTrack.Domain.Common;

namespace FinTrack.Domain.Errors;

/// <summary>
/// All possible User-related errors in one place.
/// No magic strings scattered across the codebase.
/// </summary>
public static class UserErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "User.EmailAlreadyExists",
        "A user with this email already exists.");

    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "The email or password is incorrect.");

    public static readonly Error NotFound = new(
        "User.NotFound",
        "The user was not found.");
}
