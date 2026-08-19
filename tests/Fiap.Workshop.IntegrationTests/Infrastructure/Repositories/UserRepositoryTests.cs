using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class UserRepositoryTests(DatabaseFixture fixture)
{
    private static User CreateUser(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        TestData.Email(),
        "Integration User",
        TestData.ShortString(32),
        UserRole.Admin,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task WithScopeAsync(Func<IUserRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IUserRepository>());
    }

    private async Task SeedAsync(User user) => await WithScopeAsync(async repo =>
    {
        await repo.AddAsync(user, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "UserRepository >> Should persist and retrieve >> When adding a new user")]
    public async Task UserRepository_ShouldPersistAndRetrieve_WhenAddingNewUser()
    {
        // Arrange
        var user = CreateUser();

        // Act
        await WithScopeAsync(async repo =>
        {
            await repo.AddAsync(user, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        User? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(user.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
        Assert.Equal(user.Email, found.Email);
        Assert.Equal(user.Name, found.Name);
        Assert.Equal(user.Password, found.Password);
        Assert.Equal(user.Role, found.Role);
    }

    [Fact(DisplayName = "UserRepository >> Should return null >> When user does not exist")]
    public async Task UserRepository_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        User? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "UserRepository >> Should find by email >> When user exists")]
    public async Task UserRepository_ShouldFindByEmail_WhenUserExists()
    {
        // Arrange
        var user = CreateUser();
        await SeedAsync(user);

        // Act
        User? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByEmailAsync(user.Email, CancellationToken.None));

        // Assert
        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact(DisplayName = "UserRepository >> Should confirm existence >> When email is already used")]
    public async Task UserRepository_ShouldConfirmExistence_WhenEmailIsAlreadyUsed()
    {
        // Arrange
        var user = CreateUser();
        await SeedAsync(user);

        // Act
        var exists = false;
        await WithScopeAsync(async repo => exists = await repo.ExistsWithEmailAsync(user.Email, CancellationToken.None));

        // Assert
        Assert.True(exists);
    }

    [Fact(DisplayName = "UserRepository >> Should persist changes >> When updating an existing user")]
    public async Task UserRepository_ShouldPersistChanges_WhenUpdatingExistingUser()
    {
        // Arrange
        var user = CreateUser();
        await SeedAsync(user);

        var updated = new User(user.Id, user.Email, "Updated Name", user.Password, UserRole.Admin, user.CreatedAt, DateTime.UtcNow);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        User? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(user.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal("Updated Name", found!.Name);
        Assert.Equal(UserRole.Admin, found.Role);
    }

    [Fact(DisplayName = "UserRepository >> Should remove entity >> When removing an existing user")]
    public async Task UserRepository_ShouldRemoveEntity_WhenRemovingExistingUser()
    {
        // Arrange
        var user = CreateUser();
        await SeedAsync(user);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Remove(user);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        User? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(user.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
