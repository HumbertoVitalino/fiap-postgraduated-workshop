using System.Security.Claims;
using Fiap.Workshop.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

    public string Email =>
        Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Role =>
        Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;
}
