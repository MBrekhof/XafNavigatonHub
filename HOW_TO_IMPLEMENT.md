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

```csharp
using DevExpress.ExpressApp;

namespace XafNavigatonHub.Module.Controllers;

public class MyController : ViewController
{
    public MyController()
    {
        TargetObjectType = typeof(MyEntity);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        // Add logic here
    }
}
```

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
