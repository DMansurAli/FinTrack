using FinTrack.Application.Auth.Commands.RegisterUser;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Auth.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
