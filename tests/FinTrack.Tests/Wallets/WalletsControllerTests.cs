using System.Security.Claims;
using FinTrack.Api.Controllers;
using FinTrack.Api.Data;
using FinTrack.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FinTrack.Tests.Wallets;

public class WalletsControllerTests
{
    private static (WalletsController controller, AppDbContext db) Build(Guid userId)
    {
        var db = TestDbFactory.Create();
        var controller = new WalletsController(db);

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };

        return (controller, db);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersWallets()
    {
        var aliceId = Guid.NewGuid();
        var bobId   = Guid.NewGuid();
        var (controller, db) = Build(aliceId);

        db.Wallets.AddRange(
            new Wallet { UserId = aliceId, Name = "Alice Wallet 1", Currency = "USD" },
            new Wallet { UserId = aliceId, Name = "Alice Wallet 2", Currency = "EUR" },
            new Wallet { UserId = bobId,   Name = "Bob Wallet",     Currency = "USD" }
        );
        await db.SaveChangesAsync();

        var result = await controller.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<List<WalletsController.WalletResponse>>().Subject;
        list.Should().HaveCount(2);
        list.Should().AllSatisfy(w => w.Name.Should().StartWith("Alice"));
    }

    [Fact]
    public async Task GetAll_WhenNoWallets_ReturnsEmptyList()
    {
        var (controller, _) = Build(Guid.NewGuid());

        var result = await controller.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<WalletsController.WalletResponse>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_OwnWallet_Returns200()
    {
        var userId = Guid.NewGuid();
        var (controller, db) = Build(userId);

        var wallet = new Wallet { UserId = userId, Name = "Main", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.GetById(wallet.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<WalletsController.WalletResponse>()
            .Which.Id.Should().Be(wallet.Id);
    }

    [Fact]
    public async Task GetById_AnotherUsersWallet_Returns404()
    {
        var (controller, db) = Build(Guid.NewGuid());

        var wallet = new Wallet { UserId = Guid.NewGuid(), Name = "Other", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.GetById(wallet.Id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithCorrectData()
    {
        var (controller, db) = Build(Guid.NewGuid());

        var result = await controller.Create(
            new WalletsController.CreateWalletRequest("Holiday Fund", "EUR"),
            CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<WalletsController.WalletResponse>().Subject;
        response.Name.Should().Be("Holiday Fund");
        response.Currency.Should().Be("EUR");
        response.Balance.Should().Be(0);
        db.Wallets.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_CurrencyIsUppercased()
    {
        var (controller, _) = Build(Guid.NewGuid());

        var result = await controller.Create(
            new WalletsController.CreateWalletRequest("Test", "usd"),
            CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<WalletsController.WalletResponse>()
            .Which.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Update_OwnWallet_RenamesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var (controller, db) = Build(userId);

        var wallet = new Wallet { UserId = userId, Name = "Old Name", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.Update(
            wallet.Id,
            new WalletsController.UpdateWalletRequest("New Name"),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<WalletsController.WalletResponse>()
            .Which.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Update_AnotherUsersWallet_Returns404()
    {
        var (controller, db) = Build(Guid.NewGuid());

        var wallet = new Wallet { UserId = Guid.NewGuid(), Name = "Not mine", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.Update(
            wallet.Id,
            new WalletsController.UpdateWalletRequest("Hacked"),
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_OwnWallet_Returns204AndRemovesFromDb()
    {
        var userId = Guid.NewGuid();
        var (controller, db) = Build(userId);

        var wallet = new Wallet { UserId = userId, Name = "To Delete", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.Delete(wallet.Id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        db.Wallets.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonExistentWallet_Returns404()
    {
        var (controller, _) = Build(Guid.NewGuid());

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_AnotherUsersWallet_Returns404_AndDoesNotDelete()
    {
        var (controller, db) = Build(Guid.NewGuid());

        var wallet = new Wallet { UserId = Guid.NewGuid(), Name = "Untouchable", Currency = "USD" };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var result = await controller.Delete(wallet.Id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        db.Wallets.Should().HaveCount(1);
    }
}
