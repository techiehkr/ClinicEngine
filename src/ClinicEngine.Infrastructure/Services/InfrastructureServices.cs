using ClinicEngine.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClinicEngine.Infrastructure.Services;


public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Name"].FirstOrDefault()
        ?? "system";
}


public sealed class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
