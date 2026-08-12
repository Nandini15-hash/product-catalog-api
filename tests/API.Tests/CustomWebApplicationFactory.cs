using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Spins up the real API pipeline (auth, filters, middleware, controllers) but swaps
/// the relational EF Core provider for a uniquely-named InMemory database per factory
/// instance, so integration tests never touch a real SQL Server/SQLite file.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            // Guarantee the JWT settings the running host needs exist even if the
            // test host can't locate the API project's appsettings.json on disk.
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-only-super-secret-signing-key-min-32-chars!!",
                ["Jwt:Issuer"] = "ProductCatalog.API.Tests",
                ["Jwt:Audience"] = "ProductCatalog.Client.Tests",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["DatabaseProvider"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
