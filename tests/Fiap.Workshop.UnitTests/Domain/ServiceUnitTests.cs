using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceUnitTests
{
    private readonly Fixture _fixture = new();

    private Service CreateService(decimal basePrice, short estimatedDuration) => new(
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        basePrice,
        estimatedDuration,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Service >> Should be created >> When all required properties are provided")]
    public void Service_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var basePrice = _fixture.Create<decimal>();
        var estimatedDuration = _fixture.Create<short>();
        var isActive = _fixture.Create<bool>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var service = new Service(
            id,
            code,
            name,
            description,
            basePrice,
            estimatedDuration,
            isActive,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, service.Id);
        Assert.Equal(code, service.Code);
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(basePrice, service.BasePrice);
        Assert.Equal(estimatedDuration, service.EstimatedDuration);
        Assert.Equal(isActive, service.IsActive);
        Assert.Equal(createdAt, service.CreatedAt);
        Assert.Equal(updatedAt, service.UpdatedAt);
        Assert.Equal(0, service.ExecutionCount);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When base price is negative")]
    public void Service_ShouldThrowDomainException_WhenBasePriceIsNegative()
    {
        // Act
        var act = () => CreateService(-0.01m, 60);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidBasePrice, exception.Message);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When estimated duration is negative")]
    public void Service_ShouldThrowDomainException_WhenEstimatedDurationIsNegative()
    {
        // Act
        var act = () => CreateService(150.00m, -1);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidEstimatedDuration, exception.Message);
    }

    [Fact(DisplayName = "Service >> Should set estimated duration to actual duration >> When it is the first recorded execution")]
    public void Service_ShouldSetEstimatedDurationToActualDuration_WhenItIsTheFirstRecordedExecution()
    {
        // Arrange
        var service = CreateService(150.00m, 60);
        var before = DateTime.Now;

        // Act
        service.RecordExecution(90);

        // Assert
        Assert.Equal((short)90, service.EstimatedDuration);
        Assert.Equal(1, service.ExecutionCount);
        Assert.InRange(service.UpdatedAt, before, DateTime.Now);
    }

    [Fact(DisplayName = "Service >> Should update the incremental average >> When there are previous recorded executions")]
    public void Service_ShouldUpdateTheIncrementalAverage_WhenThereArePreviousRecordedExecutions()
    {
        // Arrange
        var service = CreateService(150.00m, 60);
        service.RecordExecution(90);

        // Act
        service.RecordExecution(100);

        // Assert
        Assert.Equal((short)95, service.EstimatedDuration);
        Assert.Equal(2, service.ExecutionCount);
    }

    [Fact(DisplayName = "Service >> Should truncate the average toward the previous estimate >> When the difference does not divide evenly")]
    public void Service_ShouldTruncateTheAverageTowardThePreviousEstimate_WhenTheDifferenceDoesNotDivideEvenly()
    {
        // Arrange — after two executions with no drift, EstimatedDuration=60 and ExecutionCount=2. A one-minute
        // difference (61-60=1) divided by (ExecutionCount+1)=3 truncates to zero under integer arithmetic, so
        // the estimate does not move even though ExecutionCount still advances.
        var service = CreateService(150.00m, 60);
        service.RecordExecution(60);
        service.RecordExecution(60);

        // Act
        service.RecordExecution(61);

        // Assert
        Assert.Equal((short)60, service.EstimatedDuration);
        Assert.Equal(3, service.ExecutionCount);
    }

    [Fact(DisplayName = "Service >> Should update profile >> When estimated duration does not change")]
    public void Service_ShouldUpdateProfile_WhenEstimatedDurationDoesNotChange()
    {
        // Arrange
        var service = CreateService(150.00m, 60);
        service.RecordExecution(90);
        var originalCode = service.Code;
        var currentEstimatedDuration = service.EstimatedDuration;

        // Act
        service.UpdateProfile("New Name", "New Description", 200.00m, currentEstimatedDuration, false);

        // Assert
        Assert.Equal("New Name", service.Name);
        Assert.Equal("New Description", service.Description);
        Assert.Equal(200.00m, service.BasePrice);
        Assert.Equal(currentEstimatedDuration, service.EstimatedDuration);
        Assert.False(service.IsActive);
        Assert.Equal(originalCode, service.Code);
        Assert.Equal(1, service.ExecutionCount);
    }

    [Fact(DisplayName = "Service >> Should reset execution count >> When estimated duration changes")]
    public void Service_ShouldResetExecutionCount_WhenEstimatedDurationChanges()
    {
        // Arrange
        var service = CreateService(150.00m, 60);
        service.RecordExecution(90);
        service.RecordExecution(100);

        // Act
        service.UpdateProfile("New Name", "New Description", 200.00m, 45, true);

        // Assert
        Assert.Equal((short)45, service.EstimatedDuration);
        Assert.Equal(0, service.ExecutionCount);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When UpdateProfile sets a negative base price")]
    public void Service_ShouldThrowDomainException_WhenUpdateProfileSetsNegativeBasePrice()
    {
        // Arrange
        var service = CreateService(150.00m, 60);

        // Act
        var act = () => service.UpdateProfile("New Name", "New Description", -0.01m, 60, true);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidBasePrice, exception.Message);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When UpdateProfile sets a negative estimated duration")]
    public void Service_ShouldThrowDomainException_WhenUpdateProfileSetsNegativeEstimatedDuration()
    {
        // Arrange
        var service = CreateService(150.00m, 60);

        // Act
        var act = () => service.UpdateProfile("New Name", "New Description", 150.00m, -1, true);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidEstimatedDuration, exception.Message);
    }
}
