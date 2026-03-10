using FinTrack.Application.Interfaces;
using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.UpdateWallet;

public sealed class UpdateWalletHandler
    : IRequestHandler<UpdateWalletCommand, Result<WalletResponse>>
{
    private readonly IWalletRepository _wallets;

    public UpdateWalletHandler(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Result<WalletResponse>> Handle(UpdateWalletCommand command, CancellationToken ct)
    {
        var wallet = await _wallets.GetByIdAndUserIdAsync(command.WalletId, command.UserId, ct);

        if (wallet is null)
            return Result.Failure<WalletResponse>(WalletErrors.NotFound);

        // Business rule lives on the entity
        var renameResult = wallet.Rename(command.Name);
        if (renameResult.IsFailure)
            return Result.Failure<WalletResponse>(renameResult.Error);

        await _wallets.SaveChangesAsync(ct);

        return Result.Success(CreateWalletHandler.ToResponse(wallet));
    }
}
