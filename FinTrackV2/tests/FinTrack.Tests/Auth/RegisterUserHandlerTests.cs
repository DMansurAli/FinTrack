using FinTrack.Application.Auth.Commands.RegisterUser;
using FinTrack.Domain.Errors;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Tests.Common;
using FluentAssertions;

namespace FinTrack.Tests.Auth;

public class RegisterUserHandlerTests
{
    private RegisterUserHandler CreateHandler(string dbName)
    {
        var db     = TestDbContext.Create(dbName);
        var users  = new UserRepository(db);
        var hasher = new FakePasswordHasher();
        var jwt    = new FakeJwtService();
        return new RegisterUserHandler(users, hasher, jwt);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithToken()
    {
        var handler = CreateHandler(nameof(Handle_ValidCommand_ReturnsSuccessWithToken));
        var command = new RegisterUserCommand("alice@example.com", "password123", "Alice", "Smith");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.FirstName.Should().Be("Alice");
        result.Value.AccessToken.Should().StartWith("fake-token-for-");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsEmailAlreadyExistsError()
    {
        var handler = CreateHandler(nameof(Handle_DuplicateEmail_ReturnsEmailAlreadyExistsError));
        var command = new RegisterUserCommand("alice@example.com", "password123", "Alice", "Smith");

        await handler.Handle(command, CancellationToken.None);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailAlreadyExists);
    }

    [Fact]
    public async Task Handle_EmailStoredAsLowercase()
    {
        var handler = CreateHandler(nameof(Handle_EmailStoredAsLowercase));
        var command = new RegisterUserCommand("ALICE@EXAMPLE.COM", "password123", "Alice", "Smith");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_PasswordIsHashed_NotStoredAsPlaintext()
    {
        var dbName = nameof(Handle_PasswordIsHashed_NotStoredAsPlaintext);
        var db     = TestDbContext.Create(dbName);
        var handler = new RegisterUserHandler(
            new UserRepository(db), new FakePasswordHasher(), new FakeJwtService());

        await handler.Handle(
            new RegisterUserCommand("alice@example.com", "secret", "Alice", "Smith"),
            CancellationToken.None);

        var user = db.Users.Single();
        user.PasswordHash.Should().NotBe("secret");
        user.PasswordHash.Should().Be("hashed:secret");
    }
}
