using Fiap.Workshop.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(configuration["Email:FromName"], configuration["Email:FromAddress"]!));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart(TextFormat.Plain) { Text = message.Body };

        var port = int.TryParse(configuration["Email:Port"], out var configuredPort)
            ? configuredPort
            : 587;

        var secureSocketOptions = Enum.TryParse<SecureSocketOptions>(configuration["Email:SecureSocketOptions"], out var configuredOptions)
            ? configuredOptions
            : SecureSocketOptions.Auto;

        var timeoutInSeconds = int.TryParse(configuration["Email:TimeoutInSeconds"], out var configuredTimeout)
            ? configuredTimeout
            : 10;

        using var client = new SmtpClient
        {
            Timeout = (int)TimeSpan.FromSeconds(timeoutInSeconds).TotalMilliseconds
        };

        await client.ConnectAsync(configuration["Email:Host"]!, port, secureSocketOptions, cancellationToken);

        var username = configuration["Email:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, configuration["Email:Password"] ?? string.Empty, cancellationToken);

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
