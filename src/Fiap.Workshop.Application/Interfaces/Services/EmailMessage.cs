namespace Fiap.Workshop.Application.Interfaces.Services;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body
);
