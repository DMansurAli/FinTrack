using FinTrack.Domain.Entities;
using FluentAssertions;

namespace FinTrack.Tests.Outbox;

public class OutboxMessageTests
{
    [Fact]
    public void Create_SetsTypeAndPayload()
    {
        var msg = OutboxMessage.Create("TransactionCreatedEvent", "{\"amount\":500}");

        msg.Type.Should().Be("TransactionCreatedEvent");
        msg.Payload.Should().Be("{\"amount\":500}");
        msg.ProcessedAt.Should().BeNull();
        msg.Error.Should().BeNull();
    }

    [Fact]
    public void MarkProcessed_SetsProcessedAt()
    {
        var msg = OutboxMessage.Create("WalletCreatedEvent", "{}");

        msg.MarkProcessed();

        msg.ProcessedAt.Should().NotBeNull();
        msg.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkFailed_SetsErrorAndProcessedAt()
    {
        var msg = OutboxMessage.Create("TransactionCreatedEvent", "{}");

        msg.MarkFailed("Deserialisation failed");

        msg.Error.Should().Be("Deserialisation failed");
        msg.ProcessedAt.Should().NotBeNull(); // prevents infinite retry
    }

    [Fact]
    public void NotificationMessage_MarkRead_SetsReadAt()
    {
        var notif = NotificationMessage.Create(
            Guid.NewGuid(), "Deposit received", "500.00 deposited");

        notif.IsRead.Should().BeFalse();

        notif.MarkRead();

        notif.IsRead.Should().BeTrue();
        notif.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void NotificationMessage_MarkRead_IsIdempotent()
    {
        var notif = NotificationMessage.Create(Guid.NewGuid(), "Title", "Body");
        notif.MarkRead();
        var firstReadAt = notif.ReadAt;

        // Calling again should not change ReadAt
        notif.MarkRead();

        notif.ReadAt.Should().Be(firstReadAt);
    }
}
