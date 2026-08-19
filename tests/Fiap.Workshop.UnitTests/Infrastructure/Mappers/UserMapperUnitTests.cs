using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class UserMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "User >> Should map to model >> When mapping from domain")]
    public void User_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var user = new User(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            UserRole.Admin,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = user.MapToModel();

        // Assert
        Assert.Equal(user.Id, model.Id);
        Assert.Equal(user.Email, model.Email);
        Assert.Equal(user.Name, model.Name);
        Assert.Equal(user.Password, model.Password);
        Assert.Equal(user.Role, model.Role);
        Assert.Equal(user.CreatedAt, model.CreatedAt);
        Assert.Equal(user.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "UserModel >> Should map to domain >> When mapping from model")]
    public void UserModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new UserModel(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            UserRole.Admin,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var user = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, user.Id);
        Assert.Equal(model.Email, user.Email);
        Assert.Equal(model.Name, user.Name);
        Assert.Equal(model.Password, user.Password);
        Assert.Equal(model.Role, user.Role);
        Assert.Equal(model.CreatedAt, user.CreatedAt);
        Assert.Equal(model.UpdatedAt, user.UpdatedAt);
    }
}
