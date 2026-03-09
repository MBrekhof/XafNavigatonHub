# NavigationHub Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a full-page NavigationHub home screen with role-aware categorized card buttons, user-pinnable favorites, running as a permanent first tab in TabbedMDI mode.

**Architecture:** Extend the XAF Application Model with custom nodes (`IModelNavigationHub`, `IModelHubCategory`, `IModelHubButton`) to define hub buttons. Create a `DashboardView` with a custom `ViewItem` that renders the hub UI — a Blazor Razor component on the web side, a WinForms UserControl on the Win side. A `WindowController` ensures TabbedMDI mode and makes the hub tab non-closable. User pin preferences are stored via a `UserHubPreference` business object.

**Tech Stack:** DevExpress XAF 25.2, EF Core, Blazor Server (DxTabs TabbedMDI), WinForms (DocumentManager TabbedMDI), C# / .NET 8

**Key XAF Docs References:**
- Custom model nodes: https://docs.devexpress.com/eXpressAppFramework/404125
- Custom ViewItem (Blazor): https://docs.devexpress.com/eXpressAppFramework/113653
- Custom Blazor template: https://docs.devexpress.com/eXpressAppFramework/403452
- Tab customization (Blazor): https://docs.devexpress.com/eXpressAppFramework/404978
- Tab customization (Win): https://docs.devexpress.com/eXpressAppFramework/113443
- ShowNavigationItemController: https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.SystemModule.ShowNavigationItemController
- UIType / TabbedMDI: https://docs.devexpress.com/eXpressAppFramework/404211

---

### Task 1: Enable TabbedMDI on Both Platforms

**Files:**
- Modify: `XafNavigatonHub/XafNavigatonHub.Blazor.Server/Model.xafml`
- Modify: `XafNavigatonHub/XafNavigatonHub.Win/Model.xafml`

**Step 1: Set UIType to TabbedMDI in Blazor Model.xafml**

Open `Model.xafml` in the Blazor.Server project and add/update the Options node:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application>
  <Options UIType="TabbedMDI" />
</Application>
```

**Step 2: Set UIType to TabbedMDI in Win Model.xafml**

Open `Model.xafml` in the Win project and add/update the Options node:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application>
  <Options UIType="TabbedMDI" />
</Application>
```

**Step 3: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Blazor.Server/Model.xafml XafNavigatonHub/XafNavigatonHub.Win/Model.xafml
git commit -m "feat: enable TabbedMDI on both Blazor and WinForms"
```

---

### Task 2: Define Application Model Extensions for Hub Configuration

**Files:**
- Modify: `XafNavigatonHub/XafNavigatonHub.Module/Module.cs`
- Create: `XafNavigatonHub/XafNavigatonHub.Module/Model/IModelNavigationHub.cs`

**Step 1: Create the model interfaces**

Create `XafNavigatonHub/XafNavigatonHub.Module/Model/IModelNavigationHub.cs`:

```csharp
using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace XafNavigatonHub.Module.Model;

public interface IModelNavigationHubExtension : IModelNode
{
    IModelNavigationHub NavigationHub { get; }
}

public interface IModelNavigationHub : IModelNode, IModelList<IModelHubCategory>
{
}

[KeyProperty(nameof(Id))]
public interface IModelHubCategory : IModelNode
{
    string Id { get; set; }

    [Localizable(true)]
    string Caption { get; set; }

    int SortOrder { get; set; }

    IModelHubButtons Buttons { get; }
}

public interface IModelHubButtons : IModelNode, IModelList<IModelHubButton>
{
}

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
}
```

**Step 2: Register model extension in Module.cs**

Add `ExtendModelInterfaces` override to `XafNavigatonHubModule`:

```csharp
public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders)
{
    base.ExtendModelInterfaces(extenders);
    extenders.Add<IModelApplication, IModelNavigationHubExtension>();
}
```

Add the using: `using XafNavigatonHub.Module.Model;`

**Step 3: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds. Model Editor should now show a NavigationHub node under Application.

**Step 4: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Module/Model/IModelNavigationHub.cs XafNavigatonHub/XafNavigatonHub.Module/Module.cs
git commit -m "feat: add Application Model extensions for NavigationHub config"
```

