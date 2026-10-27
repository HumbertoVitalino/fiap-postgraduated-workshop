using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.EventHandlers;

[Collection("Integration")]
public sealed class SendStatusChangedEmailHandlerIntegrationTests(DatabaseFixture fixture)
{
    private FakeEmailService Emails => fixture.Services.GetRequiredService<FakeEmailService>();

    private async Task<Guid> CreateUserAsync()
    {
        var user = new User(
            Guid.NewGuid(), TestData.Email(), "Attendant", TestData.ShortString(32), UserRole.Attendant,
            DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        return user.Id;
    }

    private async Task<(ServiceOrderResponse ServiceOrder, string CustomerEmail)> CreateServiceOrderAsync()
    {
        using var scope = fixture.Services.CreateScope();

        var customerInput = new CreateCustomerInput(
            Guid.NewGuid(), "Maria Silva", TestData.Document(), TestData.Email(), TestData.ShortString(15));
        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(customerInput, CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var vehicleInput = new CreateVehicleInput(
            Guid.NewGuid(), customerId, TestData.LicensePlate(), "Ford", "Ka", 2020, 2021, "Black");
        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(vehicleInput, CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var createdBy = await CreateUserAsync();

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var serviceOrderInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, createdBy, "Engine noise", 12000);
        var serviceOrderResult = await createServiceOrderUseCase.Handle(serviceOrderInput, CancellationToken.None);

        return (serviceOrderResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject, customerInput.Email);
    }

    private async Task StartDiagnosisAsync(Guid serviceOrderId, Guid mechanicId)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        await useCase.Handle(new StartDiagnosisInput(Guid.NewGuid(), serviceOrderId, mechanicId, "Worn timing belt"), CancellationToken.None);
    }

    [Fact(DisplayName = "SendStatusChangedEmailHandler >> Should not email the customer >> When service order is opened")]
    public async Task Handle_ShouldNotEmailTheCustomer_WhenServiceOrderIsOpened()
    {
        // Act
        var (_, customerEmail) = await CreateServiceOrderAsync();

        // Assert
        Emails.Sent.Should().NotContain(email => email.To == customerEmail);
    }

    [Fact(DisplayName = "SendStatusChangedEmailHandler >> Should email the customer once >> When service order status changes")]
    public async Task Handle_ShouldEmailTheCustomerOnce_WhenServiceOrderStatusChanges()
    {
        // Arrange
        var (serviceOrder, customerEmail) = await CreateServiceOrderAsync();
        var mechanicId = await CreateUserAsync();

        // Act — the second call is an invalid transition (already Diagnosing) and must not send another email.
        await StartDiagnosisAsync(serviceOrder.Id, mechanicId);
        var repeated = () => StartDiagnosisAsync(serviceOrder.Id, mechanicId);

        // Assert
        await repeated.Should().ThrowAsync<DomainException>();

        var email = Emails.Sent.Should().ContainSingle(sent => sent.To == customerEmail).Subject;
        email.Subject.Should().Be("Sua ordem de serviço agora está \"Em diagnóstico\"");
        email.Body.Should().Contain("Olá, Maria Silva.");
        email.Body.Should().Contain("mudou de \"Recebida\" para \"Em diagnóstico\"");
    }
}
