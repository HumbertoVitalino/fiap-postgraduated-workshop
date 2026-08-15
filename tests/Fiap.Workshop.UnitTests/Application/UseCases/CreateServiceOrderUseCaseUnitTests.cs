using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateServiceOrderUseCaseUnitTests : LoggerTestBase<CreateServiceOrderUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly CreateServiceOrderUseCase _useCase;

    public CreateServiceOrderUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private CreateServiceOrderInput CreateInput(Guid customerId, Guid vehicleId) => new(
        _fixture.Create<Guid>(),
        customerId,
        vehicleId,
        _fixture.Create<Guid>(),
        "Engine noise",
        12000
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

    private static Vehicle CreateVehicle(Guid id, Guid customerId) => new(
        id,
        customerId,
        "ABC1234",
        "Ford",
        "Ka",
        2020,
        2021,
        "Black",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When customer and vehicle exist and vehicle belongs to customer")]
    public async Task Handle_ShouldSuccess_WhenCustomerAndVehicleExistAndVehicleBelongsToCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), customer.Id);
        var input = CreateInput(customer.Id, vehicle.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();

        _serviceOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), _fixture.Create<Guid>());

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find customer by [{input.CustomerId}].",
            Times.Once()
        );
        _vehicleRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id, _fixture.Create<Guid>());

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find vehicle");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find vehicle by [{input.VehicleId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When vehicle does not belong to customer")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotBelongToCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var otherCustomerId = _fixture.Create<Guid>();
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), otherCustomerId);
        var input = CreateInput(customer.Id, vehicle.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Vehicle does not belong to the specified customer");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Vehicle [{input.VehicleId}] does not belong to customer [{input.CustomerId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service order")]
    public async Task Handle_ShouldFail_WhenErrorSavingServiceOrder()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), customer.Id);
        var input = CreateInput(customer.Id, vehicle.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Error saving service order");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error saving service order for vehicle [{input.VehicleId}].",
            Times.Once()
        );
    }
}