---

### Task 3: Create the UserHubPreference Business Object

**Files:**
- Create: `XafNavigatonHub/XafNavigatonHub.Module/BusinessObjects/UserHubPreference.cs`
- Modify: `XafNavigatonHub/XafNavigatonHub.Module/BusinessObjects/XafNavigatonHubDbContext.cs`

**Step 1: Create UserHubPreference entity**

Create `XafNavigatonHub/XafNavigatonHub.Module/BusinessObjects/UserHubPreference.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF;

namespace XafNavigatonHub.Module.BusinessObjects;

public class UserHubPreference : BaseObject
{
    public virtual Guid UserId { get; set; }
    public virtual string NavigationItemId { get; set; }
    public virtual int SortOrder { get; set; }
}
```

**Step 2: Add DbSet to context**

In `XafNavigatonHubEFCoreDbContext`, add:

```csharp
public DbSet<UserHubPreference> UserHubPreferences { get; set; }
```

**Step 3: Add security permissions in Updater.cs**

In the `CreateDefaultRole` method, add read/write permission for `UserHubPreference` so users can manage their own pins:

```csharp
defaultRole.AddObjectPermissionFromLambda<UserHubPreference>(
    SecurityOperations.ReadWriteAccess,
    p => p.UserId == (Guid)CurrentUserIdOperator.CurrentUserId(),
    SecurityPermissionState.Allow);
defaultRole.AddTypePermissionsRecursively<UserHubPreference>(
    SecurityOperations.Create, SecurityPermissionState.Allow);
```

**Step 4: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds.

