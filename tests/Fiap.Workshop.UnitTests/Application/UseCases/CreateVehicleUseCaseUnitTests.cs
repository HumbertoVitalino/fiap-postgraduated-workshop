using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateVehicleUseCaseUnitTests : LoggerTestBase<CreateVehicleUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly CreateVehicleUseCase _useCase;

    public CreateVehicleUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private CreateVehicleInput CreateInput(Guid customerId) => new(
        _fixture.Create<Guid>(),
        customerId,
        "ABC1234",
        "Ford",
        "Ka",
        2020,
        2021,
        "Black"
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

    [Fact(DisplayName = "Handle >> Should Success >> When customer exists and plate is not in use")]
    public async Task Handle_ShouldSuccess_WhenCustomerExistsAndPlateIsNotInUse()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();

        _vehicleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

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
        _vehicleRepositoryMock.Verify(x => x.ExistsWithLicensePlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When license plate already exists")]
    public async Task Handle_ShouldFail_WhenLicensePlateAlreadyExists()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Vehicle with license plate {input.LicensePlate} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Vehicle with license plate {input.LicensePlate} already exists.",
            Times.Once()
        );
        _vehicleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving vehicle")]
    public async Task Handle_ShouldFail_WhenErrorSavingVehicle()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>());
        var input = CreateInput(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock.Setup(x => x.ExistsWithLicensePlateAsync(input.LicensePlate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error saving vehicle with license plate {input.LicensePlate}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error saving vehicle with license plate {input.LicensePlate}.",
            Times.Once()
        );
    }
}
