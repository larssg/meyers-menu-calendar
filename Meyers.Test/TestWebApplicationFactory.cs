using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Meyers.Web.Data;

namespace Meyers.Test;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing database context registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MenuDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Create and open a SQLite connection that will be kept alive for the duration of the test
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Add SQLite in-memory database for testing
            services.AddDbContext<MenuDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Build the service provider and ensure the database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
            
            // Apply migrations to create the schema
            context.Database.Migrate();
        });
    }

    public new void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
    }
}