**Step 5: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Module/BusinessObjects/UserHubPreference.cs XafNavigatonHub/XafNavigatonHub.Module/BusinessObjects/XafNavigatonHubDbContext.cs XafNavigatonHub/XafNavigatonHub.Module/DatabaseUpdate/Updater.cs
git commit -m "feat: add UserHubPreference business object for pinned favorites"
```

---

### Task 4: Create the Hub Controller (Platform-Agnostic Logic)

**Files:**
- Create: `XafNavigatonHub/XafNavigatonHub.Module/Controllers/NavigationHubController.cs`

**Step 1: Create the controller**

This `WindowController` runs in the main window. It:
- Reads the hub model config
- Filters buttons by the current user's navigation permissions
- Provides data to the platform-specific ViewItems

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using XafNavigatonHub.Module.BusinessObjects;
using XafNavigatonHub.Module.Model;

namespace XafNavigatonHub.Module.Controllers;

public class NavigationHubController : WindowController
{
    public NavigationHubController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
    }

    public List<HubCategoryData> GetHubData()
    {
        var model = (IModelNavigationHubExtension)Application.Model;
        var hubModel = model.NavigationHub;
        if (hubModel == null) return new List<HubCategoryData>();

        var showNavController = Frame.GetController<ShowNavigationItemController>();
        var navAction = showNavController?.ShowNavigationItemAction;
        var permittedItemIds = new HashSet<string>();
        if (navAction != null)
        {
            CollectPermittedItems(navAction.Items, permittedItemIds);
        }

        var categories = new List<HubCategoryData>();
        foreach (IModelHubCategory category in hubModel.OrderBy(c => c.SortOrder))
        {
            var buttons = new List<HubButtonData>();
            foreach (IModelHubButton button in category.Buttons.OrderBy(b => b.SortOrder))
            {
                if (!string.IsNullOrEmpty(button.NavigationItemId) && !permittedItemIds.Contains(button.NavigationItemId))
                    continue;
                buttons.Add(new HubButtonData
                {
                    Id = button.Id,
                    Caption = button.Caption,
                    ImageName = button.ImageName,
                    NavigationItemId = button.NavigationItemId,
                    Color = button.Color
                });
            }
            if (buttons.Count > 0)
            {
                categories.Add(new HubCategoryData
                {
                    Id = category.Id,
                    Caption = category.Caption,
                    Buttons = buttons
                });
            }
        }
        return categories;
    }

    private void CollectPermittedItems(ChoiceActionItemCollection items, HashSet<string> ids)
    {
        foreach (var item in items)
        {
            if (item.Data is ViewShortcut shortcut)
            {
                ids.Add(item.GetIdPath());
            }
            if (item.Items.Count > 0)
            {
                CollectPermittedItems(item.Items, ids);
            }
        }
    }

    public void NavigateToItem(string navigationItemId)
    {
        var showNavController = Frame.GetController<ShowNavigationItemController>();
        if (showNavController == null) return;

        var item = FindItemById(showNavController.ShowNavigationItemAction.Items, navigationItemId);
        if (item != null)
        {
            showNavController.ShowNavigationItemAction.DoExecute(item);
        }
    }

    private ChoiceActionItem FindItemById(ChoiceActionItemCollection items, string id)
    {
        foreach (var item in items)
        {
            if (item.GetIdPath() == id || (item.Data is ViewShortcut vs && vs.ViewId == id))
                return item;
            if (item.Items.Count > 0)
            {
                var found = FindItemById(item.Items, id);
                if (found != null) return found;
            }
        }
        return null;
    }

    public List<string> GetPinnedItemIds()
    {
        using var os = Application.CreateObjectSpace(typeof(UserHubPreference));
        var userId = (Guid)SecuritySystem.CurrentUserId;
        return os.GetObjects<UserHubPreference>(
            DevExpress.Data.Filtering.CriteriaOperator.Parse("UserId = ?", userId))
            .OrderBy(p => p.SortOrder)
            .Select(p => p.NavigationItemId)
            .ToList();
    }

    public void SetPinnedItems(List<string> navigationItemIds)
    {
        using var os = Application.CreateObjectSpace(typeof(UserHubPreference));
        var userId = (Guid)SecuritySystem.CurrentUserId;
        var existing = os.GetObjects<UserHubPreference>(
            DevExpress.Data.Filtering.CriteriaOperator.Parse("UserId = ?", userId)).ToList();
        foreach (var e in existing)
            os.Delete(e);
        for (int i = 0; i < navigationItemIds.Count; i++)
        {
            var pref = os.CreateObject<UserHubPreference>();
            pref.UserId = userId;
            pref.NavigationItemId = navigationItemIds[i];
            pref.SortOrder = i;
        }
        os.CommitChanges();
    }
}

public class HubCategoryData
{
    public string Id { get; set; }
    public string Caption { get; set; }
    public List<HubButtonData> Buttons { get; set; } = new();
}

public class HubButtonData
{
    public string Id { get; set; }
    public string Caption { get; set; }
    public string ImageName { get; set; }
    public string NavigationItemId { get; set; }
    public string Color { get; set; }
}
```

**Step 2: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds.

**Step 3: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Module/Controllers/NavigationHubController.cs
git commit -m "feat: add NavigationHubController with role filtering and pin management"
```

---

### Task 5: Create the Blazor Hub ViewItem and Razor Component

**Files:**
- Create: `XafNavigatonHub/XafNavigatonHub.Blazor.Server/Editors/NavigationHubComponent.razor`
- Create: `XafNavigatonHub/XafNavigatonHub.Blazor.Server/Editors/NavigationHubViewItem.cs`

**Step 1: Create the Razor component**

Create `NavigationHubComponent.razor` — the card grid UI with categories and pin support:

```razor
@namespace XafNavigatonHub.Blazor.Server.Editors

@using DevExpress.ExpressApp.Blazor.Editors
@using XafNavigatonHub.Module.Controllers

