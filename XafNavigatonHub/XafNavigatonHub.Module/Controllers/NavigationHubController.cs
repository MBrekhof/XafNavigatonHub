using DevExpress.Data.Filtering;
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
            if (item.Data is ViewShortcut)
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
        var navAction = showNavController?.ShowNavigationItemAction;
        if (navAction == null) return;

        var item = FindItemById(navAction.Items, navigationItemId);
        if (item != null)
        {
            navAction.DoExecute(item);
        }
    }

    private ChoiceActionItem FindItemById(ChoiceActionItemCollection items, string id)
    {
        foreach (var item in items)
        {
            // Match by full path (category/item) or by ViewId for flexibility
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
        if (SecuritySystem.CurrentUserId is not Guid userId)
            return new List<string>();

        using var os = Application.CreateObjectSpace(typeof(UserHubPreference));
        return os.GetObjects<UserHubPreference>(
            CriteriaOperator.Parse("UserId = ?", userId))
            .OrderBy(p => p.SortOrder)
            .Select(p => p.NavigationItemId)
            .ToList();
    }

    public void SetPinnedItems(List<string> navigationItemIds)
    {
        if (navigationItemIds == null) return;
        if (SecuritySystem.CurrentUserId is not Guid userId) return;

        using var os = Application.CreateObjectSpace(typeof(UserHubPreference));
        var existing = os.GetObjects<UserHubPreference>(
            CriteriaOperator.Parse("UserId = ?", userId)).ToList();
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
    public string Id { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public List<HubButtonData> Buttons { get; set; } = new();
}

public class HubButtonData
{
    public string Id { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string NavigationItemId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
