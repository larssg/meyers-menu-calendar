# Meyers Menu Calendar

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
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Deployment

### Database Migrations

The application uses Entity Framework migrations to manage database schema changes. When you deploy, the app automatically applies any pending migrations at startup using `context.Database.Migrate()`.

#### First-time deployment to existing production database:

If you already have a production database with MenuEntries table but no migrations history, you need to manually mark the initial migration as applied to avoid trying to recreate the table:

1. **Before deploying the new version**, connect to your production database and run:
   ```sql
   CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
       MigrationId TEXT NOT NULL PRIMARY KEY, 
       ProductVersion TEXT NOT NULL
   );
   INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
   VALUES ('20250803010915_InitialCreate', '9.0.7');
   ```

2. **Deploy the new version** - it will automatically apply the unique index migration

#### New deployments:
The unique index on the Date field will be automatically created when you deploy to a fresh environment.

### Using Docker (Recommended)

```bash
# Build the Docker image
docker build -t meyers-menu-calendar .

# Run the container
docker run -p 8080:8080 meyers-menu-calendar
```

### Using Dokploy

The repository includes a `Dockerfile` for deployment since .NET 9 may not be available in nixpacks yet.

For Dokploy deployment:
1. Connect your GitHub repository
2. Set the port to 8080
3. Deploy (it will automatically use the Dockerfile)

The database will be automatically created with the unique Date index when the app starts for the first time.

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