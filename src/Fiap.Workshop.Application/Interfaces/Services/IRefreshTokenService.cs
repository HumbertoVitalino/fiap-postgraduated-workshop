namespace Fiap.Workshop.Application.Interfaces.Services;

public interface IRefreshTokenService
{
    GeneratedRefreshToken Generate();
    string Hash(string token);
}
