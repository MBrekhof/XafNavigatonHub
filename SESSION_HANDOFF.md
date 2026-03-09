# Session Handoff

## Last Session Summary

Implemented the NavigationHub Blazor frontend (Phase 1). All 9 tasks from the implementation plan completed.

## Current State

- **NavigationHub Blazor UI fully implemented** — card-based home screen with categorized buttons
- Model extensions: `IModelNavigationHub`, `IModelHubCategory`, `IModelHubButton` in Module project
- `UserHubPreference` business object for persisting per-user pinned favorites
- `NavigationHubController` (Module): role-filtered hub data, programmatic navigation, pin CRUD
- `NavigationHubComponent.razor` (Blazor): self-contained Razor component using `CascadingParameter` from `BlazorControlViewItem`
- `HubTabController` (Blazor): prevents closing the hub tab in TabbedMDI
- Hub registered as `DashboardView` with `ControlDetailItem` in Blazor `Model.xafml`
- Sample config in `Model.DesignedDiffs.xafml` (Administration category with Users/Roles buttons)
- TabbedMDI enabled on both platforms, hub is startup navigation item
- Design doc: `docs/plans/2026-03-09-navigation-hub-design.md`
- Implementation plan: `docs/plans/2026-03-09-navigation-hub-implementation.md`

## Key Architecture Decisions

- Used `ControlDetailItem` + `BlazorControlViewItem` (standard XAF pattern) instead of custom `ViewItem` for DashboardView hosting
- Razor component is self-contained — gets controller via `CascadingParameter` → `Application.MainWindow`
- Hub tab non-closable via tracking first `ActiveTemplate` from `ChildTemplatesChanged` event
- Role filtering uses `ShowNavigationItemController.ShowNavigationItemAction.Items` (XAF's built-in permission system)

## Next Steps

- **WinForms Hub ViewItem**: Create equivalent UserControl with painted cards for Win platform
- **WinForms Hub Tab Non-Closable**: Use `DocumentManager` to prevent closing hub document
- **Drag & Drop Pinning**: HTML5 drag API on Blazor for reordering pinned items
- **Runtime Admin UI (Phase 2)**: Business objects for hub config, admin CRUD screen
- **External URL Support**: Add URL property to model, open in browser on click
- **Test the Blazor app end-to-end**: Verify hub displays, navigation works, pin/unpin works

## Open Questions / Blockers

- Image path `_content/DevExpress.ExpressApp.Blazor/images/{name}.svg` may need adjustment — verify with actual DevExpress image names
- CSS in Razor component is global (not scoped) — consider moving to `.razor.css` isolation file
