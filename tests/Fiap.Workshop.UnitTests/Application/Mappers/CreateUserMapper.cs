using AutoFixture;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;
using Fiap.Workshop.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.Mappers;

public class CreateUserMapper
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "MapToDomain >> Should Success >> When map to domain")]
    public void MapToDomain_ShouldSuccess_WhenMapToDomain()
    {
        // Arrange
        var input = _fixture.Create<CreateUserInput>();
        var passwordHash = _fixture.Create<string>();

        // Act
        var result = input.MapToDomain(passwordHash);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(input.Name);
        result.Password.Should().Be(passwordHash);
        result.Email.Should().Be(input.Email);
        result.Role.Should().Be(input.Role);
    }

    [Fact(DisplayName = "MapToDto >> Should Success >> When map to dto")]
    public void MapToDto_ShouldSuccess_WhenMapToDto()
    {
        // Arrange
        var user = _fixture.Create<User>();

        // Act
        var result = user.MapToDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
        result.Email.Should().Be(user.Email);
        result.Role.Should().Be(user.Role.ToString());
    }
}
