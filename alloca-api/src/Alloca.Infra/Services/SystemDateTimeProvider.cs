using Alloca.Application.Common.Interfaces;

namespace Alloca.Infra.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
