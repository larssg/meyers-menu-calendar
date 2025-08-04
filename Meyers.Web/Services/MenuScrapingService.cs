using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Meyers.Web.Models;
using Meyers.Web.Repositories;
using static System.Net.WebUtility;

namespace Meyers.Web.Services;

public partial class MenuScrapingService(HttpClient httpClient, IMenuRepository menuRepository)
{
    private const string Url = "https://meyers.dk/erhverv/frokostordning/ugens-menuer/";
    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromHours(6);

    public async Task<List<MenuDay>> ScrapeMenuAsync(bool forceRefresh = false)
    {
        // Check if we need to refresh the cache
        var lastUpdate = await menuRepository.GetLastUpdateTimeAsync();
        var shouldRefresh = forceRefresh || lastUpdate == null ||
                            DateTime.UtcNow - lastUpdate.Value > CacheRefreshInterval;

        if (!shouldRefresh)
        {
            // Return cached data
            var cachedMenus = await GetCachedMenusAsync();
            if (cachedMenus.Count != 0) return cachedMenus;
        }

        // Scrape fresh data from the website
        var freshMenus = await ScrapeFromWebsiteAsync();

        // Save to cache
        if (freshMenus.Count != 0) await SaveMenusToCache(freshMenus);

        return freshMenus;
    }

    private async Task<List<MenuDay>> GetCachedMenusAsync()
    {
        // Get all cached entries from the past week to future two weeks to ensure we don't miss any data
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today.AddDays(14);
        var cachedEntries = await menuRepository.GetMenusForDateRangeAsync(startDate, endDate);

        return cachedEntries.Select(entry => new MenuDay
        {
            DayName = entry.DayName,
            Date = entry.Date,
            MenuItems = string.IsNullOrEmpty(entry.MenuItems)
                ? []
                : entry.MenuItems.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
            MainDish = entry.MainDish,
            Details = entry.Details,
            MenuType = entry.MenuType?.Name ?? "Det velkendte"
        }).ToList();
    }

    private async Task SaveMenusToCache(List<MenuDay> menuDays)
    {
        var menuEntries = new List<MenuEntry>();

        foreach (var day in menuDays)
        {
            var menuType = await menuRepository.GetOrCreateMenuTypeAsync(day.MenuType);

            menuEntries.Add(new MenuEntry
            {
                Date = day.Date,
                DayName = day.DayName,
                MenuItems = string.Join('\n', day.MenuItems),
                MainDish = day.MainDish,
                Details = day.Details,
                MenuTypeId = menuType.Id
            });
        }

        await menuRepository.SaveMenusAsync(menuEntries);
    }

