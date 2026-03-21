using FinTrack.IdentityService.Data;
using FinTrack.IdentityService.Models;
using FinTrack.IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly TokenService      _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IdentityDbContext db, TokenService tokens, ILogger<AuthController> logger)
    {
        _db     = db;
        _tokens = tokens;
        _logger = logger;
    }

    public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(Guid UserId, string Email, string FirstName, string AccessToken);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.ToLowerInvariant().Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { code = "User.EmailAlreadyExists",
                                  message = "A user with this email already exists." });

        var user = new AppUser
        {
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            FirstName    = req.FirstName.Trim(),
            LastName     = req.LastName.Trim(),
        };

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email}", user.Email);

        return CreatedAtAction(nameof(Register),
            new AuthResponse(user.Id, user.Email, user.FirstName, _tokens.GenerateToken(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.ToLowerInvariant().Trim();
        var user  = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same error for wrong email AND wrong password — no email enumeration
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { code    = "User.InvalidCredentials",
                                      message = "The email or password is incorrect." });

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return Ok(new AuthResponse(user.Id, user.Email, user.FirstName, _tokens.GenerateToken(user)));
    }
}
