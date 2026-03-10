using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Application.Wallets.Commands.DeleteWallet;
using FinTrack.Application.Wallets.Commands.UpdateWallet;
using FinTrack.Application.Wallets.Queries.GetWalletById;
using FinTrack.Application.Wallets.Queries.GetWallets;
using FinTrack.Domain.Common;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Errors;
using FinTrack.Infrastructure.Persistence;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Tests.Common;
using FluentAssertions;

namespace FinTrack.Tests.Wallets;

public class WalletHandlerTests
{
    private readonly AppDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    private readonly CreateWalletHandler  _createHandler;
    private readonly UpdateWalletHandler  _updateHandler;
    private readonly DeleteWalletHandler  _deleteHandler;
    private readonly GetWalletsHandler    _getallHandler;
    private readonly GetWalletByIdHandler _getByIdHandler;

    public WalletHandlerTests()
    {
        _db = TestDbContext.Create();

        _db.Users.Add(User.Create("owner@test.com", "hash", "Owner", "One"));
        _db.Users.Add(User.Create("other@test.com", "hash", "Other", "Two"));
        _db.SaveChanges();

        var wallets     = new WalletRepository(_db);
        _createHandler  = new CreateWalletHandler(wallets);
        _updateHandler  = new UpdateWalletHandler(wallets);
        _deleteHandler  = new DeleteWalletHandler(wallets);
        _getallHandler  = new GetWalletsHandler(wallets);
        _getByIdHandler = new GetWalletByIdHandler(wallets);
    }

    private Task<Result<WalletResponse>> CreateWallet(
        string name = "My Wallet", string currency = "USD", Guid? userId = null) =>
        _createHandler.Handle(
            new CreateWalletCommand(userId ?? _userId, name, currency),
            CancellationToken.None);

    [Fact]
    public async Task Create_ValidCommand_ReturnsWallet()
    {
        var result = await CreateWallet("Savings", "USD");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Savings");
        result.Value.Currency.Should().Be("USD");
        result.Value.Balance.Should().Be(0);
    }

    [Fact]
    public async Task Create_CurrencyStoredAsUppercase()
    {
        var result = await CreateWallet("Savings", "usd");
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetAll_OnlyReturnsCurrentUsersWallets()
    {
        await CreateWallet("Mine",   userId: _userId);
        await CreateWallet("Mine2",  userId: _userId);
        await CreateWallet("Theirs", userId: _otherUserId);

        var result = await _getallHandler.Handle(
            new GetWalletsQuery(_userId), CancellationToken.None);

        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(w => w.Name.Should().NotBe("Theirs"));
    }

    [Fact]
    public async Task GetById_OwnWallet_ReturnsWallet()
    {
        var created = await CreateWallet();
        var result  = await _getByIdHandler.Handle(
            new GetWalletByIdQuery(created.Value.Id, _userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetById_OtherUsersWallet_ReturnsNotFound()
    {
        var created = await CreateWallet(userId: _otherUserId);

        var result = await _getByIdHandler.Handle(
            new GetWalletByIdQuery(created.Value.Id, _userId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WalletErrors.NotFound);
    }

    [Fact]
    public async Task Update_ValidName_RenamesWallet()
    {
        var created = await CreateWallet("Old Name");

        var result = await _updateHandler.Handle(
            new UpdateWalletCommand(created.Value.Id, _userId, "New Name"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Update_OtherUsersWallet_ReturnsNotFound()
    {
        var created = await CreateWallet(userId: _otherUserId);

        var result = await _updateHandler.Handle(
            new UpdateWalletCommand(created.Value.Id, _userId, "Hack"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WalletErrors.NotFound);
    }

    [Fact]
    public async Task Delete_OwnWallet_Succeeds()
    {
        var created = await CreateWallet();

        var result = await _deleteHandler.Handle(
            new DeleteWalletCommand(created.Value.Id, _userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.Wallets.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_OtherUsersWallet_ReturnsNotFound()
    {
        var created = await CreateWallet(userId: _otherUserId);

        var result = await _deleteHandler.Handle(
            new DeleteWalletCommand(created.Value.Id, _userId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _db.Wallets.Should().HaveCount(1);
    }
}
