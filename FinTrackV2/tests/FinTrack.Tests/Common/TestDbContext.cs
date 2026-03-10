using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Tests.Common;

public static class TestDbContext
{
    /// <summary>
    /// Each call returns a fresh in-memory database.
    /// We use a unique name per call so tests never share state.
    /// </summary>
    public static AppDbContext Create(string? name = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(opts);
    }
}
