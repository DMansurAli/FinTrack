using FinTrack.Application.Auth.Commands.LoginUser;
using FinTrack.Application.Auth.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    public record RegisterRequest(string Email, string Password,
        string FirstName, string LastName);

    public record LoginRequest(string Email, string Password);

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterUserCommand(
            req.Email, req.Password, req.FirstName, req.LastName);

        var result = await _sender.Send(command, ct);

        return result.IsFailure
            ? Conflict(new { result.Error.Code, result.Error.Message })
            : CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>Login and receive a JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req, CancellationToken ct)
    {
        var command = new LoginUserCommand(req.Email, req.Password);

        var result = await _sender.Send(command, ct);

        return result.IsFailure
            ? Unauthorized(new { result.Error.Code, result.Error.Message })
            : Ok(result.Value);
    }
}
