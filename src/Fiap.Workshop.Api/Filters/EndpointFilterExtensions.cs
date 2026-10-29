namespace Fiap.Workshop.Api.Filters;

public static class EndpointFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull
        => builder.AddEndpointFilter<ValidationFilter<TRequest>>();

    public static RouteHandlerBuilder WithApiKey(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<ApiKeyFilter>();
}
