using System.Security.Claims;
using FinTrack.Application.Wallets.Commands.CreateWallet;
using FinTrack.Application.Wallets.Commands.DeleteWallet;
using FinTrack.Application.Wallets.Commands.UpdateWallet;
using FinTrack.Application.Wallets.Queries.GetWalletById;
using FinTrack.Application.Wallets.Queries.GetWallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class WalletsController : ControllerBase
{
    private readonly ISender _sender;

    public WalletsController(ISender sender) => _sender = sender;

    private Guid UserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public record CreateWalletRequest(string Name, string Currency);
    public record UpdateWalletRequest(string Name);

    /// <summary>Get all wallets for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetWalletsQuery(UserId), ct);
        return Ok(result.Value);
    }

    /// <summary>Get a single wallet by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetWalletByIdQuery(id, UserId), ct);

        return result.IsFailure
            ? NotFound(new { result.Error.Code, result.Error.Message })
            : Ok(result.Value);
    }

    /// <summary>Create a new wallet.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWalletRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(
            new CreateWalletCommand(UserId, req.Name, req.Currency), ct);

        return result.IsFailure
            ? BadRequest(new { result.Error.Code, result.Error.Message })
            : CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Rename a wallet.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWalletRequest req, CancellationToken ct)
    {
        var result = await _sender.Send(
            new UpdateWalletCommand(id, UserId, req.Name), ct);

        return result.IsFailure
            ? NotFound(new { result.Error.Code, result.Error.Message })
            : Ok(result.Value);
    }

    /// <summary>Delete a wallet.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteWalletCommand(id, UserId), ct);

        return result.IsFailure
            ? NotFound(new { result.Error.Code, result.Error.Message })
            : NoContent();
    }
}
