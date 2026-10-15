using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class RefreshTokenMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "RefreshToken >> Should map to model >> When mapping from domain")]
    public void RefreshToken_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            DateTime.Now.AddDays(7),
            DateTime.Now,
            DateTime.Now,
            DateTime.Now,
            Guid.NewGuid()
        );

        // Act
        var model = refreshToken.MapToModel();

        // Assert
        Assert.Equal(refreshToken.Id, model.Id);
        Assert.Equal(refreshToken.UserId, model.UserId);
        Assert.Equal(refreshToken.TokenHash, model.TokenHash);
        Assert.Equal(refreshToken.ExpiresAt, model.ExpiresAt);
        Assert.Equal(refreshToken.RevokedAt, model.RevokedAt);
        Assert.Equal(refreshToken.ReplacedByTokenId, model.ReplacedByTokenId);
        Assert.Equal(refreshToken.CreatedAt, model.CreatedAt);
        Assert.Equal(refreshToken.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "RefreshTokenModel >> Should map to domain >> When mapping from model")]
    public void RefreshTokenModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new RefreshTokenModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            DateTime.Now.AddDays(7),
            DateTime.Now,
            Guid.NewGuid(),
            DateTime.Now,
            DateTime.Now
        );

        // Act
        var refreshToken = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, refreshToken.Id);
        Assert.Equal(model.UserId, refreshToken.UserId);
        Assert.Equal(model.TokenHash, refreshToken.TokenHash);
        Assert.Equal(model.ExpiresAt, refreshToken.ExpiresAt);
        Assert.Equal(model.RevokedAt, refreshToken.RevokedAt);
        Assert.Equal(model.ReplacedByTokenId, refreshToken.ReplacedByTokenId);
        Assert.Equal(model.CreatedAt, refreshToken.CreatedAt);
        Assert.Equal(model.UpdatedAt, refreshToken.UpdatedAt);
    }
}
