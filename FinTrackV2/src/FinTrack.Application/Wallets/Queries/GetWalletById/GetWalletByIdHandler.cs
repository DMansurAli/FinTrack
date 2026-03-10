using FinTrack.Application.Interfaces;
using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Wallets.Queries.GetWalletById;

public sealed class GetWalletByIdHandler : IRequestHandler<GetWalletByIdQuery, Result<WalletResponse>>
{
    private readonly IWalletRepository _wallets;

    public GetWalletByIdHandler(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Result<WalletResponse>> Handle(GetWalletByIdQuery query, CancellationToken ct)
    {
        var wallet = await _wallets.GetByIdAndUserIdAsync(query.WalletId, query.UserId, ct);

        if (wallet is null)
            return Result.Failure<WalletResponse>(WalletErrors.NotFound);

        return Result.Success(CreateWalletHandler.ToResponse(wallet));
    }
}
