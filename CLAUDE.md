# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET application that scrapes menu content from the Meyers restaurant website and provides both a beautiful web interface and an iCal calendar feed. The application uses Blazor Server-Side Rendering (SSR) for fast, static rendering with Tailwind CSS v4 for modern styling. It specifically extracts the "Det velkendte" menu section with persistent storage and automated caching.

## Architecture

- **Meyers.Web**: ASP.NET Core Blazor SSR application containing:
  - `Program.cs`: Main application entry point with Blazor SSR, API endpoints, and dependency injection
  - `Components/MainLayout.razor`: Main layout with modern glassmorphism design
  - `Components/Home.razor`: Homepage component showing menu preview and calendar URLs
  - `Components/Routes.razor`: Blazor routing configuration
  - `App.razor`: Root document with HTML structure
  - `Handlers/CalendarEndpointHandler.cs`: Clean separation of calendar API logic
  - `Handlers/MenuPreviewHandler.cs`: Handler for menu preview API endpoint
  - `Handlers/RefreshMenusHandler.cs`: Handler for admin menu refresh endpoint
  - `Services/MenuScrapingService.cs`: Web scraping logic using HtmlAgilityPack
  - `Services/CalendarService.cs`: iCal generation using Ical.Net
  - `Services/MenuCacheBackgroundService.cs`: Background service for automated menu data refresh
  - `Data/MenuDbContext.cs`: Entity Framework Core database context
  - `Models/MenuEntry.cs`: Database model for persisting menu data
  - `Repositories/IMenuRepository.cs` & `MenuRepository.cs`: Data access layer using repository pattern
  - `Configuration/MenuCacheOptions.cs`: Configuration options for cache intervals
  - `Styles/app.css`: Tailwind CSS v4 source configuration
  - `wwwroot/css/app.css`: Compiled CSS (auto-generated during build, served via MapStaticAssets)
  - `wwwroot/js/menu-app.js`: Client-side JavaScript for menu interactions and auto-refresh (served via MapStaticAssets with cache busting)
  - `package.json`: Node.js dependencies for Tailwind CSS compilation
  - `Migrations/`: Entity Framework Core database migrations
- **Meyers.Test**: xUnit test project with comprehensive test coverage including web interface tests

## Key Dependencies

### .NET Dependencies
- Microsoft.AspNetCore.Components.Web: Blazor SSR support
- HtmlAgilityPack: HTML parsing and web scraping
- Ical.Net: iCal calendar format generation
- Microsoft.AspNetCore.Mvc.Testing: Integration testing
- Microsoft.EntityFrameworkCore.Sqlite: Database persistence layer
- Microsoft.EntityFrameworkCore.Design: EF Core tooling support

### Frontend Dependencies (Node.js)
- @tailwindcss/cli v4.0.0-alpha.34: CSS framework with modern design system
- Node.js 20+: Required for Tailwind CSS compilation during build

## Common Development Commands

```bash
# Install Node.js dependencies for Tailwind CSS
cd Meyers.Web && npm install && cd ..

# Build the solution (automatically compiles Tailwind CSS)
dotnet build

# Run the web application
dotnet run --project Meyers.Web

# Run all tests (includes CSS compilation)
dotnet test

# Run tests with detailed output
dotnet test --logger console

# Manually compile Tailwind CSS for development
npm run dev --prefix Meyers.Web

# Build production CSS
npm run build --prefix Meyers.Web

# Add new migration
dotnet ef migrations add MigrationName --project Meyers.Web

# Apply database migrations
dotnet ef database update --project Meyers.Web
```

## Endpoints

- `GET /` - Beautiful Blazor SSR homepage with menu preview and calendar feed URLs
- `GET /calendar/{menuType}.ics` - Returns iCal calendar feed for specific menu type (e.g., `/calendar/det-velkendte.ics`, `/calendar/den-groenne.ics`)
- `GET /api/menu-types` - Returns available menu types with slugs
- `GET /api/menu-preview/{menuTypeId}` - Returns today's and tomorrow's menu preview for specific menu type
- `GET /admin/refresh-menus?secret={REFRESH_SECRET}` - Hidden endpoint for manual menu refresh (returns JSON with success status and menu count, requires REFRESH_SECRET environment variable)

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
- **Integration tests** for both web interface and API using real downloaded HTML fixtures
- **Blazor SSR testing** using TestWebApplicationFactory with proper service mocking
- **Mock HttpClient** for isolated unit testing with SQLite in-memory database  
- **Test data** stored in `TestData/meyers-menu-page.html` and `TestData/meyers-menu-page-current.html`
- **Comprehensive validation** of all 5 weekdays
- **Web interface tests** verifying HTML output, content types, and menu display
- **Calendar functionality tests** ensuring menu data is properly rendered in both formats
- **Specific tests** for day-specific features (e.g., "Torsdagssødt" on Thursday)

Tests verify complete weekly menu extraction, web interface rendering, and correct calendar event generation for proper dates.

## Coding Standards

- **Regular Expressions**: Always use `GeneratedRegex` attribute for regex patterns to improve performance and compile-time validation.
- **Repository Pattern**: Use the repository abstraction for all data access operations
- **Background Services**: Implement long-running tasks as hosted services rather than blocking API calls
- **Blazor SSR**: Use Server-Side Rendering for fast, static content delivery without websockets
- **Component Structure**: Keep layout, routing, and page components separate for maintainability
- **CSS Framework**: Use Tailwind CSS v4 with @theme directive for consistent design system
- **Build Integration**: Ensure Tailwind CSS compilation is integrated into MSBuild process
- **Static Assets**: Use .NET 9's MapStaticAssets() instead of UseStaticFiles() for improved performance and automatic cache busting
- **Asset References**: Use @Assets["path"] directive in Razor components for automatic fingerprinting and cache optimization
- **Testing**: When adding new endpoints or features, comprehensive tests must be added and all existing tests must be updated to reflect the changes. Tests should cover both success and error scenarios.

## Build System

The application includes automatic Tailwind CSS compilation:
- **MSBuild Targets**: `CheckNodeModules`, `BuildTailwindCss`, `CleanTailwindCss`
- **Auto-compilation**: CSS is built before `ResolveStaticWebAssetsInputs` to ensure availability
- **Docker Support**: Dockerfile includes Node.js 20.x installation for containerized builds
- **Development**: Use `npm run dev` for watch mode during development
- **Production**: Use `npm run build` for minified output
