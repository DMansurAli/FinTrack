using FinTrack.Application.Interfaces;
using FinTrack.Application.Transactions.Commands.CreateTransaction;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Transactions.Queries.GetTransactions;

public sealed class GetTransactionsHandler
    : IRequestHandler<GetTransactionsQuery, Result<PagedResult<TransactionResponse>>>
{
    private readonly IWalletRepository _wallets;
    private readonly ITransactionRepository _transactions;

    public GetTransactionsHandler(IWalletRepository wallets, ITransactionRepository transactions)
    {
        _wallets      = wallets;
        _transactions = transactions;
    }

    public async Task<Result<PagedResult<TransactionResponse>>> Handle(
        GetTransactionsQuery query, CancellationToken ct)
    {
        var wallet = await _wallets.GetByIdAndUserIdAsync(query.WalletId, query.UserId, ct);
        if (wallet is null)
            return Result.Failure<PagedResult<TransactionResponse>>(WalletErrors.NotFound);

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (transactions, totalCount) = await _transactions
            .GetPagedByWalletIdAsync(query.WalletId, page, pageSize, ct);

        var items  = transactions.Select(CreateTransactionHandler.ToResponse).ToList();
        var result = new PagedResult<TransactionResponse>(items, page, pageSize, totalCount);

        return Result.Success(result);
    }
}
