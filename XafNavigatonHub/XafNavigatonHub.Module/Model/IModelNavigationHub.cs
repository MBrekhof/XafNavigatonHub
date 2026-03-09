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

    string ExternalUrl { get; set; }
}
