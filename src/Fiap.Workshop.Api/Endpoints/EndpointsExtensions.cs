using Asp.Versioning;
using Asp.Versioning.Builder;
using Fiap.Workshop.Api.Endpoints.Auth;
using Fiap.Workshop.Api.Endpoints.Users;

namespace Fiap.Workshop.Api.Endpoints;

public static class EndpointsExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        app.MapUsers(versionSet);
        app.MapAuth(versionSet);

        return app;
    }
}
