using Fiap.Workshop.Api.Endpoints;
using Fiap.Workshop.Api.IoC;
using Fiap.Workshop.Application.IoC;
using Fiap.Workshop.Infrastructure.IoC;
using Fiap.Workshop.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();