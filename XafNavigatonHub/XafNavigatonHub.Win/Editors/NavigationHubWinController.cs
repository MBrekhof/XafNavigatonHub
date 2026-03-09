using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using XafNavigatonHub.Module.Controllers;

namespace XafNavigatonHub.Win.Editors;

/// <summary>
/// Initializes the NavigationHubControl when it appears in a DashboardView's ControlDetailItem.
/// </summary>
public class NavigationHubWinController : ViewController<DashboardView>
{
    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();

        foreach (var item in View.GetItems<ControlViewItem>())
        {
            if (item.Control is NavigationHubControl hubControl)
            {
                var mainWindow = Application.MainWindow;
                var controller = mainWindow?.GetController<NavigationHubController>();
                if (controller != null)
                {
                    hubControl.Initialize(controller);
                }
            }
        }
    }
}
