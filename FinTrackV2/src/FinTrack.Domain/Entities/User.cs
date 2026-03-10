namespace FinTrack.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<Wallet> Wallets => _wallets.AsReadOnly();
    private readonly List<Wallet> _wallets = [];

    // EF Core needs a parameterless constructor
    private User() { }

    /// <summary>
    /// Factory method — the only way to create a valid User.
    /// All required fields are enforced here, not scattered across the codebase.
    /// </summary>
    public static User Create(string email, string passwordHash,
                              string firstName, string lastName) =>
        new()
        {
            Email        = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName    = firstName.Trim(),
            LastName     = lastName.Trim(),
        };
}
