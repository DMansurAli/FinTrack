using System.ComponentModel.DataAnnotations;
using FinTrack.Api.Data;
using FinTrack.Api.Models;
using FinTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;

    public AuthController(AppDbContext db, IJwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // ── DTOs ──────────────────────────────────────────────────

    public record RegisterRequest(
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MinLength(8), MaxLength(100)] string Password,
        [Required, MaxLength(100)]               string FirstName,
        [Required, MaxLength(100)]               string LastName);

    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required]               string Password);

    public record AuthResponse(
        Guid   UserId,
        string Email,
        string FirstName,
        string LastName,
        string AccessToken,
        DateTime ExpiresAt);

    // ── POST /api/auth/register ───────────────────────────────

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req,
        CancellationToken ct)
    {
        // Check duplicate email
        var exists = await _db.Users
            .AnyAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

        if (exists)
            return Conflict(new ProblemDetails
            {
                Title  = "Email already registered.",
                Status = StatusCodes.Status409Conflict,
                Detail = $"The email '{req.Email}' is already associated with an account."
            });

        var user = new User
        {
            Email        = req.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            FirstName    = req.FirstName.Trim(),
            LastName     = req.LastName.Trim(),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token   = _jwt.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(24);

        return CreatedAtAction(nameof(Register), BuildResponse(user, token, expires));
    }

    // ── POST /api/auth/login ──────────────────────────────────

    /// <summary>Login with email and password, receive a JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req,
        CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

        // Deliberate: same error for "not found" and "wrong password" — no enumeration
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new ProblemDetails
            {
                Title  = "Invalid credentials.",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "The email or password is incorrect."
            });

        var token   = _jwt.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(24);

        return Ok(BuildResponse(user, token, expires));
    }

    // ── Helper ────────────────────────────────────────────────

    private static AuthResponse BuildResponse(User user, string token, DateTime expires) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, token, expires);
}
