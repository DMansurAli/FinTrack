using System.Security.Claims;
using FinTrack.Application.Transactions.Commands.CreateTransaction;
using FinTrack.Application.Transactions.Queries.GetTransactions;
using FinTrack.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/wallets/{walletId:guid}/transactions")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender) => _sender = sender;

    private Guid UserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public record CreateTransactionRequest(TransactionType Type, decimal Amount, string Description);

    /// <summary>Get paginated transactions for a wallet.</summary>
    /// <param name="walletId">The wallet ID.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100).</param>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid walletId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new GetTransactionsQuery(walletId, UserId, page, pageSize), ct);

        return result.IsFailure
            ? NotFound(new { result.Error.Code, result.Error.Message })
            : Ok(result.Value);
    }

    /// <summary>Create a deposit or withdrawal.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid walletId, [FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(
            new CreateTransactionCommand(walletId, UserId, req.Type, req.Amount, req.Description), ct);

        return result.IsFailure
            ? BadRequest(new { result.Error.Code, result.Error.Message })
            : CreatedAtAction(nameof(GetAll), new { walletId }, result.Value);
    }
}
