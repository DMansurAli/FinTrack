using FinTrack.Application.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Entities;
using MediatR;

namespace FinTrack.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletHandler : IRequestHandler<CreateWalletCommand, Result<WalletResponse>>
{
    private readonly IWalletRepository _wallets;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateWalletHandler(IWalletRepository wallets, IDomainEventDispatcher dispatcher)
    {
        _wallets    = wallets;
        _dispatcher = dispatcher;
    }

    public async Task<Result<WalletResponse>> Handle(CreateWalletCommand command, CancellationToken ct)
    {
        var wallet = Wallet.Create(command.UserId, command.Name, command.Currency);

        await _wallets.AddAsync(wallet, ct);
        await _wallets.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(wallet.DomainEvents, ct);
        wallet.ClearDomainEvents();

        return Result.Success(ToResponse(wallet));
    }

    internal static WalletResponse ToResponse(Wallet w) =>
        new(w.Id, w.Name, w.Currency, w.Balance, w.CreatedAt, w.UpdatedAt);
}
