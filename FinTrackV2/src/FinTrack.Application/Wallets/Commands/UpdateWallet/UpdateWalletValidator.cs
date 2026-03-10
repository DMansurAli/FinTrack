using FluentValidation;

namespace FinTrack.Application.Wallets.Commands.UpdateWallet;

public sealed class UpdateWalletValidator : AbstractValidator<UpdateWalletCommand>
{
    public UpdateWalletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required.")
            .MaximumLength(100);
    }
}
