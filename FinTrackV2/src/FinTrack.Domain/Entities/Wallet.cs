using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Errors;
using FinTrack.Domain.Events;

namespace FinTrack.Domain.Entities;

public class Wallet : AggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User? User { get; private set; }

    private readonly List<Transaction> _transactions = [];
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    private Wallet() { }

    public static Wallet Create(Guid userId, string name, string currency)
    {
        var wallet = new Wallet
        {
            UserId   = userId,
            Name     = name.Trim(),
            Currency = currency.ToUpperInvariant(),
        };

        // Raise event — something important happened
        wallet.RaiseDomainEvent(new WalletCreatedEvent(
            wallet.Id, userId, wallet.Name, wallet.Currency));

        return wallet;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(WalletErrors.InvalidName);

        Name      = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Business rule: amount must be positive.
    /// Records the transaction and updates the balance atomically.
    /// </summary>
    public Result<Transaction> Deposit(decimal amount, string description = "Deposit")
    {
        if (amount <= 0)
            return Result.Failure<Transaction>(TransactionErrors.InvalidAmount);

        var balanceBefore = Balance;
        Balance          += amount;
        UpdatedAt         = DateTime.UtcNow;

        var transaction = Transaction.Create(
            Id, TransactionType.Deposit, amount,
            description, balanceBefore, Balance);

        _transactions.Add(transaction);

        RaiseDomainEvent(new TransactionCreatedEvent(
            transaction.Id, Id, UserId,
            TransactionType.Deposit, amount, Balance));

        return Result.Success(transaction);
    }

    /// <summary>
    /// Business rule: amount must be positive AND balance must be sufficient.
    /// </summary>
    public Result<Transaction> Withdraw(decimal amount, string description = "Withdrawal")
    {
        if (amount <= 0)
            return Result.Failure<Transaction>(TransactionErrors.InvalidAmount);

        if (Balance < amount)
            return Result.Failure<Transaction>(TransactionErrors.InsufficientFunds);

        var balanceBefore = Balance;
        Balance          -= amount;
        UpdatedAt         = DateTime.UtcNow;

        var transaction = Transaction.Create(
            Id, TransactionType.Withdrawal, amount,
            description, balanceBefore, Balance);

        _transactions.Add(transaction);

        RaiseDomainEvent(new TransactionCreatedEvent(
            transaction.Id, Id, UserId,
            TransactionType.Withdrawal, amount, Balance));

        return Result.Success(transaction);
    }
}
