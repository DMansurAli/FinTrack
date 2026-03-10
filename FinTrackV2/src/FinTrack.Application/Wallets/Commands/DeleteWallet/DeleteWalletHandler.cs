using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Errors;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.DeleteWallet;

public sealed class DeleteWalletHandler : IRequestHandler<DeleteWalletCommand, Result>
{
    private readonly IWalletRepository _wallets;

    public DeleteWalletHandler(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Result> Handle(DeleteWalletCommand command, CancellationToken ct)
    {
        var wallet = await _wallets.GetByIdAndUserIdAsync(command.WalletId, command.UserId, ct);

        if (wallet is null)
            return Result.Failure(WalletErrors.NotFound);

        await _wallets.RemoveAsync(wallet, ct);
        await _wallets.SaveChangesAsync(ct);

        return Result.Success();
    }
}
