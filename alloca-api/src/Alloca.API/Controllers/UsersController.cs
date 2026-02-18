using Alloca.Application.DTOs.Users;
using Alloca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItem>>> List(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        CancellationToken ct)
        => Ok(await userService.ListAsync(search, role, isActive, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetail>> Get(Guid id, CancellationToken ct)
        => Ok(await userService.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<UserDetail>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var resp = await userService.CreateAsync(request, ct);
        return Created($"/api/users/{resp.Id}", resp);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDetail>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await userService.UpdateAsync(id, request, ct));

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await userService.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await userService.ActivateAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetUserPasswordRequest request, CancellationToken ct)
    {
        await userService.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }
}
