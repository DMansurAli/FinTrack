using FinTrack.Application.Interfaces;
using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Common;
using MediatR;

namespace FinTrack.Application.Wallets.Queries.GetWallets;

public sealed class GetWalletsHandler : IRequestHandler<GetWalletsQuery, Result<List<WalletResponse>>>
{
    private readonly IWalletRepository _wallets;

    public GetWalletsHandler(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Result<List<WalletResponse>>> Handle(GetWalletsQuery query, CancellationToken ct)
    {
        var wallets = await _wallets.GetAllByUserIdAsync(query.UserId, ct);
        var response = wallets.Select(CreateWalletHandler.ToResponse).ToList();
        return Result.Success(response);
    }
}
