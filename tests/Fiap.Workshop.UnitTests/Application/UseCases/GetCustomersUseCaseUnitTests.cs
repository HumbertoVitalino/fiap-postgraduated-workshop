using AutoFixture;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomers;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomers.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetCustomersUseCaseUnitTests : LoggerTestBase<GetCustomersUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetCustomersUseCase _useCase;

    public GetCustomersUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    [Fact(DisplayName = "Handle >> Should Success >> When customers exist")]
    public async Task Handle_ShouldSuccess_WhenCustomersExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var customers = _fixture.CreateMany<Customer>();

        _customerRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(customers);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(customers.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When customers do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenCustomersDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _customerRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeAssignableTo<IEnumerable<CustomerResponse>>()
            .Which.Should().BeEmpty();
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find customers",
            Times.Once()
        );
    }
}
