using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FinTrack.Api.Data;
using FinTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                      // All endpoints require a valid JWT
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    private readonly AppDbContext _db;

    public WalletsController(AppDbContext db) => _db = db;

    // ── DTOs ──────────────────────────────────────────────────

    public record CreateWalletRequest(
        [Required, MaxLength(100)]         string Name,
        [Required, StringLength(3, MinimumLength = 3)] string Currency);

    public record UpdateWalletRequest(
        [Required, MaxLength(100)] string Name);

    public record WalletResponse(
        Guid     Id,
        string   Name,
        string   Currency,
        decimal  Balance,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    // ── GET /api/wallets ──────────────────────────────────────

    /// <summary>Get all wallets for the logged-in user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WalletResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetUserId();

        var wallets = await _db.Wallets
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => ToResponse(w))
            .ToListAsync(ct);

        return Ok(wallets);
    }

    // ── GET /api/wallets/{id} ─────────────────────────────────

    /// <summary>Get a single wallet by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var wallet = await FindOwnedWalletAsync(id, ct);
        if (wallet is null) return WalletNotFound(id);

        return Ok(ToResponse(wallet));
    }

    // ── POST /api/wallets ─────────────────────────────────────

    /// <summary>Create a new wallet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWalletRequest req,
        CancellationToken ct)
    {
        var wallet = new Wallet
        {
            UserId   = GetUserId(),
            Name     = req.Name.Trim(),
            Currency = req.Currency.ToUpperInvariant(),
        };

        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = wallet.Id }, ToResponse(wallet));
    }

    // ── PUT /api/wallets/{id} ─────────────────────────────────

    /// <summary>Rename a wallet.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWalletRequest req,
        CancellationToken ct)
    {
        var wallet = await FindOwnedWalletAsync(id, ct);
        if (wallet is null) return WalletNotFound(id);

        wallet.Name      = req.Name.Trim();
        wallet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(wallet));
    }

    // ── DELETE /api/wallets/{id} ──────────────────────────────

    /// <summary>Delete a wallet.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var wallet = await FindOwnedWalletAsync(id, ct);
        if (wallet is null) return WalletNotFound(id);

        _db.Wallets.Remove(wallet);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);

    private async Task<Wallet?> FindOwnedWalletAsync(Guid id, CancellationToken ct) =>
        await _db.Wallets.FirstOrDefaultAsync(
            w => w.Id == id && w.UserId == GetUserId(), ct);

    private IActionResult WalletNotFound(Guid id) =>
        NotFound(new ProblemDetails
        {
            Title  = "Wallet not found.",
            Status = StatusCodes.Status404NotFound,
            Detail = $"Wallet '{id}' does not exist or does not belong to you."
        });

    private static WalletResponse ToResponse(Wallet w) =>
        new(w.Id, w.Name, w.Currency, w.Balance, w.CreatedAt, w.UpdatedAt);
}
