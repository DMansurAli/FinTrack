using FinTrack.Application.Auth.Commands.RegisterUser;
using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Auth.Commands.LoginUser;

public sealed class LoginUserHandler : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public LoginUserHandler(IUserRepository users, IPasswordHasher hasher, IJwtService jwt)
    {
        _users  = users;
        _hasher = hasher;
        _jwt    = jwt;
    }

    public async Task<Result<AuthResponse>> Handle(LoginUserCommand command, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(command.Email, ct);

        if (user is null || !_hasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var token   = _jwt.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(24);

        return Result.Success(new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, token, expires));
    }
}
