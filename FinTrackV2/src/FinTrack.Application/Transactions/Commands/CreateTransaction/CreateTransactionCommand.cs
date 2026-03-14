using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using MediatR;

namespace FinTrack.Application.Transactions.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid WalletId,
    Guid UserId,
    TransactionType Type,
    decimal Amount,
    string Description) : IRequest<Result<TransactionResponse>>;

public record TransactionResponse(
    Guid Id,
    Guid WalletId,
    TransactionType Type,
    decimal Amount,
    string Description,
    decimal BalanceBefore,
    decimal BalanceAfter,
    DateTime CreatedAt);
