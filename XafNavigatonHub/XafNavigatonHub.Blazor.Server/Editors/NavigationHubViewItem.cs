using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Components.Models;
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
    private NavigationHubComponentModel componentModel;

    public NavigationHubViewItem(IModelViewItem model, Type objectType)
        : base(objectType, model.Id) { }

    public RenderFragment ComponentContent =>
        componentModel != null
            ? ComponentModelObserver.Create(componentModel, componentModel.GetComponentContent())
            : builder => { };

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        this.application = application;
    }

    protected override object CreateControlCore()
    {
        componentModel = new NavigationHubComponentModel();
        RefreshData();
        return componentModel;
    }

    private NavigationHubController GetHubController() =>
        application.MainWindow?.GetController<NavigationHubController>();

    private void RefreshData()
    {
        var controller = GetHubController();
        if (controller == null) return;

        var categories = controller.GetHubData();
        var pinnedIds = controller.GetPinnedItemIds();
        var allButtons = categories.SelectMany(c => c.Buttons).ToList();
        var pinnedButtons = pinnedIds
            .Select(id => allButtons.FirstOrDefault(b => b.NavigationItemId == id))
            .OfType<HubButtonData>()
            .ToList();

        componentModel.Categories = categories;
        componentModel.PinnedButtons = pinnedButtons;
        componentModel.ButtonClicked = EventCallback.Factory.Create<HubButtonData>(this, OnButtonClicked);
        componentModel.ButtonPinned = EventCallback.Factory.Create<HubButtonData>(this, OnButtonPinned);
        componentModel.ButtonUnpinned = EventCallback.Factory.Create<HubButtonData>(this, OnButtonUnpinned);
    }

    private void OnButtonClicked(HubButtonData button)
    {
        GetHubController()?.NavigateToItem(button.NavigationItemId);
    }

    private void OnButtonPinned(HubButtonData button)
    {
        var controller = GetHubController();
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
        var controller = GetHubController();
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
