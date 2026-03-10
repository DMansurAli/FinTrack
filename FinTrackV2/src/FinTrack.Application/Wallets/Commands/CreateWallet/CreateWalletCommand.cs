using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.CreateWallet;

public record CreateWalletCommand(Guid UserId, string Name, string Currency)
    : IRequest<Result<WalletResponse>>;

public record WalletResponse(Guid Id, string Name, string Currency,
    decimal Balance, DateTime CreatedAt, DateTime UpdatedAt);
