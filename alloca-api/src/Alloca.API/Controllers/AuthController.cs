using Alloca.Application.DTOs.Auth;
using Alloca.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Alloca.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthAppService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await authService.LoginAsync(request, ct));

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var resp = await authService.RegisterAsync(request, ct);
        return Created($"/api/users/{resp.Id}", resp);
    }
}
