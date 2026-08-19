using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class TrackServiceOrdersUseCaseUnitTests : LoggerTestBase<TrackServiceOrdersUseCase>
{
    private const string Document = "11144477735";
    private const string LicensePlate = "ABC1234";

    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly TrackServiceOrdersUseCase _useCase;

    public TrackServiceOrdersUseCaseUnitTests()
    {
        _useCase = new(
            _vehicleRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private TrackServiceOrdersInput CreateInput() => new(
        _fixture.Create<Guid>(),
        Document,
        LicensePlate
    );

    private static Vehicle CreateVehicle(Guid customerId) => new(
        Guid.NewGuid(),
        customerId,
        LicensePlate,
        "Ford",
        "Ka",
        2020,
        2021,
        "Black",
        DateTime.Now,
        DateTime.Now
    );

    private static Customer CreateCustomer(Guid id, string document) => new(
        id,
        "Jane Doe",
        document,
        "jane@example.com",
        "11999999999",
        DateTime.Now,
        DateTime.Now
    );

    private static ServiceOrder CreateServiceOrder(Guid vehicleId, Guid customerId) => new(
        Guid.NewGuid(),
        customerId,
        vehicleId,
        Guid.NewGuid(),
        "Engine noise",
        12000,
        DateTime.Now,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Return Orders >> When plate and document match")]
    public async Task Handle_ShouldReturnOrders_WhenPlateAndDocumentMatch()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), Document);
        var vehicle = CreateVehicle(customer.Id);
        var serviceOrder = CreateServiceOrder(vehicle.Id, customer.Id);
        var input = CreateInput();

        _vehicleRepositoryMock.Setup(x => x.GetByLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(vehicle.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _serviceOrderRepositoryMock.Setup(x => x.GetAllByVehicleIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([serviceOrder]);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Subject;
        response.Should().ContainSingle().Which.Id.Should().Be(serviceOrder.Id);
    }

    [Fact(DisplayName = "Handle >> Should Return Empty >> When vehicle does not exist")]
    public async Task Handle_ShouldReturnEmpty_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = CreateInput();

        _vehicleRepositoryMock.Setup(x => x.GetByLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();

        _customerRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _serviceOrderRepositoryMock.Verify(x => x.GetAllByVehicleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Return Empty >> When vehicle's customer no longer exists")]
    public async Task Handle_ShouldReturnEmpty_WhenVehicleCustomerNoLongerExists()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>());
        var input = CreateInput();

        _vehicleRepositoryMock.Setup(x => x.GetByLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(vehicle.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();

        _serviceOrderRepositoryMock.Verify(x => x.GetAllByVehicleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Return Empty >> When document does not match the vehicle's owner")]
    public async Task Handle_ShouldReturnEmpty_WhenDocumentDoesNotMatchVehicleOwner()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "22233344456");
        var vehicle = CreateVehicle(customer.Id);
        var input = CreateInput();

        _vehicleRepositoryMock.Setup(x => x.GetByLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(vehicle.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();

        _serviceOrderRepositoryMock.Verify(x => x.GetAllByVehicleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Return Empty List >> When vehicle has no service orders")]
    public async Task Handle_ShouldReturnEmptyList_WhenVehicleHasNoServiceOrders()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), Document);
        var vehicle = CreateVehicle(customer.Id);
        var input = CreateInput();

        _vehicleRepositoryMock.Setup(x => x.GetByLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(vehicle.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _serviceOrderRepositoryMock.Setup(x => x.GetAllByVehicleIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();
    }
}
