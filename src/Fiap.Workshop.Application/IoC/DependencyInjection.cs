using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser;
using Fiap.Workshop.Application.UseCases.Users.GetUserById;
using Fiap.Workshop.Application.UseCases.Users.Login;
using Fiap.Workshop.Application.UseCases.Users.UpdateEmail;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.Workshop.Application.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IUpdateEmailUseCase, UpdateEmailUseCase>();

        return services;
    }
}
