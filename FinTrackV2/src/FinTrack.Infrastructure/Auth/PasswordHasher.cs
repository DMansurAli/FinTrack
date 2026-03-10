using FinTrack.Application.Interfaces;

namespace FinTrack.Infrastructure.Auth;

/// <summary>
/// BCrypt implementation of IPasswordHasher.
/// Application layer only knows the interface — could swap to Argon2 without touching any handler.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
