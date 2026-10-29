using System.Security.Cryptography;
using System.Text;

namespace Fiap.Workshop.Api.Filters;

public sealed class ApiKeyFilter(IConfiguration configuration, ILogger<ApiKeyFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ApiKeyFilter> _logger = logger;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var expectedApiKey = _configuration["Webhooks:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            _logger.LogError("Webhook API key is not configured; rejecting request to {Path}.", context.HttpContext.Request.Path);
            return Results.Unauthorized();
        }

        var providedApiKey = context.HttpContext.Request.Headers[HeaderName].ToString();
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedApiKey),
            Encoding.UTF8.GetBytes(expectedApiKey)
        );

        if (!isValid)
        {
            _logger.LogWarning("Invalid or missing {HeaderName} header on request to {Path}.", HeaderName, context.HttpContext.Request.Path);
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
