namespace Fiap.Workshop.Api.Requests.Customers;

public sealed record UpdateCustomerRequest(
    Guid CorrelationId,
    string Name,
    string Email,
    string Phone
);
