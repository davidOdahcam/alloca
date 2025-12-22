using Alloca.Application.Features.Availability;
using Alloca.Application.Features.Pavilions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pavilions")]
public class PavilionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await sender.Send(new ListPavilionsQuery(), ct));

    [HttpGet("{pavilionId:guid}/floors")]
    public async Task<IActionResult> Floors(Guid pavilionId, CancellationToken ct)
        => Ok(await sender.Send(new ListFloorsQuery(pavilionId), ct));

    [HttpPost("{pavilionId:guid}/floors/{floorId:guid}/availability")]
    public async Task<IActionResult> Availability(
        Guid pavilionId, Guid floorId, [FromBody] AvailabilityRequest body, CancellationToken ct)
        => Ok(await sender.Send(new CheckAvailabilityQuery(pavilionId, floorId, body.StartUtc, body.EndUtc), ct));

    public record AvailabilityRequest(DateTime StartUtc, DateTime EndUtc);
}
