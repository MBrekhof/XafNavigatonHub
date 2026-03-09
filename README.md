# XafNavigatonHub

A DevExpress XAF application with Blazor Server and WinForms frontends, built on .NET 8 with EF Core and SQL Server.

## Prerequisites

- .NET 8 SDK
- DevExpress v25.2.3 NuGet feed configured
- SQL Server LocalDB (included with Visual Studio)

## Getting Started

1. Clone the repository
2. Ensure your DevExpress NuGet feed is configured
3. Build the solution:
   ```bash
   dotnet build XafNavigatonHub.slnx
   ```
4. Run the Blazor Server app:
   ```bash
   dotnet run --project XafNavigatonHub/XafNavigatonHub.Blazor.Server
   ```
   Or the WinForms app:
   ```bash
   dotnet run --project XafNavigatonHub/XafNavigatonHub.Win
   ```
5. Log in with **Admin** (empty password) in Debug mode

## Project Structure

| Project | Description |
|---------|-------------|
| `XafNavigatonHub.Module` | Shared module: business objects, controllers, database updater |
| `XafNavigatonHub.Blazor.Server` | Blazor Server frontend |
| `XafNavigatonHub.Win` | WinForms frontend |

## Database

The app uses SQL Server LocalDB. XAF creates and updates the database automatically on first run. Default connection string points to `(localdb)\mssqllocaldb` with catalog `XafNavigatonHub`.

See [HOW_TO_IMPLEMENT.md](HOW_TO_IMPLEMENT.md) for implementation guidance.
