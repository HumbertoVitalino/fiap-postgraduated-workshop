namespace Fiap.Workshop.Api.Requests.Customers;

public sealed record CreateCustomerRequest(
    Guid CorrelationId,
    string Name,
    string Document,
    string Email,
    string Phone
);