using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(Guid WalletId, Guid UserId)
    : IRequest<Result<List<FinTrack.Application.Transactions.Commands.CreateTransaction.TransactionResponse>>>;
