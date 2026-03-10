using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Handles the RegisterUserCommand.
/// Notice: no HTTP, no EF Core, no BCrypt directly — only interfaces.
/// This makes it trivially testable — just mock the interfaces.
/// </summary>
public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public RegisterUserHandler(IUserRepository users, IPasswordHasher hasher, IJwtService jwt)
    {
        _users  = users;
        _hasher = hasher;
        _jwt    = jwt;
    }

    public async Task<Result<AuthResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken ct)
    {
        // 1. Check duplicate email
        if (await _users.ExistsWithEmailAsync(command.Email, ct))
            return Result.Failure<AuthResponse>(UserErrors.EmailAlreadyExists);

        // 2. Create the user via factory method (domain enforces valid state)
        var user = User.Create(
            command.Email,
            _hasher.Hash(command.Password),
            command.FirstName,
            command.LastName);

        // 3. Persist
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        // 4. Return token
        var token   = _jwt.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(24);

        return Result.Success(new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, token, expires));
    }
}
