using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Queries.GetWalletById;

public record GetWalletByIdQuery(Guid WalletId, Guid UserId) : IRequest<Result<WalletResponse>>;
