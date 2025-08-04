# Meyers Menu Calendar

[![Build and Deploy](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml)

A .NET 9 Blazor app that scrapes Meyers lunch menus and provides iCal feeds for calendar integration.

## Features

- 📅 **iCal Feeds**: Individual calendar subscriptions for each type
- 🚀 **Fast**: Blazor SSR with .NET 9 MapStaticAssets
- 🔄 **Auto-Refresh**: Updates every 6 hours
- 📱 **Universal**: Works with all calendar apps
- 🎨 **Responsive**: Modern UI with Tailwind CSS v4

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

- `GET /` - Web interface with all menu types
- `GET /calendar/{menu-type}.ics` - iCal feed for specific menu
- `GET /api/menu-types` - Available menu types
- `GET /api/menu-preview/{id}` - Today/tomorrow preview

## Usage

1. Visit http://localhost:5116
2. Select menu type tab
3. Copy calendar URL
4. Add to your calendar app

## Architecture

- **Components/**: Blazor pages and UI components
- **Services/**: Menu scraping, calendar generation, background refresh
- **Handlers/**: API endpoints for calendar feeds and previews
- **Models/**: MenuEntry and MenuType database models
- **wwwroot/**: Static assets (CSS, JavaScript)

## Development

```bash
# Run tests
dotnet test

# Database migrations
dotnet ef migrations add Name --project Meyers.Web
dotnet ef database update --project Meyers.Web
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
