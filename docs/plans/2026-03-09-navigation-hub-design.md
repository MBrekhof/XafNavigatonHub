# NavigationHub Design

## Core Concept

A full-page home screen with styled card buttons organized in categorized sections. Replaces XAF's sidebar as the primary navigation. Runs in TabbedMDI mode where the hub is a permanent non-closable first tab.

## Layout Structure

```
┌─────────────────────────────────────────────────┐
│  ★ Preferred Actions (user-pinned, top row)     │
│  [Card] [Card] [Card] [Card]                    │
├─────────────────────────────────────────────────┤
│  ── HR ──────────────────────────────────────   │
│  [Card] [Card] [Card]                           │
│                                                 │
│  ── Finance ─────────────────────────────────   │
│  [Card] [Card] [Card] [Card] [Card]             │
│                                                 │
│  ── Requests ────────────────────────────────   │
│  [Card] [Card]                                  │
└─────────────────────────────────────────────────┘
```

- **Preferred Actions row**: pinned by the user, persisted per-user in the database. Drag & drop to pin (right-click "Pin to top" as fallback).
- **Category sections**: labeled headers, each containing cards for that functional area.
- **Cards**: subtle shadow, icon on top, label below, slight elevation. Colored accent (border or icon tint) per category.

## Role-Based Visibility

- Each button has one or more required roles (configured in Model Editor).
- User sees only buttons their role(s) grant access to.
- Multiple roles = union of all buttons.
- Empty categories are hidden automatically.
- Leverages XAF's existing `NavigationPermission` — if a user can't navigate to a view, the button doesn't show.

## View Strategy & Tab Behavior

- **TabbedMDI** on both Blazor and WinForms.
- Hub is the first tab, **non-closable**.
- Clicking a button opens the target view in a new tab (or activates existing tab).
- Standard XAF tab behavior for everything else.

## Button Configuration (Phase 1: Model Editor)

Each button is defined as a model node under a category:

| Property | Description |
|----------|-------------|
| `Caption` | Button label |
| `ImageName` | XAF image identifier (SVG or built-in) |
| `Category` | Group header it belongs to |
| `NavigationItemId` | Links to an existing XAF navigation item (ListView/DetailView) |
| `Color` | Accent color for the card |
| `SortOrder` | Ordering within category |

Code-based registration also available via a module controller for developer convenience.

## User Preferences (Pinned Favorites)

- Business object: `UserHubPreference` (UserId, NavigationItemId, SortOrder).
- Stored in the database, secured per-user.
- Drag & drop to pin on Blazor (HTML5 drag API); right-click context menu on both platforms.
- Unpin via right-click or drag out of the preferred row.

## Card Visual Style

Cards with subtle shadows — lighter feel, icon on top, label below, slight elevation. Colored accent per category.

## Deferred (Future Phases)

- **Phase 2**: Runtime admin UI for managing buttons (business objects in DB instead of Model Editor).
- External URL buttons.
- Custom Blazor/WinForms panel buttons.
- Large button count handling (collapsible sections if needed; scrolling within categories sufficient for now).

## Target Platforms

- ASP.NET Core Blazor Server
- WinForms

Both share the same Module-level model extensions and business objects. Platform-specific rendering is implemented in each frontend project.
