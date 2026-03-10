# How to Implement the Navigation Hub in Your XAF App

Step-by-step guide for adding the NavigationHub to an existing DevExpress XAF application. This guide assumes you have a working XAF app with EF Core and the Security module.

## Overview

The hub is a full-page card-based dashboard that replaces sidebar navigation. It runs inside a `DashboardView` with a `ControlDetailItem` that hosts a platform-specific control (Razor component for Blazor, `XtraUserControl` for WinForms). Hub buttons are configured in the XAF Application Model — no code changes needed to add or rearrange buttons.

**What you'll add:**

| Component | File | Project |
|---|---|---|
| Model interfaces | `Model/IModelNavigationHub.cs` | Module |
| Hub controller | `Controllers/NavigationHubController.cs` | Module |
| Favorites entity | `BusinessObjects/UserHubPreference.cs` | Module |
| Model registration | `ExtendModelInterfaces` in your `ModuleBase` | Module |
| DbSet | `DbSet<UserHubPreference>` in your `DbContext` | Module |
| Blazor hub UI | `Editors/NavigationHubComponent.razor` | Blazor.Server |
| Blazor tab controller | `Controllers/HubTabController.cs` | Blazor.Server |
| WinForms hub UI | `Editors/NavigationHubControl.cs` | Win |
| WinForms init controller | `Editors/NavigationHubWinController.cs` | Win |
| WinForms tab controller | `Controllers/HubTabWinController.cs` | Win |
| DashboardView wiring | `Model.xafml` | Each frontend |

## Step 1: Model Interfaces (Module)

Create `Model/IModelNavigationHub.cs` in your shared module. These interfaces define the `NavigationHub` node in the XAF Application Model, where buttons and categories are configured.

```csharp
using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace YourApp.Module.Model;

public interface IModelNavigationHubExtension : IModelNode
{
    IModelNavigationHub NavigationHub { get; }
}

public interface IModelNavigationHub : IModelNode, IModelList<IModelHubCategory> { }

[KeyProperty(nameof(Id))]
public interface IModelHubCategory : IModelNode
{
    string Id { get; set; }
    [Localizable(true)]
    string Caption { get; set; }
    int SortOrder { get; set; }
    IModelHubButtons Buttons { get; }
}

public interface IModelHubButtons : IModelNode, IModelList<IModelHubButton> { }

[KeyProperty(nameof(Id))]
public interface IModelHubButton : IModelNode
{
    string Id { get; set; }
    [Localizable(true)]
    string Caption { get; set; }
    string ImageName { get; set; }
    string NavigationItemId { get; set; }
    string Color { get; set; }
    int SortOrder { get; set; }
    string ExternalUrl { get; set; }
}
```

## Step 2: Register the Model Extension (Module)

In your `ModuleBase` class, register the extension so XAF exposes the `NavigationHub` node in the Model Editor:

```csharp
public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders)
{
    base.ExtendModelInterfaces(extenders);
    extenders.Add<IModelApplication, IModelNavigationHubExtension>();
}
```

Without this, the `NavigationHub` node won't exist in the model and button configuration won't work.

## Step 3: UserHubPreference Entity (Module)

This entity stores per-user pinned favorites. Create `BusinessObjects/UserHubPreference.cs`:

```csharp
using DevExpress.Persistent.BaseImpl.EF;

namespace YourApp.Module.BusinessObjects;

public class UserHubPreference : BaseObject
{
    public virtual Guid UserId { get; set; }
    public virtual string NavigationItemId { get; set; }
    public virtual int SortOrder { get; set; }
}
```

Add a `DbSet` to your EF Core context:

```csharp
public DbSet<UserHubPreference> UserHubPreferences { get; set; }
```

## Step 4: NavigationHubController (Module)

Copy `Controllers/NavigationHubController.cs` into your shared module. This is the core controller that:

- Reads hub categories/buttons from the Application Model
- Resolves icon images to URLs (base64 data URIs for SVG/PNG)
- Filters buttons by the current user's navigation permissions
- Handles navigation when a button is clicked
- Manages pinned favorites (read/write `UserHubPreference`)

