using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.CreateCustomer;
using Fiap.Workshop.Application.UseCases.CreateCustomer.Boundaries;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateCustomerUseCaseUnitTests : LoggerTestBase<CreateCustomerUseCase>
{
    private const string ValidDocument = "11144477735";

    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private CreateCustomerUseCase _useCase;

    public CreateCustomerUseCaseUnitTests()
    {
        _useCase = new(
            _customerRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private CreateCustomerInput CreateInput() => new(
        _fixture.Create<Guid>(),
        _fixture.Create<string>(),
        ValidDocument,
        _fixture.Create<string>(),
        _fixture.Create<string>()
    );

    [Fact(DisplayName = "Handle >> Should Success >> When customer is valid and does not exist")]
    public async Task Handle_ShouldSuccess_WhenCustomerIsValidAndDoesNotExist()
    {
        // Arrange
        var input = CreateInput();

        _customerRepositoryMock.Setup(x => x.AnyAsync(input.Document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When customer already exists")]
    public async Task Handle_ShouldFail_WhenCustomerAlreadyExists()
    {
        // Arrange
        var input = CreateInput();

        _customerRepositoryMock.Setup(x => x.AnyAsync(input.Document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Customer with the provided document already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Customer with this document already exists.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving customer")]
    public async Task Handle_ShouldFail_WhenErrorSavingCustomer()
    {
        // Arrange
        var input = CreateInput();

        _customerRepositoryMock.Setup(x => x.AnyAsync(input.Document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _customerRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Failed to save customer to the database.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Failed to save customer to the database.",
            Times.Once()
        );
    }
}
