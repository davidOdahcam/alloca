using Alloca.Application.DTOs.Auth;

namespace Alloca.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
