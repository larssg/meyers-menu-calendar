using Microsoft.Extensions.Options;
using Meyers.Web.Configuration;
using Meyers.Web.Repositories;

namespace Meyers.Web.Services;

public class MenuCacheBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MenuCacheBackgroundService> _logger;
    private readonly MenuCacheOptions _options;

    public MenuCacheBackgroundService(
        IServiceProvider serviceProvider, 
        ILogger<MenuCacheBackgroundService> logger,
        IOptions<MenuCacheOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Menu Cache Background Service started (Check: {CheckInterval}, Refresh: {RefreshInterval})", 
            _options.CheckInterval, _options.RefreshInterval);

        // Initial delay to let the application start up
        await Task.Delay(_options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshCacheIfNeeded(stoppingToken);
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Menu Cache Background Service");
                // Continue running even if an error occurs
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }

        _logger.LogInformation("Menu Cache Background Service stopped");
    }

    private async Task RefreshCacheIfNeeded(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var menuRepository = scope.ServiceProvider.GetRequiredService<IMenuRepository>();
        var menuScrapingService = scope.ServiceProvider.GetRequiredService<MenuScrapingService>();

        var lastUpdate = await menuRepository.GetLastUpdateTimeAsync();
        var shouldRefresh = lastUpdate == null || DateTime.UtcNow - lastUpdate.Value > _options.RefreshInterval;

        if (shouldRefresh)
        {
            _logger.LogInformation("Cache is stale, refreshing menu data...");
            
            try
            {
                // Force refresh by calling the scraping service
                // The service will automatically cache the results
                var menuDays = await menuScrapingService.ScrapeMenuAsync();
                
                _logger.LogInformation("Successfully refreshed menu cache with {Count} menu days", menuDays.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh menu cache");
            }
        }
        else
        {
            var timeSinceLastUpdate = DateTime.UtcNow - lastUpdate!.Value;
            _logger.LogDebug("Cache is fresh (last updated {TimeSinceUpdate} ago), skipping refresh", timeSinceLastUpdate);
        }
    }
}