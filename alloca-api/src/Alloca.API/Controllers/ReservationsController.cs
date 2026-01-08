using Alloca.Application.Common.Interfaces;
using Alloca.Application.Features.Reservations;
using Alloca.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public class ReservationsController(ISender sender) : ControllerBase
{
    public record CreateReservationRequest(ResourceType ResourceType, Guid ResourceId, DateTime StartUtc, DateTime EndUtc, string? Notes);
    public record CheckInRequest(string ScannedExternalId);

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await sender.Send(new ListMyReservationsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest body, CancellationToken ct)
    {
        var id = await sender.Send(new CreateReservationCommand(
            body.ResourceType, body.ResourceId, body.StartUtc, body.EndUtc, body.Notes), ct);
        return Created($"/api/reservations/{id}", new { id });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelReservationCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/checkin")]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInRequest body, CancellationToken ct)
    {
        await sender.Send(new CheckInReservationCommand(id, body.ScannedExternalId), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> QrCode(Guid id, [FromServices] IQrCodeService qr, [FromServices] IAppDbContext db, [FromServices] ICurrentUserService current, CancellationToken ct)
    {
        // Returns a QR encoding the resource external ID (this is the QR fixed at the room/desk;
        // exposing it here just for development convenience).
        var reservation = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.Reservations, r => r.Id == id, ct);
        if (reservation is null) return NotFound();
        if (current.UserId != reservation.UserId) return Forbid();
        string payload;
        if (reservation.ResourceType == ResourceType.Room)
        {
            var room = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Rooms, r => r.Id == reservation.RoomId, ct);
            payload = room.ExternalId;
        }
        else
        {
            var desk = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Desks, d => d.Id == reservation.DeskId, ct);
            payload = desk.ExternalId;
        }
        var png = qr.GeneratePng(payload);
        return File(png, "image/png");
    }
}
