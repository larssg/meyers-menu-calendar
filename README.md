# Meyers Menu Calendar

[![CI](https://github.com/yourusername/meyers-menu-calendar/actions/workflows/ci.yml/badge.svg)](https://github.com/yourusername/meyers-menu-calendar/actions/workflows/ci.yml)
[![Deployment Check](https://github.com/yourusername/meyers-menu-calendar/actions/workflows/deploy-check.yml/badge.svg)](https://github.com/yourusername/meyers-menu-calendar/actions/workflows/deploy-check.yml)

A .NET 9 minimal API that scrapes the Meyers lunch menu and generates an iCal feed for easy calendar integration.

## Features

- 🍽️ Scrapes daily lunch menus from meyers.dk
- 📅 Generates iCal/ICS format for calendar subscriptions
- 🚀 Automatic caching with SQLite for performance
- 🔄 Background service refreshes menu data every 6 hours
- 🧹 Clean calendar titles showing just the main dish
- 📆 Includes historical data (last month) plus future menus
- 📱 Works with Google Calendar, Outlook, Apple Calendar, etc.

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- SQLite (included with .NET)

### Running Locally

```bash
# Clone the repository
git clone https://github.com/yourusername/meyers-menu-calendar.git
cd meyers-menu-calendar

# Run the application
dotnet run --project Meyers.Web

# The API will be available at http://localhost:5116
```

### Endpoints

- `GET /` - Returns API description
- `GET /calendar` - Returns the iCal feed with menu data
- `GET /calendar.ics` - Same as /calendar (for compatibility with calendar apps expecting .ics extension)

### Subscribe to Calendar

1. Copy the calendar URL: `http://localhost:5116/calendar` or `http://localhost:5116/calendar.ics`
2. Add to your calendar app:
   - **Google Calendar**: Settings → Add calendar → From URL
   - **Outlook**: Add calendar → Subscribe from web
   - **Apple Calendar**: File → New Calendar Subscription

## Architecture

```
Meyers.Web/
├── Services/
│   ├── MenuScrapingService.cs    # Scrapes meyers.dk
│   ├── CalendarService.cs        # Generates iCal format
│   └── MenuCacheBackgroundService.cs # Background refresh
├── Data/
│   └── MenuDbContext.cs          # Entity Framework setup
├── Models/
│   └── MenuEntry.cs              # Database model
└── Program.cs                    # Minimal API setup
```

## Configuration

The application uses these default settings:

```json
{
  "MenuCache": {
    "CheckInterval": "00:01:00",      // Check every minute
    "RefreshInterval": "06:00:00",    // Refresh every 6 hours
    "TimeoutSeconds": 30
  }
}
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests (recommended for development)
dotnet test --filter "FullyQualifiedName!~CalendarApiTests"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~CalendarApiTests"
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

### CI/CD

The project uses GitHub Actions for continuous integration and deployment:

- **CI Pipeline** (`.github/workflows/ci.yml`):
  - Runs on push to main/develop and pull requests
  - Builds and tests the application
  - Builds and tests Docker image
  - Unit tests must pass, integration tests allowed to fail due to migration conflicts

- **Deployment Check** (`.github/workflows/deploy-check.yml`):
  - Automatically runs after CI workflow completes successfully on main branch
  - Validates migrations and deployment scripts
  - Security scanning
  - Dockerfile best practices check
  - **Automatic deployment**: Triggers Dokploy deployment after all checks pass

- **Dependencies** (`.github/workflows/dependencies.yml`):
  - Weekly check for outdated packages
  - Security vulnerability scanning
  - Automated reporting

### Automatic Deployment

When code is pushed to the `main` branch:
1. The CI pipeline runs all tests and builds
2. If CI succeeds, the Deployment Check workflow automatically runs
3. Deployment checks validate migrations, security, and Dockerfile
4. If all checks pass, deployment to Dokploy is triggered via webhook

This ensures that production only receives code that has passed all tests and security checks.

## Deployment

### Database Migrations

The application uses Entity Framework migrations to manage database schema changes. When you deploy, the app automatically applies any pending migrations at startup using `context.Database.Migrate()`.

### Using Docker (Recommended)

```bash
# Build the Docker image
docker build -t meyers-menu-calendar .

# Run the container with persistent database storage
docker run -p 8080:8080 -v meyers-data:/app/data meyers-menu-calendar

# Or run without persistence (for testing)
docker run -p 8080:8080 meyers-menu-calendar
```

### Using Dokploy

The repository includes a `Dockerfile` for deployment since .NET 9 may not be available in nixpacks yet.

#### Dokploy deployment steps:

1. **Create the application:**
   - Connect your GitHub repository
   - Set the port to 8080
   - Deploy type: Docker

2. **Configure persistent storage for the database:**
   - Go to your application → **Mounts** tab
   - Add a new mount:
     - **Type**: Volume
     - **Name**: `meyers-db-data`
     - **Mount Path**: `/app/data`
     - **Host Path**: Leave empty (Dokploy manages it)

3. **Set environment variables (optional):**
   - Go to **Environment** tab
   - Add: `DATABASE_PATH=Data Source=/app/data/menus.db` (already set in Dockerfile)

4. **Deploy:**
   - Click Deploy
   - The database will be automatically created with the unique Date index

#### Database persistence:
- The SQLite database is stored in `/app/data/menus.db` inside the container
- This directory is mounted to a persistent volume, so data survives deployments
- The database will be automatically migrated on each deployment


## How It Works

1. **Scraping**: The service fetches the weekly menu from meyers.dk
2. **Parsing**: Extracts menu items, focusing on the main warm dish
3. **Caching**: Stores in SQLite to reduce load on Meyers' servers
4. **Calendar Generation**: Creates iCal events for each weekday lunch
5. **Smart Titles**: Removes boilerplate text like "Varm ret med tilbehør" to show just the dish name

## Calendar Event Format

Each lunch appears as a calendar event:
- **Title**: Main dish (e.g., "Oksekødboller i krydret tomatsauce")
- **Time**: 12:00-13:00
- **Description**: Full menu details including sides and salads

## Contributing

Pull requests are welcome! Please feel free to submit issues or enhancement requests.

## License

This project is provided as-is for educational and personal use. Please respect Meyers' website terms of service when using this tool.

## Disclaimer

This is an unofficial tool and is not affiliated with or endorsed by Meyers Kantiner.
