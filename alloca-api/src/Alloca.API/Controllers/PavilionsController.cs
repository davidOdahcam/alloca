using Alloca.Application.DTOs.Availability;
using Alloca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pavilions")]
public class PavilionsController(IPavilionService pavilions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await pavilions.ListAsync(ct));

    [HttpGet("{pavilionId:guid}/floors")]
    public async Task<IActionResult> Floors(Guid pavilionId, CancellationToken ct)
        => Ok(await pavilions.ListFloorsAsync(pavilionId, ct));

    [HttpPost("{pavilionId:guid}/floors/{floorId:guid}/availability")]
    public async Task<IActionResult> Availability(
        Guid pavilionId, Guid floorId, [FromBody] CheckAvailabilityRequest request, CancellationToken ct)
        => Ok(await pavilions.CheckAvailabilityAsync(pavilionId, floorId, request, ct));
}
