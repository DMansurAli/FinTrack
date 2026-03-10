using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
