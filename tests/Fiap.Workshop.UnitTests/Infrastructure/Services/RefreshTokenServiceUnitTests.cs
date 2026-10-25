using Fiap.Workshop.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Services;

public class RefreshTokenServiceUnitTests
{
    private static RefreshTokenService CreateService(string? expirationInDays = "7")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenExpirationInDays"] = expirationInDays
            })
            .Build();

        return new RefreshTokenService(configuration);
    }

    [Fact(DisplayName = "RefreshTokenService >> Should generate a 64-byte Base64Url token >> When Generate is called")]
    public void RefreshTokenService_ShouldGenerateA64ByteBase64UrlToken_WhenGenerateIsCalled()
    {
        // Arrange
        var service = CreateService();

        // Act
        var generated = service.Generate();

        // Assert
        Assert.Equal(86, generated.Token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", generated.Token);
    }

    [Fact(DisplayName = "RefreshTokenService >> Should generate different tokens >> When Generate is called twice")]
    public void RefreshTokenService_ShouldGenerateDifferentTokens_WhenGenerateIsCalledTwice()
    {
        // Arrange
        var service = CreateService();

        // Act
        var first = service.Generate();
        var second = service.Generate();

        // Assert
        Assert.NotEqual(first.Token, second.Token);
        Assert.NotEqual(first.TokenHash, second.TokenHash);
    }

    [Fact(DisplayName = "RefreshTokenService >> Should return the hash of the generated token >> When Generate is called")]
    public void RefreshTokenService_ShouldReturnTheHashOfTheGeneratedToken_WhenGenerateIsCalled()
    {
        // Arrange
        var service = CreateService();

        // Act
        var generated = service.Generate();

        // Assert
        Assert.Equal(service.Hash(generated.Token), generated.TokenHash);
        Assert.NotEqual(generated.Token, generated.TokenHash);
    }

    [Fact(DisplayName = "RefreshTokenService >> Should return a deterministic 64-char hex hash >> When Hash is called")]
    public void RefreshTokenService_ShouldReturnADeterministic64CharHexHash_WhenHashIsCalled()
    {
        // Arrange
        var service = CreateService();

        // Act
        var hash = service.Hash("refresh-token");

        // Assert
        Assert.Equal(service.Hash("refresh-token"), hash);
        Assert.Matches("^[0-9A-F]{64}$", hash);
    }

    [Fact(DisplayName = "RefreshTokenService >> Should use configured expiration >> When expiration is configured")]
    public void RefreshTokenService_ShouldUseConfiguredExpiration_WhenExpirationIsConfigured()
    {
        // Arrange
        var service = CreateService("30");
        var before = DateTime.Now;

        // Act
        var generated = service.Generate();

        // Assert
        Assert.InRange(generated.ExpiresAt, before.AddDays(30), DateTime.Now.AddDays(30));
    }

    [Fact(DisplayName = "RefreshTokenService >> Should default to 7 days >> When expiration is not a valid number")]
    public void RefreshTokenService_ShouldDefaultTo7Days_WhenExpirationIsNotAValidNumber()
    {
        // Arrange
        var service = CreateService("invalid");
        var before = DateTime.Now;

        // Act
        var generated = service.Generate();

        // Assert
        Assert.InRange(generated.ExpiresAt, before.AddDays(7), DateTime.Now.AddDays(7));
    }
}
