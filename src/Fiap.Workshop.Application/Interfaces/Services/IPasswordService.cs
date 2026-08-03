namespace Fiap.Workshop.Application.Interfaces.Services;

public interface IPasswordService
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string hash);
}
