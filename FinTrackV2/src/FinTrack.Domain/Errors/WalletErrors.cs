using FinTrack.Domain.Common;

namespace FinTrack.Domain.Errors;

public static class WalletErrors
{
    public static readonly Error NotFound = new(
        "Wallet.NotFound",
        "The wallet was not found or does not belong to you.");

    public static readonly Error InvalidName = new(
        "Wallet.InvalidName",
        "Wallet name cannot be empty.");
}
