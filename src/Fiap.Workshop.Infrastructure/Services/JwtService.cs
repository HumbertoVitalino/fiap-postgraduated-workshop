using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fiap.Workshop.Infrastructure.Services;

internal sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
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
