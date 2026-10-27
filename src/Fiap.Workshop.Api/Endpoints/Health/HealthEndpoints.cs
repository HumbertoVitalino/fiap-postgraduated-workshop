using Fiap.Workshop.Infrastructure.IoC;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Fiap.Workshop.Api.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        })
        .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(DependencyInjection.ReadinessTag)
        })
        .AllowAnonymous();

        return app;
    }
}
