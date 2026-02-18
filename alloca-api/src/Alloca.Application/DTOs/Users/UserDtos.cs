namespace Alloca.Application.DTOs.Users;

public record UserListItem(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public record UserDetail(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    string Role);

public record UpdateUserRequest(
    string FullName,
    string Role);

public record ResetUserPasswordRequest(string NewPassword);
