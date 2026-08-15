using Asp.Versioning;
using Fiap.Workshop.Api.Endpoints.Auth;
using Fiap.Workshop.Api.Endpoints.Customers;
using Fiap.Workshop.Api.Endpoints.Services;
using Fiap.Workshop.Api.Endpoints.Users;
using Fiap.Workshop.Api.Endpoints.Vehicles;

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
        app.MapCustomersEndpoints(apiVersion);
        app.MapVehiclesEndpoints(apiVersion);
        app.MapServicesEndpoints(apiVersion);

        return app;
    }
}
