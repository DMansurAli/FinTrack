using FinTrack.Application.Interfaces;
using FinTrack.Domain.Entities;

namespace FinTrack.Tests.Common;

public class FakeJwtService : IJwtService
{
    public string GenerateToken(User user) => $"fake-token-for-{user.Id}";
}
