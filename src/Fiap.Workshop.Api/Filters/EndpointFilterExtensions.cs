namespace Fiap.Workshop.Api.Filters;

public static class EndpointFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : notnull
        => builder.AddEndpointFilter<ValidationFilter<TRequest>>();
}
