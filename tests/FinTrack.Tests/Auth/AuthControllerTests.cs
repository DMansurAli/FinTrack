using FinTrack.Api.Controllers;
using FinTrack.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FinTrack.Tests.Auth;

public class AuthControllerTests
{
    private AuthController BuildController(out Api.Data.AppDbContext db)
    {
        db = TestDbFactory.Create();
        var jwt = new JwtService(TestJwt.BuildConfig());
        return new AuthController(db, jwt);
    }

    [Fact]
    public async Task Register_WithValidData_Returns201AndToken()
    {
        var controller = BuildController(out _);
        var request = new AuthController.RegisterRequest(
            "alice@test.com", "Password123", "Alice", "Smith");

        var result = await controller.Register(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<AuthController.AuthResponse>().Subject;
        response.Email.Should().Be("alice@test.com");
        response.FirstName.Should().Be("Alice");
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercase()
    {
        var controller = BuildController(out var db);
        var request = new AuthController.RegisterRequest(
            "Alice@TEST.COM", "Password123", "Alice", "Smith");

        await controller.Register(request, CancellationToken.None);

        var user = db.Users.Single();
        user.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var controller = BuildController(out _);
        var request = new AuthController.RegisterRequest(
            "alice@test.com", "Password123", "Alice", "Smith");

        await controller.Register(request, CancellationToken.None);
        var result = await controller.Register(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>()
            .Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Register_HashedPasswordIsNotPlaintext()
    {
        var controller = BuildController(out var db);
        var request = new AuthController.RegisterRequest(
            "bob@test.com", "MyPlainPassword", "Bob", "Jones");

        await controller.Register(request, CancellationToken.None);

        var user = db.Users.Single();
        user.PasswordHash.Should().NotBe("MyPlainPassword");
        user.PasswordHash.Should().StartWith("$2");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndToken()
    {
        var controller = BuildController(out _);

        await controller.Register(
            new AuthController.RegisterRequest(
                "carol@test.com", "Correct1Pass", "Carol", "White"),
            CancellationToken.None);

        var result = await controller.Login(
            new AuthController.LoginRequest("carol@test.com", "Correct1Pass"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthController.AuthResponse>().Subject;
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.Email.Should().Be("carol@test.com");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var controller = BuildController(out _);

        await controller.Register(
            new AuthController.RegisterRequest(
                "dan@test.com", "CorrectPass1", "Dan", "Brown"),
            CancellationToken.None);

        var result = await controller.Login(
            new AuthController.LoginRequest("dan@test.com", "WrongPassword"),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var controller = BuildController(out _);

        var result = await controller.Login(
            new AuthController.LoginRequest("ghost@test.com", "AnyPass123"),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveForEmail()
    {
        var controller = BuildController(out _);

        await controller.Register(
            new AuthController.RegisterRequest(
                "eve@test.com", "Password123", "Eve", "Davis"),
            CancellationToken.None);

        var result = await controller.Login(
            new AuthController.LoginRequest("EVE@TEST.COM", "Password123"),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
