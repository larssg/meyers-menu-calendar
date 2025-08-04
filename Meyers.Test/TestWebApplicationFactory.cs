using Meyers.Web.Data;
using Meyers.Web.Repositories;
using Meyers.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meyers.Test;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection? _connection;

    public new void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment variable for refresh secret
        Environment.SetEnvironmentVariable("REFRESH_SECRET", "test-secret-123");

        builder.ConfigureServices(services =>
        {
            // Remove the existing database context registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MenuDbContext>));

            if (descriptor != null) services.Remove(descriptor);

            // Create and open a SQLite connection that will be kept alive for the duration of the test
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            // Add SQLite in-memory database for testing
            services.AddDbContext<MenuDbContext>(options => { options.UseSqlite(_connection); });

            // Ensure HttpContextAccessor is registered for Blazor SSR
            services.AddHttpContextAccessor();

            // Replace MenuScrapingService with test version
            services.RemoveAll<MenuScrapingService>();
            services.RemoveAll<HttpClient>();

            // Create a test version that uses local data
            var testHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "meyers-menu-page.html");
            var mockHandler = new MockHttpMessageHandler(testHtmlPath);
            var mockHttpClient = new HttpClient(mockHandler);
            services.AddSingleton(mockHttpClient);
            services.AddScoped<MenuScrapingService>(provider =>
            {
                var repository = provider.GetRequiredService<IMenuRepository>();
                return new MenuScrapingService(mockHttpClient, repository);
            });

            // Build the service provider and ensure the database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();

            // Apply migrations to create the schema
            context.Database.Migrate();
        });
    }
}