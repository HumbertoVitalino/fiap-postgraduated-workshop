namespace Fiap.Workshop.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
