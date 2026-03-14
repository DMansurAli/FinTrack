using FinTrack.Application.Interfaces;
using FinTrack.Application.Transactions.Commands.CreateTransaction;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Transactions.Queries.GetTransactions;

public sealed class GetTransactionsHandler
    : IRequestHandler<GetTransactionsQuery, Result<List<TransactionResponse>>>
{
    private readonly IWalletRepository _wallets;
    private readonly ITransactionRepository _transactions;

    public GetTransactionsHandler(IWalletRepository wallets, ITransactionRepository transactions)
    {
        _wallets      = wallets;
        _transactions = transactions;
    }

    public async Task<Result<List<TransactionResponse>>> Handle(
        GetTransactionsQuery query, CancellationToken ct)
    {
        // Verify the wallet belongs to this user first
        var wallet = await _wallets.GetByIdAndUserIdAsync(query.WalletId, query.UserId, ct);
        if (wallet is null)
            return Result.Failure<List<TransactionResponse>>(WalletErrors.NotFound);

        var transactions = await _transactions.GetByWalletIdAsync(query.WalletId, ct);
        return Result.Success(transactions.Select(CreateTransactionHandler.ToResponse).ToList());
    }
}
