# Meyers Menu Calendar

[![Build and Deploy](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml)

A .NET 9 Blazor SSR application that scrapes Meyers restaurant menus and provides iCal feeds for 8 different menu types. Features clean architecture, multi-menu support, custom calendar builder, and automatic caching with 5-minute alarm notifications.

## Features

- 📅 **iCal Feeds**: Individual calendar subscriptions for each of 8 menu types
- 🔔 **Alarm Notifications**: 5-minute reminders before lunch events
- 🎛️ **Custom Calendar Builder**: Mix different menus per weekday (e.g., "Det velkendte" Mon-Wed-Fri, "Den grønne" Thu)
- 🚀 **Fast**: Blazor SSR with .NET 9 MapStaticAssets and clean architecture
- 🔄 **Auto-Refresh**: Background service updates every 6 hours
- 📱 **Universal**: Works with all calendar apps (Google Calendar, Outlook, Apple Calendar)
- 🎨 **Responsive**: Modern UI with Tailwind CSS v4 and 7-day calendar preview
- 🏗️ **Clean Architecture**: Separated Core, Infrastructure, and Web layers

## Quick Start

```bash
# Clone and run
git clone https://github.com/yourusername/meyers-menu-calendar.git
cd meyers-menu-calendar/Meyers.Web
npm install
cd ..
dotnet run --project Meyers.Web
# Available at http://localhost:5116
```

## Endpoints

- `GET /` - Web interface with multi-menu support and custom calendar builder
- `GET /calendar/{menu-type-slug}.ics` - iCal feed for specific menu type
- `GET /calendar/custom/{config}.ics` - Custom mixed calendar (e.g., M1T1W1R2F1)
- `GET /api/menu-types` - Available menu types
- `GET /api/menu-preview/{menuTypeId}` - Today/tomorrow menu preview
- `GET /admin/refresh-menus?secret=X` - Manual refresh endpoint

## Usage

1. Visit http://localhost:5116
2. Select menu type tab
3. Copy calendar URL
4. Add to your calendar app

## Architecture

### Clean Architecture Structure

- **Meyers.Core/**: Domain models (MenuEntry, MenuType, MenuDay) and interfaces
- **Meyers.Infrastructure/**: Data access, services, and external dependencies
  - `Data/`: Entity Framework context and migrations
  - `Repositories/`: Data access layer with optimized queries
  - `Services/`: Menu scraping, calendar generation, background processing
  - `Configuration/`: Options and settings
- **Meyers.Web/**: Presentation layer
  - `Components/`: Blazor pages and UI components
  - `Handlers/`: API endpoints for calendar feeds and previews
  - `wwwroot/`: Static assets (CSS, JavaScript)
- **Meyers.Test/**: Comprehensive test suite with 54 tests

## Development

```bash
# Setup and run
cd Meyers.Web && npm install && cd ..
dotnet run --project Meyers.Web

# Run tests (54 total)
dotnet test

# Database migrations
dotnet ef migrations add MigrationName --project Meyers.Web
dotnet ef database update --project Meyers.Web

# Build with Tailwind CSS
dotnet build  # Automatically runs npm run build
```

## Deployment

### Docker

```bash
docker build -t meyers-menu-calendar .
docker run -p 8080:8080 -v meyers-data:/app/data meyers-menu-calendar
```

### Dokploy

1. Connect GitHub repository, set port to 8080
2. Mount volume: `/app/data` for database persistence
3. Deploy

## License

Educational and personal use. Respect Meyers' website terms of service.
