using Alloca.Application.DTOs.Reservations;
using Alloca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public class ReservationsController(IReservationAppService reservations) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await reservations.ListMineAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var resp = await reservations.CreateAsync(request, ct);
        return Created($"/api/reservations/{resp.Id}", resp);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await reservations.CancelAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/checkin")]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInRequest request, CancellationToken ct)
    {
        await reservations.CheckInAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> QrCode(Guid id, CancellationToken ct)
    {
        var (png, _) = await reservations.GetQrCodeAsync(id, ct);
        return File(png, "image/png");
    }
}
