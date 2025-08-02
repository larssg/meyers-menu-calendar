# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET minimal API application that scrapes menu content from the Meyers restaurant website and provides it as an iCal calendar feed. The application specifically extracts the "Det velkendte" menu section.

## Architecture

- **Meyers.Web**: ASP.NET Core minimal API project containing:
  - `Program.cs`: Main application entry point with endpoints
  - `Services/MenuScrapingService.cs`: Web scraping logic using HtmlAgilityPack
  - `Services/CalendarService.cs`: iCal generation using Ical.Net
- **Meyers.Test**: xUnit test project with comprehensive test coverage

## Key Dependencies

- HtmlAgilityPack: HTML parsing and web scraping
- Ical.Net: iCal calendar format generation
- Microsoft.AspNetCore.Mvc.Testing: Integration testing

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
```

## API Endpoints

- `GET /` - Returns API description
- `GET /calendar` - Returns iCal calendar feed with Meyers "Det velkendte" menu

## Web Scraping Implementation

The scraper targets the Meyers website's tab-based structure:
- Extracts actual dates from `week-menu-day__header-heading` elements (e.g., "mandag 28 jul, 2025")
- Maps multiple `data-tab-content="Det velkendte"` elements to correct dates
- Extracts menu items from `menu-recipe-display` CSS classes
- Returns structured menu data with correct dates (not calculated dates)
- Handles HTML entity encoding (e.g., `&#248;` for ø)

## Testing

The test suite includes:
- Integration tests using real downloaded HTML fixtures
- Mock HttpClient for isolated unit testing  
- Test data stored in `TestData/meyers-menu-page.html`
- Comprehensive validation of all 5 weekdays
- Verification that each day has different menu content
- Specific tests for day-specific features (e.g., "Torsdagssødt" on Thursday)

Tests verify complete weekly menu extraction and correct calendar event generation for proper dates.