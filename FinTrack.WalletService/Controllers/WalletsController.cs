using System.Security.Claims;
using FinTrack.Contracts.Messages;
using FinTrack.WalletService.Data;
using FinTrack.WalletService.Models;
using FinTrack.WalletService.Services;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WalletService.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    private readonly WalletDbContext     _db;
    private readonly IdentityGrpcClient  _identity;
    private readonly IPublishEndpoint    _bus;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(
        WalletDbContext db,
        IdentityGrpcClient identity,
        IPublishEndpoint bus,
        ILogger<WalletsController> logger)
    {
        _db       = db;
        _identity = identity;
        _bus      = bus;
        _logger   = logger;
    }

    private Guid UserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public record CreateWalletRequest(string Name, string Currency);
    public record CreateTransactionRequest(string Type, decimal Amount, string Description);

    // ── GET /api/wallets ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var wallets = await _db.Wallets
            .Where(w => w.UserId == UserId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        return Ok(wallets);
    }

    // ── GET /api/wallets/{id} ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId, ct);

        return wallet is null
            ? NotFound(new { code = "Wallet.NotFound", message = "Wallet not found." })
            : Ok(wallet);
    }

    // ── POST /api/wallets ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletRequest req, CancellationToken ct)
    {
        // gRPC call to IdentityService — verify the user actually exists
        var user = await _identity.GetUserAsync(UserId, ct);
        if (user is null)
            return NotFound(new { code = "User.NotFound", message = "User not found." });

        var wallet = new Wallet
        {
            UserId   = UserId,
            Name     = req.Name.Trim(),
            Currency = req.Currency.ToUpperInvariant(),
        };

        await _db.Wallets.AddAsync(wallet, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Wallet {WalletId} created for user {UserId}", wallet.Id, UserId);

        return CreatedAtAction(nameof(GetById), new { id = wallet.Id }, wallet);
    }

    // ── POST /api/wallets/{id}/transactions ───────────────────────────────
    [HttpPost("{id:guid}/transactions")]
    public async Task<IActionResult> CreateTransaction(
        Guid id, [FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId, ct);

        if (wallet is null)
            return NotFound(new { code = "Wallet.NotFound", message = "Wallet not found." });

        var type = req.Type.Trim();

        if (type == "Withdrawal" && wallet.Balance < req.Amount)
            return BadRequest(new { code    = "Transaction.InsufficientFunds",
                                    message = "Insufficient funds." });

        var balanceBefore = wallet.Balance;
        wallet.Balance  = type == "Deposit"
            ? wallet.Balance + req.Amount
            : wallet.Balance - req.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var tx = new WalletTransaction
        {
            WalletId      = wallet.Id,
            Type          = type,
            Amount        = req.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter  = wallet.Balance,
            Description   = req.Description,
        };

        await _db.Transactions.AddAsync(tx, ct);
        await _db.SaveChangesAsync(ct);

        // gRPC — get user email for the notification message
        var user = await _identity.GetUserAsync(UserId, ct);

        // ── Publish to RabbitMQ via MassTransit ───────────────────────────
        // NotifyService subscribes to this message and creates notifications + sends email.
        // WalletService has ZERO knowledge of NotifyService — complete decoupling.
        await _bus.Publish(new TransactionCreatedMessage
        {
            TransactionId = tx.Id,
            WalletId      = wallet.Id,
            UserId        = UserId,
            UserEmail     = user?.Email     ?? string.Empty,
            UserFirstName = user?.FirstName ?? string.Empty,
            Type          = type,
            Amount        = req.Amount,
            BalanceAfter  = wallet.Balance,
            OccurredAt    = tx.CreatedAt,
        }, ct);

        _logger.LogInformation(
            "Transaction {TxId} published to RabbitMQ for wallet {WalletId}",
            tx.Id, wallet.Id);

        return CreatedAtAction(nameof(GetAll), new { id = wallet.Id }, tx);
    }

    // ── GET /api/wallets/{id}/transactions ────────────────────────────────
    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId, ct);

        if (wallet is null)
            return NotFound(new { code = "Wallet.NotFound", message = "Wallet not found." });

        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _db.Transactions.CountAsync(t => t.WalletId == id, ct);
        var items = await _db.Transactions
            .Where(t => t.WalletId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new { items, page, pageSize, totalCount = total });
    }
}
