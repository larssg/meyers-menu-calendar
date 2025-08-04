# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**IMPORTANT**: Keep this file and README.md up-to-date as the project evolves. Update both files when adding features,
changing architecture, or modifying key components.

## Project Overview

A .NET 9 Blazor SSR application that scrapes Meyers restaurant menus and provides iCal feeds for 8 different menu types.
Features multi-menu support, responsive design, and automatic caching.

## Architecture

**Clean Architecture Structure:**

### Meyers.Core (Domain Layer)

- `Models/MenuEntry.cs`: Menu data model with date, items, main dish, details
- `Models/MenuType.cs`: Menu type model with name, slug, active status
- `Models/MenuDay.cs`: DTO for menu data transfer
- `Interfaces/`: Repository and service contracts (IMenuRepository, IMenuScrapingService, ICalendarService)

### Meyers.Infrastructure (Data/External Layer)

- `Data/MenuDbContext.cs`: Entity Framework context with model configuration
- `Migrations/`: Database schema migrations
- `Repositories/MenuRepository.cs`: Data access with optimized queries
- `Services/MenuScrapingService.cs`: Scrapes all 8 menu types from meyers.dk
- `Services/CalendarService.cs`: Generates iCal feeds with 5-minute alarms and menu-type-specific UIDs
- `Services/MenuCacheBackgroundService.cs`: Background refresh every 6 hours
- `Configuration/MenuCacheOptions.cs`: Background service configuration

### Meyers.Web (Presentation Layer)

- `Components/Home.razor`: Multi-menu homepage with custom calendar builder UI
- `Handlers/`: API endpoints for calendar feeds and menu previews
- `wwwroot/js/menu-app.js`: Client-side functionality (toggleMenuMode, copyToClipboard, updateWeeklyPreview)
- `Styles/app.css`: Tailwind CSS v4 configuration

### Meyers.Test

- **54 comprehensive tests** with TestWebApplicationFactory, MockHttpMessageHandler, and real HTML fixtures
- Tests all 8 menu types, web interface, API endpoints, mobile responsiveness, CalendarService title cleanup, and
  MapStaticAssets fingerprinting

## Key Dependencies

- .NET 9.0 with MapStaticAssets
- HtmlAgilityPack (web scraping)
- Ical.Net (calendar generation)
- EntityFrameworkCore.Sqlite (database)
- Tailwind CSS v4 (styling)
- Node.js 20+ (CSS compilation)

## Common Development Commands

```bash
# Setup and run
cd Meyers.Web && npm install && cd ..
dotnet run --project Meyers.Web

# Testing (54 tests total)
dotnet test

# Database migrations (note: context is in Infrastructure but migrations run via Web project)
dotnet ef migrations add MigrationName --project Meyers.Web
dotnet ef database update --project Meyers.Web

# Build (includes Tailwind CSS compilation)
dotnet build
```

## Endpoints

- `GET /` - Homepage with multi-menu support and custom calendar builder
- `GET /calendar/{menu-type-slug}.ics` - iCal feed for specific menu type (with 5-minute alarms)
- `GET /calendar/custom/{config}.ics` - Custom mixed calendar (config format: M1T1W1R2F1 = Mon/Tue/Wed/Fri menu type 1,
  Thu menu type 2)
- `GET /api/menu-types` - Available menu types JSON
- `GET /admin/refresh-menus?secret=X` - Manual refresh endpoint (returns JSON with menu count)

## Data Persistence

SQLite database with MenuEntry and MenuType models. Repository pattern with optimized queries. Background service
refreshes every 6 hours. Unique constraints per date/menu type.

## Web Scraping Implementation

Scrapes meyers.dk tab structure. Auto-discovers menu types from `data-tab-content` attributes. Extracts dates from
`week-menu-day__header-heading` and menu items from `menu-recipe-display`. Handles HTML entities. Auto-creates menu
types. Supports all 8 menu types with clean title extraction and HTML entity decoding.

## Custom Calendar Feature

Users can create mixed calendars selecting different menu types per weekday:

- **Simple Mode**: One menu type for all days
- **Custom Mode**: Different menu type per weekday (Mon/Tue/Wed/Thu/Fri dropdowns)
- **Config Encoding**: `M1T1W1R2F1` = Monday(1), Tuesday(1), Wednesday(1), Thursday(2), Friday(1)
- **7-Day Preview**: Calendar-style preview showing selected menus for next 7 days
- **URL Generation**: `/calendar/custom/{config}.ics` with dynamic URL updates

## Background Processing

MenuCacheBackgroundService proactively refreshes menu data at 90% of cache lifetime (5.4 hours) to prevent request
handlers from triggering expensive scraping operations.

## Testing

Comprehensive test suite with TestWebApplicationFactory, MockHttpMessageHandler, and real HTML fixtures. Tests all 8
menu types, web interface, API endpoints, mobile responsiveness, and MapStaticAssets fingerprinting.

## Important Notes

- **Clean Architecture**: Follow dependency flow Core ← Infrastructure ← Web
- **CSS Assets**: Use direct path `css/app.css` for CSS (NOT @Assets - fails in production)
- **JS Assets**: Use `@Assets["js/menu-app.js"]` for JavaScript files
- **Regex**: Always use `GeneratedRegex` attribute
- **Database**: Repository pattern with optimized single queries in Infrastructure layer
- **Testing**: Update all tests when adding features (currently 54 tests)
- **Calendar Events**: All events include 5-minute alarm notifications
- **Title Cleanup**: CalendarService.CleanupTitle() only adds "..." when actually truncating content
- **Namespace Updates**: Use Infrastructure namespaces for moved services and repositories

## Build System

Tailwind CSS auto-compilation via MSBuild. .NET 9 MapStaticAssets for fingerprinting. CSS compiled before static asset
processing. Docker includes Node.js 20.x.
