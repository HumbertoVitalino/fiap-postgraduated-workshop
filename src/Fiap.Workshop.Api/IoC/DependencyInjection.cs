using System.Text;
using Asp.Versioning;
using Fiap.Workshop.Api.Handlers;
using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Api.Requests.InventoryItems;
using Fiap.Workshop.Api.Requests.Services;
using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Api.Validators.Auth;
using Fiap.Workshop.Api.Validators.Customers;
using Fiap.Workshop.Api.Validators.InventoryItems;
using Fiap.Workshop.Api.Validators.Services;
using Fiap.Workshop.Api.Validators.ServiceOrders;
using Fiap.Workshop.Api.Validators.Users;
using Fiap.Workshop.Api.Validators.Vehicles;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Fiap.Workshop.Api.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidators();
        services.AddRoles();
        services.AddJwtConfig(configuration);

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
        services.AddScoped<IValidator<CreateVehicleRequest>, CreateVehicleRequestValidator>();
        services.AddScoped<IValidator<CreateServiceRequest>, CreateServiceRequestValidator>();
        services.AddScoped<IValidator<CreateInventoryItemRequest>, CreateInventoryItemRequestValidator>();
        services.AddScoped<IValidator<CreateServiceOrderRequest>, CreateServiceOrderRequestValidator>();
        services.AddScoped<IValidator<StartDiagnosisRequest>, StartDiagnosisRequestValidator>();

        return services;
    }

    private static IServiceCollection AddRoles(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("AttendantOnly", policy => policy.RequireRole("Attendant", "Admin"))
            .AddPolicy("MechanicOnly", policy => policy.RequireRole("Mechanic", "Admin"));

        return services;
    }

    private static IServiceCollection AddJwtConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!))
                };
            });

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddProblemDetails();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Fiap.Workshop API",
                    Version = "v1"
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Enter your JWT token."
                    }
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
