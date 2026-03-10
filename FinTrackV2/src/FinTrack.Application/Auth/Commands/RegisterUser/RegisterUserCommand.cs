using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Auth.Commands.RegisterUser;

/// <summary>
/// A Command is an intention to change state.
/// It returns a Result — success with a token, or failure with an error.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<AuthResponse>>;

public record AuthResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    DateTime ExpiresAt);