It's a `WindowController` targeting `WindowType.Main`, so it activates once on the main window.

Both the Blazor component and WinForms control call this controller — they don't access the model or database directly.

## Step 5: Security Permissions for UserHubPreference

Non-admin users need permissions to manage their own pins. In your role setup (typically in `Updater.cs`), add to any role that should support favorites:

```csharp
// Allow users to read, write, and delete their own hub preferences
role.AddObjectPermissionFromLambda<UserHubPreference>(
    SecurityOperations.ReadWriteAccess + ";" + SecurityOperations.Delete,
    p => p.UserId == (Guid)CurrentUserIdOperator.CurrentUserId(),
    SecurityPermissionState.Allow);

// Allow creating new preferences
role.AddTypePermissionsRecursively<UserHubPreference>(
    SecurityOperations.Create, SecurityPermissionState.Allow);
```

The `Delete` permission is required — when a user reorders or unpins favorites, the controller deletes and recreates all preference records.

## Step 6: Platform-Specific Hub Control

### Blazor Server

Copy `Editors/NavigationHubComponent.razor` into your Blazor project. The component:

- Gets the `NavigationHubController` via `[CascadingParameter] BlazorControlViewItem ViewItem` → `ViewItem.Application.MainWindow.GetController<NavigationHubController>()`
- Renders cards as HTML with CSS styling
- Supports drag & drop for pinning/reordering
- Right-click to pin/unpin
- Opens external URLs via `IJSRuntime`

No additional controller is needed to initialize the Blazor component — the `CascadingParameter` wiring is automatic via XAF's `ControlDetailItem`.

### WinForms

Copy two files:
- `Editors/NavigationHubControl.cs` — an `XtraUserControl` with GDI+ owner-draw painting
- `Editors/NavigationHubWinController.cs` — a `ViewController<DashboardView>` that finds the control and passes it the `NavigationHubController`

The WinForms control needs explicit initialization because it can't use cascading parameters. The `NavigationHubWinController` handles this:

```csharp
public class NavigationHubWinController : ViewController<DashboardView>
{
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        foreach (var item in View.GetItems<ControlViewItem>())
        {
            if (item.Control is NavigationHubControl hubControl)
            {
                var controller = Application.MainWindow?.GetController<NavigationHubController>();
                if (controller != null)
                    hubControl.Initialize(controller);
            }
        }
    }
}
```

## Step 7: Prevent Hub Tab from Closing

In TabbedMDI mode, users can close any tab — including the hub. To prevent this, add a tab controller per platform:

- **Blazor**: Copy `Controllers/HubTabController.cs` — intercepts `ITabbedMdiMainFormTemplateClosing.Closing` and cancels if the closing tab is the hub
- **WinForms**: Copy `Controllers/HubTabWinController.cs` — intercepts `TabbedView.DocumentClosing` and cancels if the document contains the hub view

Both are `WindowController` targeting `WindowType.Main`. If you don't use TabbedMDI, you can skip these.

## Step 8: Wire Up the DashboardView (Model.xafml)

This is the critical wiring that makes everything appear. Add to each frontend's `Model.xafml`:

### Blazor Server — `Model.xafml`

```xml
<Application>
  <Options UIType="TabbedMDI" FormStyle="Ribbon">
  </Options>
  <NavigationItems StartupNavigationItem="NavigationHub_NavItem">
    <Items>
      <Item Id="NavigationHub_NavItem" Caption="Home"
        ViewId="NavigationHub_DashboardView" IsNewNode="True" Index="0"
        ImageName="Home" />
    </Items>
  </NavigationItems>
  <Views>
    <DashboardView Id="NavigationHub_DashboardView" Caption="Main" IsNewNode="True">
      <Items>
        <ControlDetailItem Id="NavigationHubControl" Caption=" "
          ControlTypeName="YourApp.Blazor.Server.Editors.NavigationHubComponent"
          IsNewNode="True" />
      </Items>
      <Layout>
        <LayoutGroup Id="Main" ShowCaption="False" IsNewNode="True">
          <LayoutItem Id="NavigationHubControl" ViewItem="NavigationHubControl"
            ShowCaption="False" IsNewNode="True" />
        </LayoutGroup>
      </Layout>
    </DashboardView>
  </Views>
</Application>
```

