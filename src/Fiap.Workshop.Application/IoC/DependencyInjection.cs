using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomers;
using Fiap.Workshop.Application.UseCases.Users.CreateUser;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle;
using Fiap.Workshop.Application.UseCases.Users.DeleteUser;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer;
using Fiap.Workshop.Application.UseCases.Users.GetUsers;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle;
using Fiap.Workshop.Application.UseCases.Users.LoginUser;
using Fiap.Workshop.Application.UseCases.Services.CreateService;
using Fiap.Workshop.Application.UseCases.Services.GetServices;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders;
using Fiap.Workshop.Application.UseCases.Customers.UpdateCustomer;
using Fiap.Workshop.Application.UseCases.Users.UpdateUser;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Application.IoC;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<ICreateCustomerUseCase, CreateCustomerUseCase>();
        services.AddScoped<IGetCustomerUseCase, GetCustomerUseCase>();
        services.AddScoped<IGetCustomersUseCase, GetCustomersUseCase>();
        services.AddScoped<IDeleteCustomerUseCase, DeleteCustomerUseCase>();
        services.AddScoped<IGetUsersUseCase, GetUsersUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();
        services.AddScoped<IUpdateCustomerUseCase, UpdateCustomerUseCase>();
        services.AddScoped<ICreateVehicleUseCase, CreateVehicleUseCase>();
        services.AddScoped<IGetVehicleUseCase, GetVehicleUseCase>();
        services.AddScoped<IGetVehiclesUseCase, GetVehiclesUseCase>();
        services.AddScoped<IUpdateVehicleUseCase, UpdateVehicleUseCase>();
        services.AddScoped<IDeleteVehicleUseCase, DeleteVehicleUseCase>();
        services.AddScoped<ICreateServiceUseCase, CreateServiceUseCase>();
        services.AddScoped<IGetServicesUseCase, GetServicesUseCase>();
        services.AddScoped<ICreateInventoryItemUseCase, CreateInventoryItemUseCase>();
        services.AddScoped<IGetInventoryItemsUseCase, GetInventoryItemsUseCase>();
        services.AddScoped<ICreateServiceOrderUseCase, CreateServiceOrderUseCase>();
        services.AddScoped<IGetServiceOrderUseCase, GetServiceOrderUseCase>();
        services.AddScoped<IStartDiagnosisUseCase, StartDiagnosisUseCase>();
        services.AddScoped<IAddBudgetUseCase, AddBudgetUseCase>();
        services.AddScoped<IApproveServiceOrderUseCase, ApproveServiceOrderUseCase>();
        services.AddScoped<IRejectServiceOrderUseCase, RejectServiceOrderUseCase>();
        services.AddScoped<ICompleteServiceOrderUseCase, CompleteServiceOrderUseCase>();
        services.AddScoped<IDeliverServiceOrderUseCase, DeliverServiceOrderUseCase>();
        services.AddScoped<ITrackServiceOrdersUseCase, TrackServiceOrdersUseCase>();

        return services;
    }
}
