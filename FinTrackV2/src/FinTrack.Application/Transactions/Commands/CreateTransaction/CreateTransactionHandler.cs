using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionHandler
    : IRequestHandler<CreateTransactionCommand, Result<TransactionResponse>>
{
    private readonly IWalletRepository _wallets;
    private readonly ITransactionRepository _transactions;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateTransactionHandler(
        IWalletRepository wallets,
        ITransactionRepository transactions,
        IDomainEventDispatcher dispatcher)
    {
        _wallets      = wallets;
        _transactions = transactions;
        _dispatcher   = dispatcher;
    }

    public async Task<Result<TransactionResponse>> Handle(
        CreateTransactionCommand command, CancellationToken ct)
    {
        // 1. Load the wallet — must belong to the requesting user
        var wallet = await _wallets.GetByIdAndUserIdAsync(command.WalletId, command.UserId, ct);
        if (wallet is null)
            return Result.Failure<TransactionResponse>(WalletErrors.NotFound);

        // 2. Apply business rule on the entity — entity decides if it's valid
        var result = command.Type switch
        {
            TransactionType.Deposit    => wallet.Deposit(command.Amount, command.Description),
            TransactionType.Withdrawal => wallet.Withdraw(command.Amount, command.Description),
            _ => Result.Failure<Domain.Entities.Transaction>(TransactionErrors.NotFound)
        };

        if (result.IsFailure)
            return Result.Failure<TransactionResponse>(result.Error);

        // 3. Persist both the updated wallet balance and the new transaction
        await _transactions.AddAsync(result.Value, ct);
        await _wallets.SaveChangesAsync(ct);

        // 4. Dispatch domain events AFTER successful save
        await _dispatcher.DispatchAsync(wallet.DomainEvents, ct);
        wallet.ClearDomainEvents();

        return Result.Success(ToResponse(result.Value));
    }

    internal static TransactionResponse ToResponse(Domain.Entities.Transaction t) =>
        new(t.Id, t.WalletId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.CreatedAt);
}
