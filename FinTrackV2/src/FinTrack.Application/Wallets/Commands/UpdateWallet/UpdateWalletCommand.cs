using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.UpdateWallet;

public record UpdateWalletCommand(Guid WalletId, Guid UserId, string Name)
    : IRequest<Result<FinTrack.Application.Wallets.Commands.CreateWallet.WalletResponse>>;
