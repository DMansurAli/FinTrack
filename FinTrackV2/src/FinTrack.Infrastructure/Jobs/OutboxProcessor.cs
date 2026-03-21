using System.Text.Json;
using FinTrack.Application.Interfaces;
using FinTrack.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinTrack.Infrastructure.Jobs;

/// <summary>
/// Polls OutboxMessages every 5 seconds and dispatches unprocessed events via MediatR.
///
/// Why IServiceScopeFactory instead of injecting IOutboxRepository directly?
/// BackgroundService is a singleton. Repositories are scoped (per-request).
/// Singletons cannot directly hold scoped services — that would cause the scoped
/// service to outlive its intended lifetime. The factory creates a fresh scope
/// (and therefore a fresh DbContext) on each poll cycle.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    // Map type name → concrete CLR type for deserialisation
    private static readonly Dictionary<string, Type> _eventTypes = new()
    {
        [nameof(WalletCreatedEvent)]      = typeof(WalletCreatedEvent),
        [nameof(TransactionCreatedEvent)] = typeof(TransactionCreatedEvent),
    };

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started — polling every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        // Create a fresh DI scope for this poll cycle
        await using var scope = _scopeFactory.CreateAsyncScope();

        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher  = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await outboxRepo.GetUnprocessedAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogInformation(
            "OutboxProcessor: processing {Count} message(s)", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                if (!_eventTypes.TryGetValue(message.Type, out var eventType))
                {
                    _logger.LogWarning(
                        "OutboxProcessor: unknown event type '{Type}' — skipping",
                        message.Type);
                    message.MarkFailed($"Unknown event type: {message.Type}");
                    continue;
                }

                var domainEvent = (IDomainEvent?)JsonSerializer
                    .Deserialize(message.Payload, eventType);

                if (domainEvent is null)
                {
                    message.MarkFailed("Deserialisation returned null");
                    continue;
                }

                await publisher.Publish(domainEvent, ct);
                message.MarkProcessed();

                _logger.LogInformation(
                    "OutboxProcessor: dispatched {Type} ({Id})",
                    message.Type, message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OutboxProcessor: failed to process message {Id}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        await outboxRepo.SaveChangesAsync(ct);
    }
}
