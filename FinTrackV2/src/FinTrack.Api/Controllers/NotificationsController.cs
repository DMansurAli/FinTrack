using System.Security.Claims;
using FinTrack.Application.Notifications.Commands.MarkNotificationRead;
using FinTrack.Application.Notifications.Queries.GetNotifications;
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
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    private Guid UserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Get all notifications for the current user, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetNotificationsQuery(UserId), ct);
        return Ok(result.Value);
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new MarkNotificationReadCommand(id, UserId), ct);

        return result.IsFailure
            ? NotFound(new { result.Error.Code, result.Error.Message })
            : NoContent();
    }
}
