using System.Security.Claims;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Alloca.IoC.Auth;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? accessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var role = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, out var r) ? r : null;
        }
    }

    public bool IsAuthenticated => accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
