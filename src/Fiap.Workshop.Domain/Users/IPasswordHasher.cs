namespace Fiap.Workshop.Domain.Users;

public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string hash);
}
