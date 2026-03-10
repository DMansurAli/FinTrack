using FinTrack.Application.Interfaces;

namespace FinTrack.Tests.Common;

/// <summary>
/// BCrypt takes ~300ms per hash. In tests we want instant results.
/// This fake just stores the password as-is so tests stay fast.
/// </summary>
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}
