using AutoFixture;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.UpdateCustomer;
using Fiap.Workshop.Application.UseCases.UpdateCustomer.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class UpdateCustomerUseCaseUnitTests : LoggerTestBase<UpdateCustomerUseCase>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly UpdateCustomerUseCase _useCase;

    public UpdateCustomerUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private UpdateCustomerInput CreateInput(Guid customerId, string email, string phone) => new(
        _fixture.Create<Guid>(),
        customerId,
        "Jane Doe",
        email,
        phone
    );

    private static Customer CreateCustomer(Guid id, string email, string phone) => new(
        id,
        "John Doe",
        "11144477735",
        email,
        phone,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When email and phone do not change")]
    public async Task Handle_ShouldSuccess_WhenEmailAndPhoneDoNotChange()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "same@example.com", "11999999999");
        var input = CreateInput(customer.Id, "same@example.com", "11999999999");

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<CustomerResponse>().Which;
        response.Id.Should().Be(customer.Id);
        response.Name.Should().Be(input.Name);

        _customerRepositoryMock.Verify(x => x.ExistsWithEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _customerRepositoryMock.Verify(x => x.ExistsWithPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _customerRepositoryMock.Verify(x => x.Update(customer), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Success >> When email and phone change and are not in use")]
    public async Task Handle_ShouldSuccess_WhenEmailAndPhoneChangeAndAreNotInUse()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "old@example.com", "11988887777");
        var input = CreateInput(customer.Id, "new@example.com", "11999998888");

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _customerRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.ExistsWithPhoneAsync(input.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<CustomerResponse>().Which;
        response.Id.Should().Be(customer.Id);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), "test@example.com", "11999999999");

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
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When new email already belongs to another customer")]
    public async Task Handle_ShouldFail_WhenNewEmailAlreadyBelongsToAnotherCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "old@example.com", "11988887777");
        var input = CreateInput(customer.Id, "taken@example.com", "11988887777");

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _customerRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Customer with email {input.Email} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Customer with email {input.Email} already exists.",
            Times.Once()
        );
        _customerRepositoryMock.Verify(x => x.ExistsWithPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _customerRepositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When new phone already belongs to another customer")]
    public async Task Handle_ShouldFail_WhenNewPhoneAlreadyBelongsToAnotherCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "old@example.com", "11988887777");
        var input = CreateInput(customer.Id, "old@example.com", "11999998888");

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _customerRepositoryMock.Setup(x => x.ExistsWithPhoneAsync(input.Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Customer with phone {input.Phone} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Customer with phone {input.Phone} already exists.",
            Times.Once()
        );
        _customerRepositoryMock.Verify(x => x.Update(It.IsAny<Customer>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error updating customer")]
    public async Task Handle_ShouldFail_WhenErrorUpdatingCustomer()
    {
        // Arrange
        var customer = CreateCustomer(_fixture.Create<Guid>(), "same@example.com", "11999999999");
        var input = CreateInput(customer.Id, "same@example.com", "11999999999");

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(input.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating customer with id {input.CustomerId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating customer with id {input.CustomerId}.",
            Times.Once()
        );
    }
}
