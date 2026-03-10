using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Queries.GetWallets;

public record GetWalletsQuery(Guid UserId) : IRequest<Result<List<WalletResponse>>>;
