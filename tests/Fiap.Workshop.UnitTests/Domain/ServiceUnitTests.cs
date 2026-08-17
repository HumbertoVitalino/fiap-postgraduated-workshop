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
}
