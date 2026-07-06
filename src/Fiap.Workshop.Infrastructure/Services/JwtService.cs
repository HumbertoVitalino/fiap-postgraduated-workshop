using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fiap.Workshop.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(string userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var expiration = int.TryParse(configuration["Jwt:ExpirationInMinutes"], out var minutes)
            ? minutes
            : 60;

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiration),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
