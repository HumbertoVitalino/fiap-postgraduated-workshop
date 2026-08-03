using Fiap.Workshop.Application.Interfaces.Services;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string rawPassword) =>
        BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: WorkFactor);

    public bool Verify(string rawPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(rawPassword, hash);
}
