using FinTrack.Application.Transactions.Commands.CreateTransaction;
using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Tests.Common;
using FinTrack.Tests.Wallets;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Tests.Integration;

/// <summary>
/// Integration tests against a real PostgreSQL instance (via TestContainers).
/// These tests verify things unit tests cannot:
///   - Actual SQL queries execute correctly
///   - Migrations are valid
///   - Constraints (unique email, FK cascade) work as expected
///   - Balance is persisted correctly after transactions
/// </summary>
[Collection("Integration")]
public class WalletIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public WalletIntegrationTests(DatabaseFixture fixture)
        => _fixture = fixture;

    private async Task<User> CreateUserAsync(string email = "test@example.com")
    {
        var user = User.Create(email, "hashed:password", "Test", "User");
        await _fixture.Db.Users.AddAsync(user);
        await _fixture.Db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateWallet_PersistsToDatabase()
    {
        var user       = await CreateUserAsync($"wallet-{Guid.NewGuid()}@test.com");
        var wallets    = new WalletRepository(_fixture.Db);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler    = new CreateWalletHandler(wallets, dispatcher);

        var result = await handler.Handle(
            new CreateWalletCommand(user.Id, "Savings", "USD"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Query the real database to verify it was actually saved
        var saved = await _fixture.Db.Wallets
            .FirstOrDefaultAsync(w => w.Id == result.Value.Id);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Savings");
        saved.Currency.Should().Be("USD");
        saved.Balance.Should().Be(0);
    }

    [Fact]
    public async Task Deposit_UpdatesBalanceInDatabase()
    {
        var user       = await CreateUserAsync($"deposit-{Guid.NewGuid()}@test.com");
        var wallets    = new WalletRepository(_fixture.Db);
        var dispatcher = new FakeDomainEventDispatcher();
        var txRepo     = new TransactionRepository(_fixture.Db);

        // Create wallet
        var createHandler = new CreateWalletHandler(wallets, dispatcher);
        var wallet = await createHandler.Handle(
            new CreateWalletCommand(user.Id, "Main", "USD"),
            CancellationToken.None);

        // Deposit
        var txHandler = new CreateTransactionHandler(wallets, txRepo, dispatcher);
        var result = await txHandler.Handle(
            new CreateTransactionCommand(
                wallet.Value.Id, user.Id,
                TransactionType.Deposit, 500, "Salary"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BalanceAfter.Should().Be(500);

        // Verify in real DB
        var saved = await _fixture.Db.Wallets
            .FirstAsync(w => w.Id == wallet.Value.Id);
        saved.Balance.Should().Be(500);

        var tx = await _fixture.Db.Transactions
            .FirstAsync(t => t.WalletId == wallet.Value.Id);
        tx.Amount.Should().Be(500);
        tx.Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_DoesNotPersistTransaction()
    {
        var user       = await CreateUserAsync($"withdraw-{Guid.NewGuid()}@test.com");
        var wallets    = new WalletRepository(_fixture.Db);
        var dispatcher = new FakeDomainEventDispatcher();
        var txRepo     = new TransactionRepository(_fixture.Db);

        var createHandler = new CreateWalletHandler(wallets, dispatcher);
        var wallet = await createHandler.Handle(
            new CreateWalletCommand(user.Id, "Empty Wallet", "USD"),
            CancellationToken.None);

        var txHandler = new CreateTransactionHandler(wallets, txRepo, dispatcher);
        var result = await txHandler.Handle(
            new CreateTransactionCommand(
                wallet.Value.Id, user.Id,
                TransactionType.Withdrawal, 100, "ATM"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InsufficientFunds");

        // Verify nothing was saved
        var txCount = await _fixture.Db.Transactions
            .CountAsync(t => t.WalletId == wallet.Value.Id);
        txCount.Should().Be(0);

        // Balance still zero
        var saved = await _fixture.Db.Wallets
            .FirstAsync(w => w.Id == wallet.Value.Id);
        saved.Balance.Should().Be(0);
    }

    [Fact]
    public async Task DeleteWallet_CascadesTransactions()
    {
        var user       = await CreateUserAsync($"cascade-{Guid.NewGuid()}@test.com");
        var wallets    = new WalletRepository(_fixture.Db);
        var dispatcher = new FakeDomainEventDispatcher();
        var txRepo     = new TransactionRepository(_fixture.Db);

        var createHandler = new CreateWalletHandler(wallets, dispatcher);
        var wallet = await createHandler.Handle(
            new CreateWalletCommand(user.Id, "To Delete", "USD"),
            CancellationToken.None);

        // Add a transaction
        var txHandler = new CreateTransactionHandler(wallets, txRepo, dispatcher);
        await txHandler.Handle(
            new CreateTransactionCommand(
                wallet.Value.Id, user.Id,
                TransactionType.Deposit, 100, "Test"),
            CancellationToken.None);

        // Delete the wallet directly
        var dbWallet = await _fixture.Db.Wallets
            .FirstAsync(w => w.Id == wallet.Value.Id);
        _fixture.Db.Wallets.Remove(dbWallet);
        await _fixture.Db.SaveChangesAsync();

        // Transactions should be cascade deleted
        var txCount = await _fixture.Db.Transactions
            .CountAsync(t => t.WalletId == wallet.Value.Id);
        txCount.Should().Be(0);
    }
}
