using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer;
using Fiap.Workshop.Application.UseCases.Users.CreateUser;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle;
using Fiap.Workshop.Application.UseCases.Users.DeleteUser;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer;
using Fiap.Workshop.Application.UseCases.Users.GetUsers;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle;
using Fiap.Workshop.Application.UseCases.Users.LoginUser;
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
        services.AddScoped<IGetUsersUseCase, GetUsersUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();
        services.AddScoped<IUpdateCustomerUseCase, UpdateCustomerUseCase>();
        services.AddScoped<ICreateVehicleUseCase, CreateVehicleUseCase>();
        services.AddScoped<IGetVehicleUseCase, GetVehicleUseCase>();

        return services;
    }
}
