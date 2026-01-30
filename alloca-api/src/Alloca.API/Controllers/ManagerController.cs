using Alloca.Application.DTOs.Blocks;
using Alloca.Application.DTOs.Manager;
using Alloca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize(Roles = "PavilionManager,Admin")]
[Route("api/manager")]
public class ManagerController(IManagerService managerService, IBlockService blockService) : ControllerBase
{
    [HttpGet("reservations/pending")]
    public async Task<IActionResult> Pending([FromQuery] Guid? pavilionId, CancellationToken ct)
        => Ok(await managerService.ListPendingAsync(pavilionId, ct));

    [HttpPost("reservations/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await managerService.ApproveAsync(id, ct);
        return NoContent();
    }

    [HttpPost("reservations/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReasonRequest request, CancellationToken ct)
    {
        await managerService.RejectAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("reservations/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] ReasonRequest request, CancellationToken ct)
    {
        await managerService.RevokeAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlock([FromBody] CreateBlockRequest request, CancellationToken ct)
    {
        var resp = await blockService.CreateAsync(request, ct);
        return Created($"/api/manager/blocks/{resp.Id}", resp);
    }
}
