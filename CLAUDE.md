# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET minimal API application that scrapes menu content from the Meyers restaurant website and provides it as an iCal calendar feed. The application specifically extracts the "Det velkendte" menu section with persistent storage and automated caching.

## Architecture

- **Meyers.Web**: ASP.NET Core minimal API project containing:
  - `Program.cs`: Main application entry point with endpoints and dependency injection
  - `Services/MenuScrapingService.cs`: Web scraping logic using HtmlAgilityPack
  - `Services/CalendarService.cs`: iCal generation using Ical.Net
  - `Services/MenuCacheBackgroundService.cs`: Background service for automated menu data refresh
  - `Data/MenuDbContext.cs`: Entity Framework Core database context
  - `Models/MenuEntry.cs`: Database model for persisting menu data
  - `Repositories/IMenuRepository.cs` & `MenuRepository.cs`: Data access layer using repository pattern
  - `Configuration/MenuCacheOptions.cs`: Configuration options for cache intervals
  - `Migrations/`: Entity Framework Core database migrations
- **Meyers.Test**: xUnit test project with comprehensive test coverage

## Key Dependencies

- HtmlAgilityPack: HTML parsing and web scraping
- Ical.Net: iCal calendar format generation
- Microsoft.AspNetCore.Mvc.Testing: Integration testing
- Microsoft.EntityFrameworkCore.Sqlite: Database persistence layer
- Microsoft.EntityFrameworkCore.Design: EF Core tooling support

## Common Development Commands

```bash
# Build the solution
dotnet build

# Run the web application
dotnet run --project Meyers.Web

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger console

# Add new migration
dotnet ef migrations add MigrationName --project Meyers.Web

# Apply database migrations
dotnet ef database update --project Meyers.Web
```

## API Endpoints

- `GET /` - Returns API description
- `GET /calendar` - Returns iCal calendar feed with Meyers "Det velkendte" menu (current + historical data)
- `GET /calendar.ics` - Alternative endpoint for the same iCal feed

## Data Persistence

The application uses SQLite database for menu data persistence:
- **MenuEntry** model stores daily menu data with timestamps
- **MenuDbContext** manages database operations
- **Repository pattern** provides clean data access abstraction
- **Background service** automatically refreshes menu data every 6 hours (configurable)
- **Historical data** is preserved and included in calendar output (last month + future month)

## Web Scraping Implementation

The scraper targets the Meyers website's tab-based structure:
- Extracts actual dates from `week-menu-day__header-heading` elements (e.g., "mandag 28 jul, 2025")
- Maps multiple `data-tab-content="Det velkendte"` elements to correct dates
- Extracts menu items from `menu-recipe-display` CSS classes
- Returns structured menu data with correct dates (not calculated dates)
- Handles HTML entity encoding (e.g., `&#248;` for ø)
- **Persists scraped data** to database for historical access and performance

## Background Processing

- **MenuCacheBackgroundService** runs continuously to keep menu data fresh
- Configurable intervals via `MenuCacheOptions`:
  - Check interval: 30 minutes (default)
  - Refresh interval: 6 hours (default)
  - Startup delay: 30 seconds (default)
- Ensures calendar feed always includes current data without blocking API requests

## Testing

The test suite includes:
- Integration tests using real downloaded HTML fixtures
- Mock HttpClient for isolated unit testing  
- Test data stored in `TestData/meyers-menu-page.html` and `TestData/meyers-menu-page-current.html`
- Comprehensive validation of all 5 weekdays
- Verification that each day has different menu content
- Specific tests for day-specific features (e.g., "Torsdagssødt" on Thursday)

Tests verify complete weekly menu extraction and correct calendar event generation for proper dates.

## Coding Standards

- **Regular Expressions**: Always use `GeneratedRegex` attribute for regex patterns to improve performance and compile-time validation.
- **Repository Pattern**: Use the repository abstraction for all data access operations
- **Background Services**: Implement long-running tasks as hosted services rather than blocking API calls
