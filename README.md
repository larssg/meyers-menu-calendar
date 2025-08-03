# Meyers Menu Calendar

[![Build and Deploy](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/larssg/meyers-menu-calendar/actions/workflows/build-and-deploy.yml)

A .NET 9 application that scrapes the Meyers lunch menu and provides both an iCal feed and a beautiful web interface for easy calendar integration.

## Features

- 🍽️ **Web Interface**: Beautiful homepage showing today's and tomorrow's menu with calendar feed URLs
- 📅 **iCal Feed**: Generates iCal/ICS format for calendar subscriptions
- 🚀 **Fast Performance**: Blazor Server-Side Rendering (SSR) with automatic caching
- 🔄 **Auto-Refresh**: Background service updates menu data every 6 hours
- 🧹 **Clean Titles**: Calendar events show just the main dish name
- 📆 **Historical Data**: Preserves past menus and includes future data
- 📱 **Universal**: Works with Google Calendar, Outlook, Apple Calendar, etc.
- 🎨 **Modern Design**: Responsive UI with Tailwind CSS v4 and glassmorphism effects

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Node.js 20+ (for Tailwind CSS compilation)
- SQLite (included with .NET)

### Running Locally

```bash
# Clone the repository
git clone https://github.com/yourusername/meyers-menu-calendar.git
cd meyers-menu-calendar

# Install npm dependencies for Tailwind CSS
cd Meyers.Web
npm install
cd ..

# Run the application
dotnet run --project Meyers.Web

# The application will be available at http://localhost:5116
```

### Endpoints

- `GET /` - Beautiful web interface with menu preview and calendar URLs
- `GET /calendar` - Returns the iCal feed with menu data
- `GET /calendar.ics` - Same as /calendar (for compatibility with calendar apps expecting .ics extension)

### Subscribe to Calendar

1. Visit the homepage at `http://localhost:5116` 
2. Copy either calendar URL from the web interface
3. Add to your calendar app:
   - **Google Calendar**: Settings → Add calendar → From URL
   - **Outlook**: Add calendar → Subscribe from web
   - **Apple Calendar**: File → New Calendar Subscription

## Architecture

```
Meyers.Web/
├── Components/
│   ├── MainLayout.razor          # Main layout with modern design
│   ├── Home.razor                # Homepage with menu preview
│   └── Routes.razor              # Blazor SSR routing
├── Services/
│   ├── MenuScrapingService.cs    # Scrapes meyers.dk
│   ├── CalendarService.cs        # Generates iCal format
│   └── MenuCacheBackgroundService.cs # Background refresh
├── Handlers/
│   └── CalendarEndpointHandler.cs # Clean calendar API logic
├── Data/
│   └── MenuDbContext.cs          # Entity Framework setup
├── Models/
│   └── MenuEntry.cs              # Database model
├── Styles/
│   └── app.css                   # Tailwind CSS v4 configuration
├── wwwroot/css/
│   └── app.css                   # Compiled CSS (auto-generated)
├── package.json                  # Node.js dependencies for Tailwind
└── Program.cs                    # Blazor SSR + API setup
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
# Run all tests (includes Tailwind CSS compilation)
dotnet test

# Run only unit tests (recommended for development)
dotnet test --filter "FullyQualifiedName!~CalendarApiTests"

# Run only integration tests (tests web interface + API)
dotnet test --filter "FullyQualifiedName~CalendarApiTests"
```

### Building for Production

```bash
# Building automatically compiles Tailwind CSS
dotnet publish -c Release -o ./publish

# Or build CSS separately if needed
npm run build --prefix Meyers.Web
```

### CI/CD

The project uses GitHub Actions for continuous integration and deployment:

- **Build and Deploy Pipeline** (`.github/workflows/build-and-deploy.yml`):
  - **Triggers**: Push to main/develop and pull requests to main
  - **Parallel execution**: All checks run simultaneously for faster feedback
  - **Jobs**:
    - `test`: Runs unit and integration tests
    - `check-migrations`: Validates EF migrations
    - `security-scan`: Scans for vulnerabilities and Dockerfile best practices
    - `build-docker`: Builds and tests Docker image
    - `deploy`: Triggers Dokploy deployment (only on main branch after all jobs succeed)

- **Dependencies** (`.github/workflows/dependencies.yml`):
  - Weekly check for outdated packages
  - Security vulnerability scanning
  - Automated reporting

### Automatic Deployment

When code is pushed to the `main` branch:
1. All jobs (test, migrations, security, docker) run in parallel
2. If ALL jobs succeed, deployment to Dokploy is triggered automatically
3. Deployment uses the Dokploy API with secure authentication

This parallel approach provides faster feedback while ensuring production only receives code that has passed all tests and security checks.

#### Required GitHub Secrets

To enable automatic deployment, add these secrets to your GitHub repository (**Settings** → **Secrets and variables** → **Actions**):

- `DOKPLOY_API_KEY`: Your Dokploy API key for authentication
- `DOKPLOY_APPLICATION_ID`: Your Dokploy application ID

## Deployment

### Database Migrations

The application uses Entity Framework migrations to manage database schema changes. When you deploy, the app automatically applies any pending migrations at startup using `context.Database.Migrate()`.

### Using Docker (Recommended)

```bash
# Build the Docker image (includes Node.js for Tailwind CSS compilation)
docker build -t meyers-menu-calendar .

# Run the container with persistent database storage
docker run -p 8080:8080 -v meyers-data:/app/data meyers-menu-calendar

# Or run without persistence (for testing)
docker run -p 8080:8080 meyers-menu-calendar
```

The Docker build automatically:
- Installs Node.js 20.x for Tailwind CSS compilation
- Runs `npm install` to get Tailwind dependencies
- Compiles CSS during the .NET build process
- Includes the compiled CSS in the final image

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
