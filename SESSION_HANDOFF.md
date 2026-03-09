# Session Handoff

## Current State

All Phase 1 features are complete and working on both Blazor and WinForms.

### What's Implemented

- **Navigation Hub** — card-based dashboard replacing sidebar navigation, registered as DashboardView startup item
- **Model extensions** — `IModelNavigationHub`, `IModelHubCategory`, `IModelHubButton` with `ExternalUrl` support
- **NavigationHubController** (Module) — role-filtered hub data via `ShowNavigationItemAction`, programmatic navigation, per-user pin CRUD via `UserHubPreference`
- **Blazor frontend** — `NavigationHubComponent.razor` with drag & drop pinning, external URLs via JSInterop, dark theme via `--dxds-*` CSS variables with `--bs-*` fallbacks
- **WinForms frontend** — `NavigationHubControl` (owner-draw `XtraUserControl` with GDI+ painting), SVG icon rendering via `SvgPaletteHelper`, dark theme via `CommonSkins.GetSkin().SvgPalettes` palette colors ("Paint", "Paint High", "Brush", "Paint Shadow")
- **Non-closable hub tab** — `HubTabController` (Blazor) and `HubTabWinController` (Win)
- **Demo data** — 7 business objects (Employee, Department, Customer, Product, SalesOrder, ProjectTask, AuditLogEntry) with seed data, HR/Sales roles, HrManager/SalesRep users
- **Permission filtering** — buttons hidden based on `ChoiceActionItem.Enabled && Active` state, external URL buttons bypass permission check

### Key Architecture Decisions

- `ControlDetailItem` + platform-specific control (standard XAF pattern) for DashboardView hosting
- Blazor component gets controller via `CascadingParameter` → `BlazorControlViewItem` → `Application.MainWindow`
- WinForms control initialized via `NavigationHubWinController` (ViewController<DashboardView>)
- Icon resolution: `ImageLoader.Instance` → `ImageInfo.ImageBytes` → base64 data URI (Blazor) or `SvgImage.Render()` with skin palette (WinForms)
- WinForms skin colors: `CommonSkins.GetSkin()` → `SvgPalettes[DefaultSkinPaletteName]` → named colors ("Paint", "Paint High", etc.)
- `DXSkinColors.FillColors` only has semantic colors (Primary, Success, Warning, Danger, Question) — NOT suitable for general background/text colors

## Next Steps

- **Phase 2**: Runtime admin UI for hub configuration (business objects instead of Model Editor)
- Optional: Blazor drag & drop for WinForms (WinForms currently supports right-click pin/unpin only)
- Optional: CSS isolation for Blazor component (`.razor.css`)
