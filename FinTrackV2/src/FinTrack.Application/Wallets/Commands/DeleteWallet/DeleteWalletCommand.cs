using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.DeleteWallet;

public record DeleteWalletCommand(Guid WalletId, Guid UserId) : IRequest<Result>;
