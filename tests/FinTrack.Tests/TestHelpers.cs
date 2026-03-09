using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinTrack.Api.Data;
using FinTrack.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.Tests;

public static class TestDbFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}

public static class TestJwt
{
    public const string SecretKey  = "test-secret-key-that-is-long-enough-32chars";
    public const string Issuer     = "FinTrack";
    public const string Audience   = "FinTrack.Users";

    public static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"]    = SecretKey,
                ["Jwt:Issuer"]      = Issuer,
                ["Jwt:Audience"]    = Audience,
                ["Jwt:ExpiryHours"] = "24"
            })
            .Build();

    public static string GenerateFor(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,       user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier,         user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,     user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
        };

        var token = new JwtSecurityToken(
            issuer:   Issuer,
            audience: Audience,
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