### WinForms — `Model.xafml`

Same structure, but change `ControlTypeName` to your WinForms control:

```xml
<ControlDetailItem Id="NavigationHubControl" Caption=" "
    ControlTypeName="YourApp.Win.Editors.NavigationHubControl"
    IsNewNode="True" />
```

**Key points:**
- `ControlTypeName` must be the full namespace + class name of your platform control
- `StartupNavigationItem` makes the hub the first thing users see after login
- `Caption=" "` (single space) hides the control's label in the layout
- Each platform needs its own `Model.xafml` with the correct `ControlTypeName`

## Step 9: Configure Hub Buttons

Buttons are defined in the shared module's `Model.DesignedDiffs.xafml` under the `NavigationHub` node:

```xml
<NavigationHub>
  <Item Id="HR" Caption="Human Resources" SortOrder="0" IsNewNode="True">
    <Buttons>
      <Item Id="Employees" Caption="Employees" ImageName="BO_Employee"
        NavigationItemId="Employee_ListView" Color="#4CAF50" SortOrder="0"
        IsNewNode="True" />
    </Buttons>
  </Item>
  <Item Id="Admin" Caption="Administration" SortOrder="1" IsNewNode="True">
    <Buttons>
      <Item Id="Docs" Caption="Documentation" ImageName="Action_AboutInfo"
        ExternalUrl="https://docs.example.com" Color="#7B1FA2" SortOrder="0"
        IsNewNode="True" />
    </Buttons>
  </Item>
</NavigationHub>
```

You can also use the Visual Studio Model Editor: navigate to the **NavigationHub** node and add categories/buttons interactively.

### Button Properties

| Property | Required | Description |
|---|---|---|
| `Id` | Yes | Unique identifier |
| `Caption` | Yes | Display label on the card |
| `ImageName` | No | XAF image name (e.g., `BO_Employee`, `Action_Export`). Resolved via `ImageLoader` |
| `NavigationItemId` | Yes* | The XAF navigation item to open. See "Finding NavigationItemId" below |
| `Color` | No | Hex accent color for the card's left border (e.g., `#4CAF50`). Defaults to `#1976D2` |
| `SortOrder` | No | Order within the category (lower = first) |
| `ExternalUrl` | No | If set, opens this URL in a new tab instead of navigating within XAF |

*Required unless `ExternalUrl` is set.

### Finding the Right NavigationItemId

The controller accepts either format:
- **ViewId** — e.g., `Employee_ListView` (simpler, usually sufficient)
- **Full path** — e.g., `Default/Employee_ListView` (the `GetIdPath()` of the navigation item)

To find the correct value:
1. Open the **Model Editor** in Visual Studio
2. Navigate to **NavigationItems → Items → Default → Items**
3. The `Id` of each item is the `ViewId` you need (e.g., `Employee_ListView`)

### Role-Based Visibility

Buttons are automatically hidden if the current user doesn't have navigation permission for the linked view. The controller checks `ShowNavigationItemAction.Items` and only includes buttons where the matching item is both `Enabled` and `Active`. No extra configuration needed.

External URL buttons (`ExternalUrl` set) bypass permission checks and are always visible.

## Dark Theme Support

Both platforms automatically adapt to the active theme:

- **Blazor**: CSS uses `var(--dxds-color-*)` (DX Design System) with `var(--bs-*)` (Bootstrap) fallbacks
- **WinForms**: Reads skin colors from `CommonSkins.GetSkin().SvgPalettes[Skin.DefaultSkinPaletteName]` using named palette colors ("Paint", "Paint High", "Brush", "Paint Shadow")

No configuration needed — it follows whatever theme/skin is active.
