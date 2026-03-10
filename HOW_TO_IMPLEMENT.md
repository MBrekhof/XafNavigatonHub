# How to Implement

Guide for contributors and users working with this XAF application.

## Adding a Business Object

1. Create a new class in `XafNavigatonHub.Module/BusinessObjects/`
2. Inherit from `DevExpress.Persistent.BaseImpl.EF.BaseObject` (or another XAF base class)
3. Add a `DbSet<YourEntity>` to `XafNavigatonHubEFCoreDbContext`
4. XAF auto-generates CRUD views — no manual UI code needed

```csharp
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace XafNavigatonHub.Module.BusinessObjects;

[DefaultClassOptions] // Makes it visible in navigation
public class MyEntity : BaseObject
{
    public virtual string Name { get; set; }
}
```

## Adding a Controller

Place platform-agnostic controllers in `XafNavigatonHub.Module/Controllers/`. For platform-specific controllers, use the respective frontend project's `Controllers/` folder.

### Controller Patterns Used in This Project

**WindowController (main window scope)** — Used when you need access to the main window's frame, e.g. to read navigation items or intercept tab closing. Set `TargetWindowType = WindowType.Main` in the constructor.

```csharp
public class MyMainWindowController : WindowController
{
    public MyMainWindowController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        // Access Frame.GetController<T>() to interact with other controllers
        // Access Application.Model to read model extensions
    }
}
```

**ViewController\<DashboardView\> (view-specific)** — Used when you need to interact with controls inside a DashboardView, such as finding the hub control in a `ControlDetailItem`.

```csharp
public class MyDashboardController : ViewController<DashboardView>
{
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        foreach (var item in View.GetItems<ControlViewItem>())
        {
            if (item.Control is MyCustomControl control)
            {
                // Initialize the control, e.g. pass it data from a WindowController
                var controller = Application.MainWindow?.GetController<NavigationHubController>();
                if (controller != null)
                    control.Initialize(controller);
            }
        }
    }
}
```

### Existing Controllers

| Controller | Type | Project | Purpose |
|---|---|---|---|
| `NavigationHubController` | `WindowController` | Module | Core hub logic: reads model config, resolves icons, handles navigation, manages pinned favorites |
| `HubTabController` | `WindowController` | Blazor.Server | Prevents the hub tab from being closed in Blazor TabbedMDI |
| `HubTabWinController` | `WindowController` | Win | Prevents the hub tab from being closed in WinForms TabbedMDI |
| `NavigationHubWinController` | `ViewController<DashboardView>` | Win | Finds the `NavigationHubControl` in the DashboardView and passes it the `NavigationHubController` |

> **Note:** Blazor doesn't need an equivalent of `NavigationHubWinController` — the Razor component gets its controller via `[CascadingParameter] BlazorControlViewItem ViewItem` → `Application.MainWindow.GetController<NavigationHubController>()`.

## Database Seeding

Add seed data in `XafNavigatonHub.Module/DatabaseUpdate/Updater.cs` inside `UpdateDatabaseAfterUpdateSchema()`.

## Security & Permissions

- Roles and permissions are managed via `PermissionPolicyRole`
- Default roles (Admin, Default, HR, Sales) are created in the `Updater`
- To restrict access to a new business object, add type/object/member permissions to the relevant role

## NavigationHub: Adding Buttons

Hub buttons are configured via the XAF Application Model. Each button belongs to a category and links to an XAF navigation item or an external URL.

### Via Model Editor

1. Open the Model Editor in Visual Studio
2. Navigate to the **NavigationHub** node
3. Add a category (e.g., "HR") with `Id`, `Caption`, and `SortOrder`
4. Under the category's **Buttons** node, add buttons with:
   - `Id` — unique identifier
   - `Caption` — display label
   - `ImageName` — XAF image name (e.g., `BO_Employee`)
   - `NavigationItemId` — the `GetIdPath()` of the target navigation item (e.g., `Default/Employee_ListView`)
   - `Color` — hex accent color (e.g., `#4CAF50`)
   - `SortOrder` — ordering within category
   - `ExternalUrl` — (optional) URL to open in a new browser tab instead of navigating within XAF

### Via Model.DesignedDiffs.xafml

```xml
<NavigationHub>
  <Item Id="HR" Caption="HR" SortOrder="1" IsNewNode="True">
    <Buttons>
      <Item Id="Employees" Caption="Employees" ImageName="BO_Employee"
        NavigationItemId="Employee_ListView" Color="#4CAF50" SortOrder="0"
        IsNewNode="True" />
      <Item Id="Docs" Caption="Documentation" ImageName="Action_Export"
        ExternalUrl="https://docs.example.com" Color="#7B1FA2" SortOrder="1"
        IsNewNode="True" />
    </Buttons>
  </Item>
</NavigationHub>
```

### Role-Based Visibility

Buttons are automatically hidden if the current user doesn't have navigation permission for the linked view. No extra configuration needed — XAF's built-in `NavigationPermission` handles it. External URL buttons are always visible regardless of permissions.

### User Pinned Favorites

Users can pin/unpin cards to the "Preferred Actions" row:

- **Right-click** a card → Pin to Favorites / Unpin
- **Drag & drop** (Blazor only) — drag a card into the pinned area, reorder pinned items, or drag out to unpin

Pins are stored per-user in the `UserHubPreference` table and persist across sessions.

## Model Customization

- Use the XAF Model Editor (Visual Studio designer) to customize views
- Platform-agnostic changes: `XafNavigatonHub.Module/Model.DesignedDiffs.xafml`
- Platform-specific changes: `Model.xafml` in each frontend project
