using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class AddBudgetUseCaseUnitTests : LoggerTestBase<AddBudgetUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly AddBudgetUseCase _useCase;

    public AddBudgetUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            _serviceRepositoryMock.Object,
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateDiagnosingServiceOrder(Guid id)
    {
        var serviceOrder = new ServiceOrder(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Engine noise",
            12000,
            DateTime.Now,
            DateTime.Now,
            DateTime.Now
        );

        serviceOrder.StartDiagnosis("Worn brake pads", Guid.NewGuid());
        return serviceOrder;
    }

    private static InventoryItem CreateInventoryItem(Guid id, int quantityOnHand, decimal unitPrice, bool isActive = true) => new(
        id,
        "BP-001",
        "Brake Pad",
        "Brake pad set",
        quantityOnHand,
        0,
        5,
        unitPrice,
        UnitOfMeasure.Piece,
        isActive,
        DateTime.Now,
        DateTime.Now
    );

    private static Service CreateService(Guid id, decimal basePrice, bool isActive = true) => new(
        id,
        "SV-001",
        "Oil Change",
        "Oil change service",
        basePrice,
        30,
        isActive,
        DateTime.Now,
        DateTime.Now
    );

    private AddBudgetInput CreateInput(Guid serviceOrderId, IReadOnlyCollection<AddBudgetPartItem> parts, IReadOnlyCollection<AddBudgetServiceItem> services) => new(
        _fixture.Create<Guid>(),
        serviceOrderId,
        _fixture.Create<Guid>(),
        services,
        parts
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service order is Diagnosing and all referenced parts and services exist")]
    public async Task Handle_ShouldSuccess_WhenServiceOrderIsDiagnosingAndAllReferencedPartsAndServicesExist()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>(), 20, 50m);
        var service = CreateService(_fixture.Create<Guid>(), 120m);

        var input = CreateInput(
            serviceOrder.Id,
            [new AddBudgetPartItem(inventoryItem.Id, 2)],
            [new AddBudgetServiceItem(service.Id, 1)]
        );

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceOrderResponse>().Which;
        response.Status.Should().Be(ServiceOrderStatus.AwaitingApproval.ToString());
        response.Subtotal.Should().Be(220m);
        response.Discount.Should().Be(0m);
        response.Total.Should().Be(220m);
        response.Parts.Should().ContainSingle();
        response.Services.Should().ContainSingle();

        inventoryItem.ReservedQuantity.Should().Be(2);

        _inventoryItemRepositoryMock.Verify(x => x.Update(inventoryItem), Times.Once());
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(serviceOrder, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), [], []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service order by [{input.ServiceOrderId}].",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var missingInventoryItemId = _fixture.Create<Guid>();
        var input = CreateInput(serviceOrder.Id, [new AddBudgetPartItem(missingInventoryItemId, 2)], []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(missingInventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find inventory item by [{missingInventoryItemId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var missingServiceId = _fixture.Create<Guid>();
        var input = CreateInput(serviceOrder.Id, [], [new AddBudgetServiceItem(missingServiceId, 1)]);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(missingServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service by [{missingServiceId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service is inactive")]
    public async Task Handle_ShouldFail_WhenServiceIsInactive()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var service = CreateService(_fixture.Create<Guid>(), 120m, isActive: false);
        var input = CreateInput(serviceOrder.Id, [], [new AddBudgetServiceItem(service.Id, 1)]);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Service is inactive and cannot be added to a budget.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Service with id {service.Id} is inactive and cannot be added to a budget.",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item is inactive")]
    public async Task Handle_ShouldFail_WhenInventoryItemIsInactive()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>(), 20, 50m, isActive: false);
        var input = CreateInput(serviceOrder.Id, [new AddBudgetPartItem(inventoryItem.Id, 2)], []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Inventory item is inactive and cannot be added to a budget.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Inventory item with id {inventoryItem.Id} is inactive and cannot be added to a budget.",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service order")]
    public async Task Handle_ShouldFail_WhenErrorSavingServiceOrder()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder(_fixture.Create<Guid>());
        var input = CreateInput(serviceOrder.Id, [], []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating service order with id {input.ServiceOrderId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating service order with id {input.ServiceOrderId}.",
            Times.Once()
        );
    }
}
