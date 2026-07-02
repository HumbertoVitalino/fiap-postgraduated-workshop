using Fiap.Workshop.Api.Endpoints;
using Fiap.Workshop.Api.IoC;
using Fiap.Workshop.Application.IoC;
using Fiap.Workshop.Infrastructure.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.Services.MigrateDatabase();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program;
