using Alloca.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd, ct));

    [HttpPost("register")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterCommand cmd, CancellationToken ct)
    {
        var id = await sender.Send(cmd, ct);
        return Created($"/api/users/{id}", new { id });
    }
}