<div class="navigation-hub" style="padding: 24px; overflow-y: auto;">
    @if (PinnedButtons.Any())
    {
        <div class="hub-section" style="margin-bottom: 32px;">
            <h5 style="color: #666; margin-bottom: 16px; font-weight: 600;">
                ★ Preferred Actions
            </h5>
            <div style="display: flex; flex-wrap: wrap; gap: 16px;">
                @foreach (var button in PinnedButtons)
                {
                    <div class="hub-card"
                         style="@GetCardStyle(button.Color)"
                         @onclick="() => OnButtonClick(button)"
                         @oncontextmenu="() => UnpinButton(button)"
                         @oncontextmenu:preventDefault="true">
                        <div class="hub-card-icon">
                            <img src="_content/DevExpress.ExpressApp.Blazor/images/@(button.ImageName).svg"
                                 onerror="this.style.display='none'"
                                 style="width: 32px; height: 32px;" />
                        </div>
                        <div class="hub-card-label">@button.Caption</div>
                    </div>
                }
            </div>
        </div>
    }

    @foreach (var category in Categories)
    {
        <div class="hub-section" style="margin-bottom: 32px;">
            <h5 style="color: #666; margin-bottom: 16px; font-weight: 600;">
                @category.Caption
            </h5>
            <div style="display: flex; flex-wrap: wrap; gap: 16px;">
                @foreach (var button in category.Buttons)
                {
                    <div class="hub-card"
                         style="@GetCardStyle(button.Color)"
                         @onclick="() => OnButtonClick(button)"
                         @oncontextmenu="() => PinButton(button)"
                         @oncontextmenu:preventDefault="true">
                        <div class="hub-card-icon">
                            <img src="_content/DevExpress.ExpressApp.Blazor/images/@(button.ImageName).svg"
                                 onerror="this.style.display='none'"
                                 style="width: 32px; height: 32px;" />
                        </div>
                        <div class="hub-card-label">@button.Caption</div>
                    </div>
                }
            </div>
        </div>
    }
</div>

<style>
    .hub-card {
        width: 160px;
        min-height: 120px;
        border-radius: 12px;
        padding: 20px 16px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 12px;
        cursor: pointer;
        transition: transform 0.15s ease, box-shadow 0.15s ease;
        user-select: none;
    }
    .hub-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(0,0,0,0.15);
    }
    .hub-card-label {
        font-size: 14px;
        font-weight: 500;
        text-align: center;
    }
</style>

@code {
    [Parameter] public List<HubCategoryData> Categories { get; set; } = new();
    [Parameter] public List<HubButtonData> PinnedButtons { get; set; } = new();
    [Parameter] public EventCallback<HubButtonData> ButtonClicked { get; set; }
    [Parameter] public EventCallback<HubButtonData> ButtonPinned { get; set; }
    [Parameter] public EventCallback<HubButtonData> ButtonUnpinned { get; set; }

    private string GetCardStyle(string color)
    {
        var c = string.IsNullOrEmpty(color) ? "#1976D2" : color;
        return $"background: white; border: 1px solid #e0e0e0; box-shadow: 0 2px 8px rgba(0,0,0,0.08); border-left: 4px solid {c};";
    }

    private async Task OnButtonClick(HubButtonData button)
    {
        await ButtonClicked.InvokeAsync(button);
    }

    private async Task PinButton(HubButtonData button)
    {
        await ButtonPinned.InvokeAsync(button);
    }

    private async Task UnpinButton(HubButtonData button)
    {
        await ButtonUnpinned.InvokeAsync(button);
    }
}
```

**Step 2: Create the ViewItem**

Create `NavigationHubViewItem.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using XafNavigatonHub.Module.Controllers;

namespace XafNavigatonHub.Blazor.Server.Editors;

public interface IModelNavigationHubViewItem : IModelViewItem;

[ViewItem(typeof(IModelNavigationHubViewItem))]
public class NavigationHubViewItem : ViewItem, IComponentContentHolder, IComplexViewItem
{
    private XafApplication application;
    private IObjectSpace objectSpace;
    private NavigationHubComponentModel componentModel;

    public NavigationHubViewItem(IModelViewItem model, Type objectType)
        : base(objectType, model.Id) { }

