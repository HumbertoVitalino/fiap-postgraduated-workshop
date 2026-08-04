using System.Text.RegularExpressions;
using Fiap.Workshop.Application.IoC;
using Fiap.Workshop.Infrastructure.IoC;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Fixtures;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultMasterConnectionString =
        "Server=localhost,1433;User Id=sa;Password=Integration@Test123;TrustServerCertificate=True;";

    private const string DefaultTestConnectionString =
        "Server=localhost,1433;Database=IntegrationTestsDb;User Id=sa;Password=Integration@Test123;TrustServerCertificate=True;";

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var masterConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MasterConnection")
            ?? DefaultMasterConnectionString;

        var testConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultTestConnectionString;

        await ExecuteInitScriptAsync(masterConnectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testConnectionString,
                ["Jwt:SecretKey"] = "integration-test-secret-key-min-32-chars!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpirationInMinutes"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);

        Services = services.BuildServiceProvider();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private const string TestDatabaseName = "IntegrationTestsDb";

    private static async Task ExecuteInitScriptAsync(string connectionString)
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "sql", "init.sql");
        var sql = await File.ReadAllTextAsync(sqlPath);
        sql = sql.Replace("$(DatabaseName)", TestDatabaseName);
        var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            await using var command = new SqlCommand(trimmed, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
