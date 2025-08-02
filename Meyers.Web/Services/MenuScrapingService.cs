using HtmlAgilityPack;

namespace Meyers.Web.Services;

public class MenuScrapingService
{
    private readonly HttpClient _httpClient;
    
    public MenuScrapingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<List<MenuDay>> ScrapeMenuAsync()
    {
        var url = "https://meyers.dk/erhverv/frokostordning/ugens-menuer/";
        var html = await _httpClient.GetStringAsync(url);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var menuDays = new List<MenuDay>();
        
        // Look for text containing "Det velkendte" anywhere in the document
        var allText = doc.DocumentNode.InnerText;
        if (!allText.Contains("Det velkendte", StringComparison.OrdinalIgnoreCase))
        {
            return menuDays; // No "Det velkendte" found
        }
        
        // First, extract the dates from the week menu headers
        var dateMapping = ExtractDatesFromWeekHeaders(doc);
        
        // Look for "Det velkendte" tab content specifically
        var detVelkendteNodes = doc.DocumentNode.SelectNodes("//div[@data-tab-content='Det velkendte']");
        
        if (detVelkendteNodes != null && dateMapping.Any())
        {
            var dayIndex = 0;
            
            foreach (var tabNode in detVelkendteNodes)
            {
                // Only process weekdays and if we have date mapping
                if (dayIndex >= dateMapping.Count) break;
                
                var dayInfo = dateMapping[dayIndex];
                
                // Look for menu recipe displays within this tab
                var menuRecipes = tabNode.SelectNodes(".//div[contains(@class, 'menu-recipe-display')]");
                if (menuRecipes != null)
                {
                    var dayMenuItems = new List<string>();
                    
                    foreach (var recipe in menuRecipes)
                    {
                        var title = recipe.SelectSingleNode(".//h4[contains(@class, 'menu-recipe-display__title')]")?.InnerText?.Trim();
                        var description = recipe.SelectSingleNode(".//p[contains(@class, 'menu-recipe-display__description')]")?.InnerText?.Trim();
                        
                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(description))
                        {
                            // Clean up the description by removing HTML entities and extra whitespace
                            description = System.Net.WebUtility.HtmlDecode(description);
                            description = System.Text.RegularExpressions.Regex.Replace(description, @"\s+", " ").Trim();
                            
                            dayMenuItems.Add($"{title}: {description}");
                        }
                    }
                    
                    if (dayMenuItems.Any())
                    {
                        menuDays.Add(new MenuDay
                        {
                            DayName = dayInfo.DayName,
                            Date = dayInfo.Date,
                            MenuItems = dayMenuItems
                        });
                    }
                    
                    dayIndex++;
                }
            }
        }
        
        return menuDays;
    }
    
    private List<(string DayName, DateTime Date)> ExtractDatesFromWeekHeaders(HtmlDocument doc)
    {
        var dateMapping = new List<(string DayName, DateTime Date)>();
        
        // Look for day headers with dates like "mandag <span>28 jul, 2025</span>"
        var dayHeaders = doc.DocumentNode.SelectNodes("//h5[contains(@class, 'week-menu-day__header-heading')]");
        
        if (dayHeaders != null)
        {
            foreach (var header in dayHeaders)
            {
                var headerText = header.InnerText?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;
                
                // Parse format like "mandag 28 jul, 2025"
                var parts = headerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    var dayName = CapitalizeFirst(parts[0].Trim());
                    var dayNumber = parts[1].Trim();
                    var monthName = parts[2].Trim().Replace(",", "");
                    var year = parts[3].Trim();
                    
                    if (int.TryParse(dayNumber, out var day) && int.TryParse(year, out var yearInt))
                    {
                        var month = ParseDanishMonth(monthName);
                        if (month > 0)
                        {
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
                    }
                }
            }
        }
        
        // Only return weekdays (limit to 5 days)
        return dateMapping.Where(d => IsWeekday(d.DayName)).Take(5).ToList();
    }
    
    private int ParseDanishMonth(string monthName)
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
    
    private bool IsWeekday(string dayName)
    {
        var weekdays = new[] { "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag" };
        return weekdays.Contains(dayName, StringComparer.OrdinalIgnoreCase);
    }
    
    private string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }
}

public class MenuDay
{
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<string> MenuItems { get; set; } = new();
}