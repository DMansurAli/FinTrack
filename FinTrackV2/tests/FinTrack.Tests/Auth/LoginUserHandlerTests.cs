using FinTrack.Application.Auth.Commands.LoginUser;
using FinTrack.Application.Auth.Commands.RegisterUser;
using FinTrack.Domain.Errors;
using FinTrack.Infrastructure.Persistence;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Tests.Common;
using FluentAssertions;

namespace FinTrack.Tests.Auth;

public class LoginUserHandlerTests
{
    private readonly AppDbContext _db;
    private readonly LoginUserHandler _loginHandler;
    private readonly RegisterUserHandler _registerHandler;

    public LoginUserHandlerTests()
    {
        _db = TestDbContext.Create();
        var users  = new UserRepository(_db);
        var hasher = new FakePasswordHasher();
        var jwt    = new FakeJwtService();
        _loginHandler    = new LoginUserHandler(users, hasher, jwt);
        _registerHandler = new RegisterUserHandler(users, hasher, jwt);
    }

    private Task RegisterAlice() =>
        _registerHandler.Handle(
            new RegisterUserCommand("alice@example.com", "password123", "Alice", "Smith"),
            CancellationToken.None);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        await RegisterAlice();

        var result = await _loginHandler.Handle(
            new LoginUserCommand("alice@example.com", "password123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        await RegisterAlice();

        var result = await _loginHandler.Handle(
            new LoginUserCommand("alice@example.com", "wrongpassword"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        var result = await _loginHandler.Handle(
            new LoginUserCommand("nobody@example.com", "password123"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_EmailIsCaseInsensitive()
    {
        await RegisterAlice();

        var result = await _loginHandler.Handle(
            new LoginUserCommand("ALICE@EXAMPLE.COM", "password123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
