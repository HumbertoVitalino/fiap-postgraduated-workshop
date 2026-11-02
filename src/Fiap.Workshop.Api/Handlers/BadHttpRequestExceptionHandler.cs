using Fiap.Workshop.Application.Commons;
using Microsoft.AspNetCore.Diagnostics;

namespace Fiap.Workshop.Api.Handlers;

public sealed class BadHttpRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badHttpRequestException)
            return false;

        Output output = new();
        output.AddErrorMessage("The request body is missing or is not valid JSON for this endpoint.");

        httpContext.Response.StatusCode = badHttpRequestException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(output, cancellationToken);

        return true;
    }
}
