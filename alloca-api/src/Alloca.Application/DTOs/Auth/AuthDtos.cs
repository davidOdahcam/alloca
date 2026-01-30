namespace Alloca.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    string Email,
    string Role);

public record RegisterRequest(string Email, string FullName, string Password);

public record RegisterResponse(Guid Id);
