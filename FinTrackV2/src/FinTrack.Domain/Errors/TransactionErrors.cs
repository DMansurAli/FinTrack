using FinTrack.Domain.Common;

namespace FinTrack.Domain.Errors;

public static class TransactionErrors
{
    public static readonly Error InsufficientFunds = new(
        "Transaction.InsufficientFunds",
        "Insufficient funds for this withdrawal.");

    public static readonly Error InvalidAmount = new(
        "Transaction.InvalidAmount",
        "Transaction amount must be greater than zero.");

    public static readonly Error NotFound = new(
        "Transaction.NotFound",
        "The transaction was not found.");
}
