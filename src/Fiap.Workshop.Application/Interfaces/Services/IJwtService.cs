using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
