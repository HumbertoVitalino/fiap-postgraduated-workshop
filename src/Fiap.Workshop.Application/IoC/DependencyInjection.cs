using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateCustomer;
using Fiap.Workshop.Application.UseCases.CreateUser;
using Fiap.Workshop.Application.UseCases.GetCustomer;
using Fiap.Workshop.Application.UseCases.GetUsers;
using Fiap.Workshop.Application.UseCases.LoginUser;
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

        return services;
    }
}
