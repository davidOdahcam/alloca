using Alloca.Application.DTOs.Users;

namespace Alloca.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserListItem>> ListAsync(string? search, string? role, bool? isActive, CancellationToken ct = default);
    Task<UserDetail> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetail> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task ActivateAsync(Guid id, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken ct = default);
}