    public RenderFragment ComponentContent =>
        ComponentModelObserver.Create(componentModel, componentModel.GetComponentContent());

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        this.objectSpace = objectSpace;
        this.application = application;
    }

    protected override object CreateControlCore()
    {
        componentModel = new NavigationHubComponentModel();
        RefreshData();
        return componentModel;
    }

    private void RefreshData()
    {
        var controller = application.MainWindow?.GetController<NavigationHubController>();
        if (controller == null) return;

        var categories = controller.GetHubData();
        var pinnedIds = controller.GetPinnedItemIds();
        var allButtons = categories.SelectMany(c => c.Buttons).ToList();
        var pinnedButtons = pinnedIds
            .Select(id => allButtons.FirstOrDefault(b => b.NavigationItemId == id))
            .Where(b => b != null)
            .ToList();

        componentModel.Categories = categories;
        componentModel.PinnedButtons = pinnedButtons;
        componentModel.ButtonClicked = EventCallback.Factory.Create<HubButtonData>(this, OnButtonClicked);
        componentModel.ButtonPinned = EventCallback.Factory.Create<HubButtonData>(this, OnButtonPinned);
        componentModel.ButtonUnpinned = EventCallback.Factory.Create<HubButtonData>(this, OnButtonUnpinned);
    }

    private void OnButtonClicked(HubButtonData button)
    {
        var controller = application.MainWindow?.GetController<NavigationHubController>();
        controller?.NavigateToItem(button.NavigationItemId);
    }

    private void OnButtonPinned(HubButtonData button)
    {
        var controller = application.MainWindow?.GetController<NavigationHubController>();
        if (controller == null) return;
        var pinned = controller.GetPinnedItemIds();
        if (!pinned.Contains(button.NavigationItemId))
        {
            pinned.Add(button.NavigationItemId);
            controller.SetPinnedItems(pinned);
            RefreshData();
        }
    }

    private void OnButtonUnpinned(HubButtonData button)
    {
        var controller = application.MainWindow?.GetController<NavigationHubController>();
        if (controller == null) return;
        var pinned = controller.GetPinnedItemIds();
        pinned.Remove(button.NavigationItemId);
        controller.SetPinnedItems(pinned);
        RefreshData();
    }
}

public class NavigationHubComponentModel : ComponentModelBase
{
    public List<HubCategoryData> Categories
    {
        get => GetPropertyValue<List<HubCategoryData>>();
        set => SetPropertyValue(value);
    }

