namespace Fiap.Workshop.Application.DTOs.Customer;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Phone
);