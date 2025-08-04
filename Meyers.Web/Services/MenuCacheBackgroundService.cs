using Microsoft.Extensions.Options;
using Meyers.Web.Configuration;
using Meyers.Web.Repositories;

namespace Meyers.Web.Services;

public class MenuCacheBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<MenuCacheBackgroundService> logger,
    IOptions<MenuCacheOptions> options)
    : BackgroundService
{
    private readonly MenuCacheOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Menu Cache Background Service started (Check: {CheckInterval}, Refresh: {RefreshInterval})",
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
                logger.LogError(ex, "Error occurred in Menu Cache Background Service");
                // Continue running even if an error occurs
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }

        logger.LogInformation("Menu Cache Background Service stopped");
    }

    private async Task RefreshCacheIfNeeded(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var menuRepository = scope.ServiceProvider.GetRequiredService<IMenuRepository>();
        var menuScrapingService = scope.ServiceProvider.GetRequiredService<MenuScrapingService>();

        var lastUpdate = await menuRepository.GetLastUpdateTimeAsync();

        // Refresh proactively BEFORE expiry to avoid request handlers triggering expensive operations
        // Use 90% of refresh interval as the threshold (e.g., refresh at 5.4 hours instead of 6 hours)
        var proactiveThreshold = TimeSpan.FromTicks((long)(_options.RefreshInterval.Ticks * 0.9));
        var shouldRefresh = lastUpdate == null || DateTime.UtcNow - lastUpdate.Value > proactiveThreshold;

        if (shouldRefresh)
        {
            var timeSinceUpdate = lastUpdate.HasValue ? DateTime.UtcNow - lastUpdate.Value : TimeSpan.Zero;
            logger.LogInformation(
                "Cache needs proactive refresh (last updated {TimeSinceUpdate} ago), refreshing menu data...",
                timeSinceUpdate);

            try
            {
                // Force refresh by calling the scraping service directly
                var menuDays = await menuScrapingService.ScrapeMenuAsync(forceRefresh: true);

                logger.LogInformation("Successfully refreshed menu cache with {Count} menu days", menuDays.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh menu cache");
            }
        }
        else
        {
            var timeSinceLastUpdate = DateTime.UtcNow - lastUpdate!.Value;
            logger.LogDebug("Cache is fresh (last updated {TimeSinceUpdate} ago), skipping refresh",
                timeSinceLastUpdate);
        }
    }
}