    public List<HubButtonData> PinnedButtons
    {
        get => GetPropertyValue<List<HubButtonData>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<HubButtonData> ButtonClicked
    {
        get => GetPropertyValue<EventCallback<HubButtonData>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<HubButtonData> ButtonPinned
    {
        get => GetPropertyValue<EventCallback<HubButtonData>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<HubButtonData> ButtonUnpinned
    {
        get => GetPropertyValue<EventCallback<HubButtonData>>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(NavigationHubComponent);
}
```

**Step 3: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Blazor.Server/Editors/NavigationHubComponent.razor XafNavigatonHub/XafNavigatonHub.Blazor.Server/Editors/NavigationHubViewItem.cs
git commit -m "feat: add Blazor NavigationHub ViewItem and Razor component"
```

---

### Task 6: Register the Hub as a DashboardView and Startup Navigation Item

**Files:**
- Modify: `XafNavigatonHub/XafNavigatonHub.Blazor.Server/Model.xafml`

**Step 1: Add the DashboardView and navigation item in Model.xafml**

Add to the Blazor `Model.xafml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application>
  <Options UIType="TabbedMDI" />
  <Views>
    <DashboardView Id="NavigationHub_DashboardView">
      <Items>
        <ControlDetailItem Id="NavigationHubControl"
          ControlTypeName="XafNavigatonHub.Blazor.Server.Editors.NavigationHubComponent" />
      </Items>
    </DashboardView>
  </Views>
  <NavigationItems StartupNavigationItem="NavigationHub_NavItem">
    <Items>
      <Item Id="NavigationHub_NavItem" Caption="Home"
        ViewId="NavigationHub_DashboardView" IsNewNode="True" Index="0" />
    </Items>
  </NavigationItems>
</Application>
```

**Step 2: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`
Expected: Build succeeds.

**Step 3: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Blazor.Server/Model.xafml
git commit -m "feat: register NavigationHub DashboardView as startup navigation item"
```

---

### Task 7: Add Sample Hub Configuration in Module Model

**Files:**
- Modify: `XafNavigatonHub/XafNavigatonHub.Module/Model.DesignedDiffs.xafml`

**Step 1: Add sample hub categories and buttons**

This provides a working example that developers can modify in the Model Editor. Update `Model.DesignedDiffs.xafml` to include sample NavigationHub nodes. The exact XML depends on what navigation items exist. Start with a placeholder structure:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application>
  <NavigationHub>
    <Item Id="General" Caption="General" SortOrder="0">
      <Buttons>
        <!-- Add buttons as navigation items are created -->
      </Buttons>
    </Item>
  </NavigationHub>
</Application>
```

**Step 2: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`

**Step 3: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Module/Model.DesignedDiffs.xafml
git commit -m "feat: add sample hub configuration in module model"
```

---

### Task 8: Ensure Hub Tab is Non-Closable (Blazor)

**Files:**
- Create: `XafNavigatonHub/XafNavigatonHub.Blazor.Server/Controllers/HubTabController.cs`

**Step 1: Create HubTabController**

This controller hooks into the `ITabbedMdiMainFormTemplate.Closing` event to prevent closing the hub tab:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Templates;

namespace XafNavigatonHub.Blazor.Server.Controllers;

public class HubTabController : WindowController
{
    public HubTabController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        Window.TemplateChanged += Window_TemplateChanged;
    }

    private void Window_TemplateChanged(object sender, EventArgs e)
    {
        if (Window.Template is ITabbedMdiMainFormTemplateClosing template)
        {
            template.Closing += Template_Closing;
        }
    }

    private void Template_Closing(object sender, DetailFormTemplateClosingEventArgs e)
    {
        if (e.DetailFormTemplate is ITabbedMdiDetailFormTemplate detailTemplate)
        {
            var view = detailTemplate.Frame?.View;
            if (view?.Id == "NavigationHub_DashboardView")
            {
                e.Cancel = true;
            }
        }
    }

    protected override void OnDeactivated()
    {
        Window.TemplateChanged -= Window_TemplateChanged;
        base.OnDeactivated();
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build XafNavigatonHub.slnx`

**Step 3: Commit**

```bash
git add XafNavigatonHub/XafNavigatonHub.Blazor.Server/Controllers/HubTabController.cs
git commit -m "feat: prevent closing the NavigationHub tab in Blazor TabbedMDI"
```

---

### Task 9: Update Documentation and Session Handoff

**Files:**
- Modify: `HOW_TO_IMPLEMENT.md`
- Modify: `SESSION_HANDOFF.md`
- Modify: `TODO.md`

**Step 1: Add NavigationHub section to HOW_TO_IMPLEMENT.md**

Add instructions on how to add hub buttons via the Model Editor.

**Step 2: Update SESSION_HANDOFF.md with current state**

**Step 3: Update TODO.md**

Mark completed tasks, add remaining items (WinForms hub ViewItem, drag & drop, testing).

**Step 4: Commit and push**

```bash
git add HOW_TO_IMPLEMENT.md SESSION_HANDOFF.md TODO.md
git commit -m "docs: update implementation guide and session handoff"
git push
```

---

## Deferred Tasks (Future Sessions)

- **WinForms Hub ViewItem**: Create a WinForms UserControl equivalent of the Blazor component with painted cards
- **WinForms Hub Tab Non-Closable**: Use `DocumentManager` to prevent closing the hub document
- **Drag & Drop Pinning**: HTML5 drag API on Blazor, mouse events on WinForms
- **Runtime Admin UI (Phase 2)**: Business objects for hub config, admin CRUD screen
- **External URL Support**: Add URL property to model, open in browser on click
