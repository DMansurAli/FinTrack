using FinTrack.Application.Notifications.EventHandlers;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Events;
using FinTrack.Infrastructure.Persistence;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Tests.Common;
using FluentAssertions;

namespace FinTrack.Tests.Notifications;

public class TransactionNotificationHandlerTests
{
    private readonly Guid   _userId   = Guid.NewGuid();
    private readonly string _email    = "alice@example.com";
    private readonly string _firstName = "Alice";

    private TransactionNotificationHandler BuildHandler(
        out FakeNotificationRepository notifRepo,
        out FakeEmailService           emailService,
        out AppDbContext               db)
    {
        db           = TestDbContext.Create();
        notifRepo    = new FakeNotificationRepository();
        emailService = new FakeEmailService();

        // Seed a real user so the handler can look up their email
        var user = User.Create(_email, "hashed:pw", _firstName, "Smith");
        // Override the auto-generated Id so it matches _userId
        typeof(User).GetProperty("Id")!
            .SetValue(user, _userId);
        db.Users.Add(user);
        db.SaveChanges();

        var userRepo = new UserRepository(db);

        return new TransactionNotificationHandler(notifRepo, userRepo, emailService);
    }

    private static TransactionCreatedEvent MakeEvent(Guid userId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), userId,
            TransactionType.Deposit, 500m, 1500m);

    [Fact]
    public async Task Handle_CreatesNotification_WithCorrectTitle()
    {
        var handler = BuildHandler(out var notifRepo, out _, out _);
        var evt     = MakeEvent(_userId);

        await handler.Handle(evt, CancellationToken.None);

        var notifications = await notifRepo.GetByUserIdAsync(_userId);
        notifications.Should().ContainSingle();
        notifications[0].Title.Should().Be("Deposit received");
    }

    [Fact]
    public async Task Handle_CreatesNotification_BodyContainsAmountAndBalance()
    {
        var handler = BuildHandler(out var notifRepo, out _, out _);
        var evt     = MakeEvent(_userId);

        await handler.Handle(evt, CancellationToken.None);

        var notifications = await notifRepo.GetByUserIdAsync(_userId);
        notifications[0].Body.Should().Contain("500.00");
        notifications[0].Body.Should().Contain("1,500.00");
    }

    [Fact]
    public async Task Handle_SendsEmail_ToCorrectAddress()
    {
        var handler = BuildHandler(out _, out var emailService, out _);
        var evt     = MakeEvent(_userId);

        await handler.Handle(evt, CancellationToken.None);

        emailService.SentEmails.Should().ContainSingle();
        emailService.SentEmails[0].To.Should().Be(_email);
        emailService.SentEmails[0].Subject.Should().Be("Deposit received");
    }

    [Fact]
    public async Task Handle_NewNotification_IsUnread()
    {
        var handler = BuildHandler(out var notifRepo, out _, out _);
        var evt     = MakeEvent(_userId);

        await handler.Handle(evt, CancellationToken.None);

        var notifications = await notifRepo.GetByUserIdAsync(_userId);
        notifications[0].IsRead.Should().BeFalse();
        notifications[0].ReadAt.Should().BeNull();
    }
}
