using Asp.Versioning;
using Fiap.Workshop.Api.Endpoints.Auth;
using Fiap.Workshop.Api.Endpoints.Users;

namespace Fiap.Workshop.Api.Endpoints;

public static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapMinimalApisV1(this IEndpointRouteBuilder app)
    {
        var apiVersion = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        app.MapUsersEndpoints(apiVersion);
        app.MapAuthEndpoints(apiVersion);

        return app;
    }
}
