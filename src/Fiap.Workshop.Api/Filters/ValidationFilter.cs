using Fiap.Workshop.Application.Commons;
using FluentValidation;

namespace Fiap.Workshop.Api.Filters;

public sealed class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : notnull
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
            return await next(context);

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            Output output = new();
            output.AddErrorMessages(validationResult.Errors.Select(error => error.ErrorMessage));

            return Results.BadRequest(output);
        }

        return await next(context);
    }
}