    private async Task<List<MenuDay>> ScrapeFromWebsiteAsync()
    {
        var html = await httpClient.GetStringAsync(Url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var menuDays = new List<MenuDay>();

        // First, extract the dates from the week menu headers
        var dateMapping = ExtractDatesFromWeekHeaders(doc);
        if (dateMapping.Count == 0) return menuDays;

        // Discover all available menu types from data-tab-content attributes
        var allTabContentNodes = doc.DocumentNode.SelectNodes("//div[@data-tab-content]");
        if (allTabContentNodes == null) return menuDays;

        // Get unique menu types and decode HTML entities
        var menuTypes = allTabContentNodes
            .Select(node => node.GetAttributeValue("data-tab-content", ""))
            .Where(content => !string.IsNullOrEmpty(content))
            .Select(content => HtmlDecode(content)) // Decode HTML entities like "Den Gr&#248;nne" -> "Den Grønne"
            .Distinct()
            .ToList();

        // Process each menu type
        foreach (var menuType in menuTypes)
        {
            // We need to search for both the decoded and original encoded versions
            // since the HTML might have the encoded version in attributes
            var encodedMenuType = HtmlEncode(menuType);
            var tabNodes =
                doc.DocumentNode.SelectNodes(
                    $"//div[@data-tab-content='{menuType}' or @data-tab-content='{encodedMenuType}']");
            if (tabNodes == null) continue;

            var dayIndex = 0;

            foreach (var tabNode in tabNodes)
            {
                // Only process weekdays and if we have date mapping
                if (dayIndex >= dateMapping.Count) break;

                var dayInfo = dateMapping[dayIndex];

                // Look for menu recipe displays within this tab
                var menuRecipes = tabNode.SelectNodes(".//div[contains(@class, 'menu-recipe-display')]");
                if (menuRecipes != null)
                {
                    var dayMenuItems = new List<string>();
                    var mainDishContent = "";
                    var detailsContent = "";

                    foreach (var recipe in menuRecipes)
                    {
                        var titleNode =
                            recipe.SelectSingleNode(".//h4[contains(@class, 'menu-recipe-display__title')]");
                        var title = titleNode?.InnerText?.Trim();

                        // Normalize whitespace in title (remove newlines and multiple spaces)
                        if (!string.IsNullOrEmpty(title)) title = Regex.Replace(title, @"\s+", " ").Trim();
                        var descriptionNode =
                            recipe.SelectSingleNode(".//p[contains(@class, 'menu-recipe-display__description')]");

                        if (!string.IsNullOrEmpty(title))
                        {
                            // Get the plain text content and clean it up
                            var plainText = HtmlDecode(descriptionNode?.InnerText?.Trim() ?? "");
                            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

                            if (!string.IsNullOrEmpty(plainText))
                            {
                                // For backward compatibility, also add to MenuItems
                                var fullDescription = HtmlDecode(descriptionNode?.InnerText?.Trim() ?? "");
                                fullDescription = Regex
                                    .Replace(fullDescription, @"\s+", " ").Trim();
                                dayMenuItems.Add($"{title}: {fullDescription}");

                                // Only extract main dish from "Varm ret med tilbehør" section
                                if (title.Contains("Varm ret med tilbehør", StringComparison.OrdinalIgnoreCase))
                                {
                                    var (mainDish, details) = ExtractMainDishAndDetails(plainText);
                                    mainDishContent = mainDish;
                                    detailsContent = details;
                                }
                            }
                        }
                    }

                    if (dayMenuItems.Any())
                    {
                        // If we didn't find a main dish in "Varm ret med tilbehør", use the first menu item
                        if (string.IsNullOrEmpty(mainDishContent) && dayMenuItems.Count > 0)
                            mainDishContent = ExtractMainDishFromFirstItem(dayMenuItems[0]);

                        menuDays.Add(new MenuDay
                        {
                            DayName = dayInfo.DayName,
                            Date = dayInfo.Date,
                            MenuItems = dayMenuItems,
                            MainDish = mainDishContent,
                            Details = detailsContent,
                            MenuType = menuType
                        });
                    }
                }

                dayIndex++;
            }
        }

        return menuDays;
    }

    private (string mainDish, string details) ExtractMainDishAndDetails(string plainText)
    {
        // Find the first sentence or phrase (up to the first period followed by space, or first 100 chars)
        var firstSentenceMatch = Regex.Match(plainText, @"^([^.]*\.)");

        string mainDish, details;

        if (firstSentenceMatch.Success && firstSentenceMatch.Groups[1].Value.Length < 150)
        {
            // Use the first sentence as main dish
            mainDish = firstSentenceMatch.Groups[1].Value.Trim();
            details = plainText.Substring(firstSentenceMatch.Length).Trim();
        }
        else
        {
            // Fallback: use first 100 characters as main dish
            if (plainText.Length > 100)
            {
                var cutPoint = plainText.LastIndexOf(' ', 100);
                if (cutPoint > 50)
                {
                    mainDish = plainText.Substring(0, cutPoint).Trim() + "...";
                    details = plainText.Substring(cutPoint).Trim();
                }
                else
                {
                    mainDish = plainText.Substring(0, 100) + "...";
                    details = plainText.Substring(100).Trim();
                }
            }
            else
            {
                mainDish = plainText;
                details = "";
            }
        }

        // Remove allergen info for cleaner display
        mainDish = AllergenRegex().Replace(mainDish, "").Trim();
        details = AllergenRegex().Replace(details, "").Trim();

        return (mainDish, details);
    }

    private string ExtractMainDishFromFirstItem(string firstItem)
    {
        var colonIndex = firstItem.IndexOf(':');
        if (colonIndex > 0 && colonIndex < firstItem.Length - 1)
        {
            var content = firstItem.Substring(colonIndex + 1).Trim();
            if (content.Length > 100) content = content.Substring(0, 100).Trim() + "...";

            return content;
        }

        return firstItem;
    }

    private List<(string DayName, DateTime Date)> ExtractDatesFromWeekHeaders(HtmlDocument doc)
    {
        var dateMapping = new List<(string DayName, DateTime Date)>();

        // Look for day headers with dates like "mandag <span>28 jul, 2025</span>"
        var dayHeaders = doc.DocumentNode.SelectNodes("//h5[contains(@class, 'week-menu-day__header-heading')]");

        if (dayHeaders != null)
            foreach (var header in dayHeaders)
            {
                var headerText = header.InnerText?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                // Parse format like "mandag 28 jul, 2025"
                var parts = headerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                var dayName = CapitalizeFirst(parts[0].Trim());
                var dayNumber = parts[1].Trim();
                var monthName = parts[2].Trim().Replace(",", "");
                var year = parts[3].Trim();

                if (!int.TryParse(dayNumber, out var day) || !int.TryParse(year, out var yearInt)) continue;

                var month = ParseDanishMonth(monthName);
                if (month <= 0) continue;

                try
                {
                    var date = new DateTime(yearInt, month, day);
                    dateMapping.Add((dayName, date));
                }
                catch
                {
                    // Skip invalid dates
                }
            }

        // Only return weekdays (both weeks - up to 10 days)
        return dateMapping.Where(d => IsWeekday(d.DayName)).ToList();
    }

    private static int ParseDanishMonth(string monthName)
    {
        return monthName.ToLowerInvariant() switch
        {
            "jan" => 1,
            "feb" => 2,
            "mar" => 3,
            "apr" => 4,
            "maj" => 5,
            "jun" => 6,
            "jul" => 7,
            "aug" => 8,
            "sep" => 9,
            "okt" => 10,
            "nov" => 11,
            "dec" => 12,
            _ => 0
        };
    }

    private static bool IsWeekday(string dayName)
    {
        string[] weekdays = ["Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag"];
        return weekdays.Contains(dayName, StringComparer.OrdinalIgnoreCase);
    }

    private static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }

    [GeneratedRegex(@"\([^)]*\)\s*")]
    private static partial Regex AllergenRegex();
}

public class MenuDay
{
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<string> MenuItems { get; set; } = [];
    public string MainDish { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string MenuType { get; set; } = string.Empty;
}