using Meyers.Core.Models;

namespace Meyers.Core.Interfaces;

public interface IMenuScrapingService
{
    Task<List<MenuDay>> ScrapeMenuAsync(bool forceRefresh = false);
    Task<List<MenuDay>> ScrapeMenuAsync(bool forceRefresh, string source);
}