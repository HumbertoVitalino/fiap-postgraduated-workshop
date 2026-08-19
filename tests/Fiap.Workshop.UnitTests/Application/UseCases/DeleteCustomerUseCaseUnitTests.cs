using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeleteCustomerUseCaseUnitTests : LoggerTestBase<DeleteCustomerUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeleteCustomerUseCase _useCase;

    public DeleteCustomerUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private DeleteCustomerInput CreateInput(Guid customerId) => new(
        _fixture.Create<Guid>(),
        customerId
    );

    private static Customer CreateCustomer(Guid id) => new(
        id,
        "Jane Doe",
        "11144477735",
        "jane@example.com",
        "11999999999",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When customer exists and has no vehicles")]
    public async Task Handle_ShouldSuccess_WhenCustomerExistsAndHasNoVehicles()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithCustomerIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        _customerRepositoryMock.Verify(x => x.Remove(customer), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Succeed idempotently >> When customer does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find customer by [{input.CustomerId}].",
            Times.Once()
        );
        _vehicleRepositoryMock.Verify(x => x.ExistsWithCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _customerRepositoryMock.Verify(x => x.Remove(It.IsAny<Customer>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer has vehicles associated")]
    public async Task Handle_ShouldFail_WhenCustomerHasVehiclesAssociated()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithCustomerIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Customer has vehicles associated and cannot be deleted.");
        result.Result.Should().BeNull();

        _customerRepositoryMock.Verify(x => x.Remove(It.IsAny<Customer>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error deleting customer")]
    public async Task Handle_ShouldFail_WhenErrorDeletingCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithCustomerIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error deleting customer with id {input.CustomerId}.");
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error deleting customer with id {input.CustomerId}.",
            Times.Once()
        );
    }
}
