# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## First Steps — Every Session

1. Read `TODO.md` to understand pending/in-progress work
2. Read `SESSION_HANDOFF.md` to pick up where the last session left off

## Workflow Rules

- **Always plan before executing.** Use the writing-plans skill or brainstorming before implementation.
- **Track work in `TODO.md`.** Update task status as you progress.
- **Update `SESSION_HANDOFF.md`** at the end of every session (or before context gets large) with current state, next steps, and blockers.
- **Commit and push after every meaningful change.** Keep the remote up to date.
- **Keep `README.md` and `HOW_TO_IMPLEMENT.md` current.** These target repo users — update them when the project evolves.

## Project Overview

XafNavigatonHub is a **Navigation Hub / Launchpad** — a DevExpress XAF application that replaces the default sidebar navigation with a full-page colorful button grid. Users land on a dashboard-style home screen with styled buttons that open different functional areas (request pages, modules, etc.). Built on .NET 8 with DevExpress v25.2.3, EF Core, SQL Server (LocalDB), with Blazor Server and WinForms frontends.

## Solution Structure

- **XafNavigatonHub.Module** — Platform-agnostic module containing business objects, controllers, and database updater. Both frontends reference this.
- **XafNavigatonHub.Blazor.Server** — Blazor Server frontend (ASP.NET Core). Entry point: `Startup.cs` configures XAF, security, and EF Core.
- **XafNavigatonHub.Win** — WinForms frontend (net8.0-windows).

Solution file: `XafNavigatonHub.slnx` (XML-based solution format). Build configurations: Debug, Release, EasyTest.

## Build & Run

```bash
# Build entire solution
dotnet build XafNavigatonHub.slnx

# Run Blazor Server app
dotnet run --project XafNavigatonHub/XafNavigatonHub.Blazor.Server

# Run WinForms app
dotnet run --project XafNavigatonHub/XafNavigatonHub.Win
```

## Database

- EF Core with `XafNavigatonHubEFCoreDbContext` (SQL Server LocalDB)
- Connection string in `appsettings.json`: `(localdb)\mssqllocaldb`, catalog `XafNavigatonHub`
- XAF handles migrations automatically via `ModuleUpdater` (`DatabaseUpdate/Updater.cs`)
- Debug/EasyTest builds seed default "Admin" and "User" accounts with empty passwords

## Key Architecture Details

- **Security**: Integrated security mode with `ApplicationUser` (extends `PermissionPolicyUser`) and `PermissionPolicyRole`. Cookie authentication on Blazor, password-based auth.
- **DbContext configuration**: Uses deferred deletion, optimistic locking, `ChangingAndChangedNotificationsWithOriginalValues` change tracking, and `PreferFieldDuringConstruction` property access mode.
- **Blazor SignalR proxies**: `ProxyHubConnectionHandler` and `CircuitHandlerProxy` in `Services/` initialize XAF's `IValueManagerStorageContainer` per circuit — required for XAF Blazor to function.
- **XAF modules enabled**: ConditionalAppearance, Dashboards, FileAttachments, Notifications, Office, ReportsV2, Scheduler, Validation, ViewVariants, TreeListEditors, Chart.
- **Model customization**: `Model.DesignedDiffs.xafml` (embedded resource in Module), `Model.xafml` (content files in frontend projects).
