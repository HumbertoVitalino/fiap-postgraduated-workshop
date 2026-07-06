using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.UnitTests.Domain.Users;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string rawPassword) => $"hashed:{rawPassword}";

    public bool Verify(string rawPassword, string hash) => hash == Hash(rawPassword);
}
