using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Meyers.Core.Interfaces;
using Meyers.Core.Models;
using Meyers.Core.Utilities;

namespace Meyers.Infrastructure.Services;

public partial class MenuScrapingService(HttpClient httpClient, IMenuRepository menuRepository) : IMenuScrapingService
{
    private const string Url = "https://meyers.dk/ugens-menuer";
    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromHours(6);

    public async Task<List<MenuDay>> ScrapeMenuAsync(bool forceRefresh = false)
    {
        return await ScrapeMenuAsync(forceRefresh, "API");
    }

    public async Task<List<MenuDay>> ScrapeMenuAsync(bool forceRefresh, string source)
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
        var freshMenus = await ScrapeFromWebsiteAsync(source);

        // Save to cache
        if (freshMenus.Count != 0) await SaveMenusToCache(freshMenus);

        return freshMenus;
    }

    private async Task<List<MenuDay>> GetCachedMenusAsync()
    {
        // Get all cached entries from the past week to future two weeks to ensure we don't miss any data
        // Use .Date to ensure we're working with date-only comparisons
        var startDate = DateTime.Today.AddDays(-7).Date;
        var endDate = DateTime.Today.AddDays(14).Date;
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

        // Deactivate menu types that no longer appear on the website
        var activeMenuTypeNames = menuDays.Select(d => d.MenuType).Distinct();
        await menuRepository.DeactivateMenuTypesNotInAsync(activeMenuTypeNames);
    }

    private async Task<List<MenuDay>> ScrapeFromWebsiteAsync(string source = "API")
    {
        var startTime = DateTime.UtcNow;
        var scrapingLog = new ScrapingLog
        {
            Timestamp = startTime,
            Source = source,
            RequestSuccessful = false,
            ParsingSuccessful = false,
            NewMenuItemsCount = 0
        };

        try
        {
            var html = await httpClient.GetStringAsync(Url);
            scrapingLog.RequestSuccessful = true;

            var menuDays = ParseNuxtData(html);

            scrapingLog.ParsingSuccessful = true;
            scrapingLog.NewMenuItemsCount = menuDays.Count;
            scrapingLog.Duration = DateTime.UtcNow - startTime;

            if (menuDays.Count == 0)
            {
                scrapingLog.ParsingSuccessful = false;
                scrapingLog.ErrorMessage = "No menu data found in __NUXT_DATA__";
            }

            try
            {
                await menuRepository.LogScrapingOperationAsync(scrapingLog);
            }
            catch
            {
                // Ignore logging errors to prevent breaking the scraping functionality
            }

            return menuDays;
        }
        catch (Exception ex)
        {
            scrapingLog.ErrorMessage = ex.Message;
            scrapingLog.Duration = DateTime.UtcNow - startTime;
            try
            {
                await menuRepository.LogScrapingOperationAsync(scrapingLog);
            }
            catch
            {
                // Ignore logging errors
            }

            throw;
        }
    }

    internal static List<MenuDay> ParseNuxtData(string html)
    {
        // Extract the __NUXT_DATA__ JSON array from the script tag
        var match = NuxtDataRegex().Match(html);
        if (!match.Success) return [];

        var jsonText = match.Groups[1].Value;
        JsonElement[] data;
        try
        {
            data = JsonSerializer.Deserialize<JsonElement[]>(jsonText) ?? [];
        }
        catch
        {
            return [];
        }

        if (data.Length == 0) return [];

        // Try the new FoodOp format first (has ISO dates and current data)
        var menuDays = ParseFoodopMenuBlocks(data);
        if (menuDays.Count > 0) return menuDays;

        // Fall back to the legacy Sanity menuBlock format
        return ParseLegacyMenuBlocks(data);
    }

    /// <summary>
    /// Parses the new FoodOp menu block format used since April 2026.
    /// Data is stored in top-level keys like "foodop-menu-block-Almanak" pointing to
    /// subsidiary entries with menus containing ISO dates and menu_sections with dishes.
    /// </summary>
    private static List<MenuDay> ParseFoodopMenuBlocks(JsonElement[] data)
    {
        var menuDays = new List<MenuDay>();

        // Find the data index object (element 3 typically) that contains foodop-menu-block-* keys
        int? foodopBlockIdx = null;
        for (var i = 0; i < Math.Min(data.Length, 10); i++)
        {
            if (data[i].ValueKind != JsonValueKind.Object) continue;
            foreach (var prop in data[i].EnumerateObject())
            {
                if (!prop.Name.StartsWith("foodop-menu-block-", StringComparison.Ordinal)) continue;
                foodopBlockIdx = prop.Value.GetInt32();
                break;
            }

            if (foodopBlockIdx.HasValue) break;
        }

        if (!foodopBlockIdx.HasValue) return menuDays;

        // Each foodop block contains ALL menu types, so we only need to parse one
        var blockRef = foodopBlockIdx.Value;
        if (blockRef >= data.Length || data[blockRef].ValueKind != JsonValueKind.Array) return menuDays;

        var blockArray = data[blockRef];
        if (blockArray.GetArrayLength() == 0) return menuDays;

        var subIdx = blockArray[0].GetInt32();
        if (subIdx >= data.Length || data[subIdx].ValueKind != JsonValueKind.Object) return menuDays;

        var subsidiary = data[subIdx];
        if (!subsidiary.TryGetProperty("menus", out var menusRef)) return menuDays;

        var menusIdx = menusRef.GetInt32();
        if (menusIdx >= data.Length || data[menusIdx].ValueKind != JsonValueKind.Array) return menuDays;

        var seen = new HashSet<string>();

        foreach (var menuEntryRef in data[menusIdx].EnumerateArray())
        {
            var entryIdx = menuEntryRef.GetInt32();
            if (entryIdx >= data.Length || data[entryIdx].ValueKind != JsonValueKind.Object) continue;

            var entry = data[entryIdx];
            if (!entry.TryGetProperty("date", out var dateRef) ||
                !entry.TryGetProperty("names", out var namesRef) ||
                !entry.TryGetProperty("menu_sections", out var sectionsRef)) continue;

            // Resolve date (ISO format like "2026-04-09")
            var dateStr = ResolveString(data, entry, "date");
            if (dateStr == null || !DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)) continue;

            // Resolve menu type name
            var namesIdx = namesRef.GetInt32();
            if (namesIdx >= data.Length || data[namesIdx].ValueKind != JsonValueKind.Object) continue;
            var namesObj = data[namesIdx];
            var menuTypeName = ResolveString(data, namesObj, "da") ?? "";
            if (string.IsNullOrEmpty(menuTypeName)) continue;

            // Deduplicate by date + menu type
            var key = $"{dateStr}|{menuTypeName}";
            if (!seen.Add(key)) continue;

            // Resolve menu sections
            var sectionsIdx = sectionsRef.GetInt32();
            if (sectionsIdx >= data.Length || data[sectionsIdx].ValueKind != JsonValueKind.Array) continue;

            var dayMenuItems = new List<string>();
            var mainDishContent = "";
            var detailsContent = "";

            foreach (var secRef in data[sectionsIdx].EnumerateArray())
            {
                var secIdx = secRef.GetInt32();
                if (secIdx >= data.Length || data[secIdx].ValueKind != JsonValueKind.Object) continue;

                var section = data[secIdx];
                if (!section.TryGetProperty("names", out var secNamesRef) ||
                    !section.TryGetProperty("menu_dishes", out var dishesRef)) continue;

                // Resolve section name
                var secNamesIdx = secNamesRef.GetInt32();
                if (secNamesIdx >= data.Length || data[secNamesIdx].ValueKind != JsonValueKind.Object) continue;
                var categoryName = ResolveString(data, data[secNamesIdx], "da") ?? "";

                var dishesIdx = dishesRef.GetInt32();
                if (dishesIdx >= data.Length || data[dishesIdx].ValueKind != JsonValueKind.Array) continue;

                foreach (var dishRef in data[dishesIdx].EnumerateArray())
                {
                    var dishIdx = dishRef.GetInt32();
                    if (dishIdx >= data.Length || data[dishIdx].ValueKind != JsonValueKind.Object) continue;

                    var dish = data[dishIdx];
                    if (!dish.TryGetProperty("names", out var dishNamesRef)) continue;

                    var dishNamesIdx = dishNamesRef.GetInt32();
                    if (dishNamesIdx >= data.Length || data[dishNamesIdx].ValueKind != JsonValueKind.Object) continue;
                    var title = ResolveString(data, data[dishNamesIdx], "da") ?? "";

                    title = WhitespaceRegex().Replace(title, " ").Trim();
                    if (string.IsNullOrEmpty(title)) continue;

                    dayMenuItems.Add($"{categoryName}: {title}");

                    // Extract main dish from "Varm ret" categories (prefer "alm" variant)
                    if (categoryName.Contains("Varm ret", StringComparison.OrdinalIgnoreCase) &&
                        categoryName.Contains("alm", StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrEmpty(mainDishContent))
                    {
                        var dishText = StripDietPrefix(title);
                        var (mainDish, details) = ExtractMainDishAndDetails(dishText);
                        mainDishContent = mainDish;
                        detailsContent = details;
                    }
                }
            }

            if (dayMenuItems.Count > 0)
            {
                if (string.IsNullOrEmpty(mainDishContent))
                    mainDishContent = StripDietPrefix(ExtractMainDishFromFirstItem(dayMenuItems[0]));

                mainDishContent = AllergenRegex().Replace(mainDishContent, "").Trim();
                mainDishContent = mainDishContent.TrimEnd('.', ' ');

                var weekdayName = DanishDateHelper.GetDanishWeekday(date);

                menuDays.Add(new MenuDay
                {
                    DayName = weekdayName,
                    Date = date,
                    MenuItems = dayMenuItems,
                    MainDish = mainDishContent,
                    Details = detailsContent,
                    MenuType = menuTypeName
                });
            }
        }

        return menuDays;
    }

    /// <summary>
    /// Parses the legacy Sanity-based menuBlock format (pre-April 2026).
    /// </summary>
    private static List<MenuDay> ParseLegacyMenuBlocks(JsonElement[] data)
    {
        var menuDays = new List<MenuDay>();

        // Find menuBlock entries by scanning for _type: "menuBlock"
        // The Nuxt data is a flat array where dicts use index references
        for (var i = 0; i < data.Length; i++)
        {
            if (data[i].ValueKind != JsonValueKind.Object) continue;

            var obj = data[i];
            if (!obj.TryGetProperty("_type", out var typeRef)) continue;
            if (!obj.TryGetProperty("menuTitle", out var titleRef)) continue;
            if (!obj.TryGetProperty("weeks", out var weeksRef)) continue;

            // Resolve the type string
            var typeIdx = typeRef.GetInt32();
            if (typeIdx >= data.Length || data[typeIdx].ValueKind != JsonValueKind.String) continue;
            if (data[typeIdx].GetString() != "menuBlock") continue;

            // Resolve the menu title
            var titleIdx = titleRef.GetInt32();
            if (titleIdx >= data.Length || data[titleIdx].ValueKind != JsonValueKind.String) continue;
            var menuTypeName = data[titleIdx].GetString()!;

            // Resolve the weeks array
            var weeksIdx = weeksRef.GetInt32();
            if (weeksIdx >= data.Length || data[weeksIdx].ValueKind != JsonValueKind.Array) continue;

            var weeksArray = data[weeksIdx];
            foreach (var weekIdxEl in weeksArray.EnumerateArray())
            {
                var weekIdx = weekIdxEl.GetInt32();
                if (weekIdx >= data.Length || data[weekIdx].ValueKind != JsonValueKind.Object) continue;

                var week = data[weekIdx];
                if (!week.TryGetProperty("days", out var daysRef) ||
                    !week.TryGetProperty("weekLabel", out var wlRef)) continue;

                var weekLabelIdx = wlRef.GetInt32();
                if (weekLabelIdx >= data.Length) continue;

                var weekLabel = data[weekLabelIdx].GetString() ?? "";
                var weekDates = ParseWeekDates(weekLabel);
                if (weekDates.Count == 0) continue;

                // Resolve days array
                var daysIdx = daysRef.GetInt32();
                if (daysIdx >= data.Length || data[daysIdx].ValueKind != JsonValueKind.Array) continue;

                var daysArray = data[daysIdx];
                var dayIndex = 0;

                foreach (var dayIdxEl in daysArray.EnumerateArray())
                {
                    if (dayIndex >= weekDates.Count) break;

                    var dayIdx = dayIdxEl.GetInt32();
                    if (dayIdx >= data.Length || data[dayIdx].ValueKind != JsonValueKind.Object) continue;

                    var day = data[dayIdx];
                    if (!day.TryGetProperty("menu", out var menuRef) ||
                        !day.TryGetProperty("weekday", out var wdRef)) continue;

                    // Resolve weekday name
                    var wdIdx = wdRef.GetInt32();
                    if (wdIdx >= data.Length || data[wdIdx].ValueKind != JsonValueKind.String) continue;
                    var weekdayName = StringHelper.CapitalizeFirst(data[wdIdx].GetString()!);

                    // Skip non-weekdays
                    if (!DanishDateHelper.IsWeekday(weekdayName))
                    {
                        dayIndex++;
                        continue;
                    }

                    var date = weekDates[dayIndex];

                    // Resolve menu -> categories
                    var menuIdx = menuRef.GetInt32();
                    if (menuIdx >= data.Length || data[menuIdx].ValueKind != JsonValueKind.Object) continue;

                    var menu = data[menuIdx];
                    if (!menu.TryGetProperty("categories", out var catsRef)) continue;

                    var catsIdx = catsRef.GetInt32();
                    if (catsIdx >= data.Length || data[catsIdx].ValueKind != JsonValueKind.Array) continue;

                    var dayMenuItems = new List<string>();
                    var mainDishContent = "";
                    var detailsContent = "";

                    foreach (var catIdxEl in data[catsIdx].EnumerateArray())
                    {
                        var catIdx = catIdxEl.GetInt32();
                        if (catIdx >= data.Length || data[catIdx].ValueKind != JsonValueKind.Object) continue;

                        var category = data[catIdx];
                        if (!category.TryGetProperty("name", out var catNameRef) ||
                            !category.TryGetProperty("items", out var itemsRef)) continue;

                        var catNameIdx = catNameRef.GetInt32();
                        var categoryName = catNameIdx < data.Length && data[catNameIdx].ValueKind == JsonValueKind.String
                            ? data[catNameIdx].GetString()!
                            : "";

                        var itemsIdx = itemsRef.GetInt32();
                        if (itemsIdx >= data.Length || data[itemsIdx].ValueKind != JsonValueKind.Array) continue;

                        foreach (var itemIdxEl in data[itemsIdx].EnumerateArray())
                        {
                            var itemIdx = itemIdxEl.GetInt32();
                            if (itemIdx >= data.Length || data[itemIdx].ValueKind != JsonValueKind.Object) continue;

                            var item = data[itemIdx];
                            var title = ResolveString(data, item, "title") ?? "";
                            var description = ResolveString(data, item, "description")?.Trim() ?? "";

                            // Normalize whitespace
                            title = WhitespaceRegex().Replace(title, " ").Trim();
                            description = WhitespaceRegex().Replace(description, " ").Trim();

                            if (string.IsNullOrEmpty(title)) continue;

                            var fullItem = string.IsNullOrEmpty(description)
                                ? $"{categoryName}: {title}"
                                : $"{categoryName}: {title} ({description})";
                            dayMenuItems.Add(fullItem);

                            // Extract main dish from "Varm ret" categories
                            if (categoryName.Contains("Varm ret", StringComparison.OrdinalIgnoreCase) &&
                                string.IsNullOrEmpty(mainDishContent))
                            {
                                // Strip diet prefix like "Alm./halal:", "Vegetarisk:" etc.
                                // to avoid the period in "Alm." being treated as a sentence ending
                                var dishText = StripDietPrefix(title);
                                var (mainDish, details) = ExtractMainDishAndDetails(dishText);
                                mainDishContent = mainDish;
                                detailsContent = details;
                            }
                        }
                    }

                    if (dayMenuItems.Count > 0)
                    {
                        if (string.IsNullOrEmpty(mainDishContent))
                            mainDishContent = StripDietPrefix(ExtractMainDishFromFirstItem(dayMenuItems[0]));

                        // Clean up main dish: remove allergens and trailing periods
                        mainDishContent = AllergenRegex().Replace(mainDishContent, "").Trim();
                        mainDishContent = mainDishContent.TrimEnd('.', ' ');

                        menuDays.Add(new MenuDay
                        {
                            DayName = weekdayName,
                            Date = date,
                            MenuItems = dayMenuItems,
                            MainDish = mainDishContent,
                            Details = detailsContent,
                            MenuType = menuTypeName
                        });
                    }

                    dayIndex++;
                }
            }
        }

        return menuDays;
    }

    private static string? ResolveString(JsonElement[] data, JsonElement obj, string property)
    {
        if (!obj.TryGetProperty(property, out var prop)) return null;

        // Null references in Nuxt data point to index 9 which holds null
        if (prop.ValueKind == JsonValueKind.Number)
        {
            var idx = prop.GetInt32();
            if (idx >= data.Length) return null;
            return data[idx].ValueKind == JsonValueKind.String ? data[idx].GetString() : null;
        }

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    /// <summary>
    /// Computes Mon-Fri dates from a week label like "Uge 11".
    /// Uses ISO 8601 week numbering. Year is inferred from proximity to current date.
    /// </summary>
    internal static List<DateTime> ParseWeekDates(string weekLabel)
    {
        var dates = new List<DateTime>();

        var weekMatch = WeekLabelRegex().Match(weekLabel);
        if (!weekMatch.Success) return dates;

        var weekNumber = int.Parse(weekMatch.Groups[1].Value);
        var year = DetermineYear(weekNumber);
        var monday = ISOWeek.GetYearStart(year).AddDays((weekNumber - 1) * 7);

        // Generate Mon-Fri dates
        for (var i = 0; i < 5; i++)
        {
            dates.Add(monday.AddDays(i));
        }

        return dates;
    }

    private static int DetermineYear(int weekNumber)
    {
        var now = DateTime.Today;
        var year = now.Year;

        // Handle year boundary: Dec showing week 1-2 = next year, Jan showing week 52+ = previous year
        if (now.Month >= 11 && weekNumber <= 2)
            return year + 1;
        if (now.Month <= 2 && weekNumber > 50)
            return year - 1;

        return year;
    }

    private static (string mainDish, string details) ExtractMainDishAndDetails(string plainText)
    {
        // Find the first sentence or phrase (up to the first period followed by space, or first 100 chars)
        var firstSentenceMatch = Regex.Match(plainText, @"^([^.]*\.)");

        string mainDish, details;

        if (firstSentenceMatch.Success && firstSentenceMatch.Groups[1].Value.Length < 150)
        {
            // Use the first sentence as main dish
            mainDish = firstSentenceMatch.Groups[1].Value.Trim();
            details = plainText[firstSentenceMatch.Length..].Trim();
        }
        else
        {
            // Fallback: use first 100 characters as main dish
            if (plainText.Length > 100)
            {
                var cutPoint = plainText.LastIndexOf(' ', 100);
                if (cutPoint > 50)
                {
                    mainDish = plainText[..cutPoint].Trim() + "...";
                    details = plainText[cutPoint..].Trim();
                }
                else
                {
                    mainDish = string.Concat(plainText.AsSpan(0, 100), "...");
                    details = plainText[100..].Trim();
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

    private static string ExtractMainDishFromFirstItem(string firstItem)
    {
        return StringHelper.ExtractMainDishFromFirstItem(firstItem);
    }

    /// <summary>
    /// Strips diet/variant prefixes like "Alm./halal:", "Vegetarisk:", "Vegansk:" from menu titles.
    /// </summary>
    private static string StripDietPrefix(string title)
    {
        var match = DietPrefixRegex().Match(title);
        return match.Success ? title[match.Length..].Trim() : title;
    }

    [GeneratedRegex(@"^(Alm\.?\s*/?\s*(halal|Halal)?\s*:?|Vegetarisk(/vegansk)?|Vegansk|Halal)\s*:?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex DietPrefixRegex();

    [GeneratedRegex(@"\([^)]*\)\s*")]
    private static partial Regex AllergenRegex();

    [GeneratedRegex(@"<script[^>]*id=""__NUXT_DATA__""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NuxtDataRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"Uge\s+(\d+)")]
    private static partial Regex WeekLabelRegex();
}
