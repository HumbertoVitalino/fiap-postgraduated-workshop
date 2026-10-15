using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Fiap.Workshop.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class RefreshTokenService(IConfiguration configuration) : IRefreshTokenService
{
    private const int TokenSizeInBytes = 64;

    public GeneratedRefreshToken Generate()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

        var expiration = int.TryParse(configuration["Jwt:RefreshTokenExpirationInDays"], out var days)
            ? days
            : 7;

        return new(token, Hash(token), DateTime.Now.AddDays(expiration));
    }

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
