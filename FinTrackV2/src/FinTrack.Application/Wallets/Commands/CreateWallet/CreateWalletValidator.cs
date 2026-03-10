using FluentValidation;

namespace FinTrack.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters (e.g. USD).");
    }
}
