using System.Collections.Concurrent;
using Fiap.Workshop.Application.Interfaces.Services;

namespace Fiap.Workshop.IntegrationTests.Fixtures;

public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
