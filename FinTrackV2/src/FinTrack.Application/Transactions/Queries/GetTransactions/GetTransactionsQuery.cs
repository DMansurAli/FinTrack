using FinTrack.Application.Transactions.Commands.CreateTransaction;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(
    Guid WalletId,
    Guid UserId,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<TransactionResponse>>>;
