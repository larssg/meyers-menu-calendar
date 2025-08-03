# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 9 Blazor SSR application that scrapes Meyers restaurant menus and provides iCal feeds for 8 different menu types. Features multi-menu support, responsive design, and automatic caching.

## Architecture

**Key Components:**
- `Components/Home.razor`: Multi-menu homepage with responsive tabs
- `Services/MenuScrapingService.cs`: Scrapes all 8 menu types from meyers.dk
- `Services/CalendarService.cs`: Generates iCal feeds with menu-type-specific UIDs
- `Handlers/`: API endpoints for calendar feeds and menu previews
- `Models/`: MenuEntry and MenuType database models with relationships
- `wwwroot/js/menu-app.js`: Client-side tab navigation and clipboard functionality
- `Styles/app.css`: Tailwind CSS v4 configuration
- **Meyers.Test**: Comprehensive test suite with MockHttpMessageHandler

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

# Testing
dotnet test

# Database migrations
dotnet ef migrations add MigrationName --project Meyers.Web
dotnet ef database update --project Meyers.Web
```

## Endpoints

- `GET /` - Homepage with multi-menu support
- `GET /calendar/{menu-type-slug}.ics` - iCal feed for specific menu type
- `GET /api/menu-types` - Available menu types
- `GET /api/menu-preview/{menuTypeId}` - Today/tomorrow menu preview
- `GET /admin/refresh-menus?secret=X` - Manual refresh endpoint

## Data Persistence

SQLite database with MenuEntry and MenuType models. Repository pattern with optimized queries. Background service refreshes every 6 hours. Unique constraints per date/menu type.

## Web Scraping Implementation

Scrapes meyers.dk tab structure. Auto-discovers menu types from `data-tab-content` attributes. Extracts dates from `week-menu-day__header-heading` and menu items from `menu-recipe-display`. Handles HTML entities. Auto-creates menu types.

## Background Processing

MenuCacheBackgroundService refreshes menu data every 6 hours without blocking API requests.

## Testing

Comprehensive test suite with TestWebApplicationFactory, MockHttpMessageHandler, and real HTML fixtures. Tests all 8 menu types, web interface, API endpoints, mobile responsiveness, and MapStaticAssets fingerprinting.

## Important Notes

- **CSS Assets**: Use direct path `css/app.css` for CSS (NOT @Assets - fails in production)
- **JS Assets**: Use `@Assets["js/menu-app.js"]` for JavaScript files
- **Regex**: Always use `GeneratedRegex` attribute
- **Database**: Repository pattern with optimized single queries
- **Testing**: Update all tests when adding features

## Build System

Tailwind CSS auto-compilation via MSBuild. .NET 9 MapStaticAssets for fingerprinting. CSS compiled before static asset processing. Docker includes Node.js 20.x.
