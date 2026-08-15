using AutoFixture;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetCustomerUseCaseUnitTests : LoggerTestBase<GetCustomerUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetCustomerUseCase _useCase;

    public GetCustomerUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private GetCustomerInput CreateInput() => new(
        _fixture.Create<Guid>(),
        _fixture.Create<Guid>()
    );

    private static Customer CreateCustomer(Guid id) => new(
        id,
        "Jane Doe",
        "11144477735",
        "jane.doe@email.com",
        "11999999999",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When customer exists")]
    public async Task Handle_ShouldSuccess_WhenCustomerExists()
    {
        // Arrange
        var input = CreateInput();
        var customer = CreateCustomer(input.CustomerId);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();

        var response = result.Result.Should().BeOfType<CustomerResponse>().Which;
        response.Id.Should().Be(customer.Id);
        response.Name.Should().Be(customer.Name);
        response.Phone.Should().Be(customer.Phone);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateInput();

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find customer by [{input.CustomerId}].",
            Times.Once()
        );
    }
}
