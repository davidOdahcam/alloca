using Alloca.Application.Features.Blocks;
using Alloca.Application.Features.Manager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize(Roles = "PavilionManager,Admin")]
[Route("api/manager")]
public class ManagerController(ISender sender) : ControllerBase
{
    [HttpGet("reservations/pending")]
    public async Task<IActionResult> Pending([FromQuery] Guid? pavilionId, CancellationToken ct)
        => Ok(await sender.Send(new ListPendingReservationsQuery(pavilionId), ct));

    [HttpPost("reservations/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await sender.Send(new ApproveReservationCommand(id), ct);
        return NoContent();
    }

    public record ReasonBody(string Reason);

    [HttpPost("reservations/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReasonBody body, CancellationToken ct)
    {
        await sender.Send(new RejectReservationCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("reservations/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] ReasonBody body, CancellationToken ct)
    {
        await sender.Send(new RevokeReservationCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlock([FromBody] CreateBlockCommand cmd, CancellationToken ct)
    {
        var id = await sender.Send(cmd, ct);
        return Created($"/api/manager/blocks/{id}", new { id });
    }
}
