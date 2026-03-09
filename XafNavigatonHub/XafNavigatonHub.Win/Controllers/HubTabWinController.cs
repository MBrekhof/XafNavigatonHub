using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Win;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;

namespace XafNavigatonHub.Win.Controllers;

/// <summary>
/// Prevents the NavigationHub tab from being closed in WinForms TabbedMDI mode.
/// </summary>
public class HubTabWinController : WindowController
{
    private const string HubViewId = "NavigationHub_DashboardView";
    private DocumentManager documentManager;

    public HubTabWinController()
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
        UnsubscribeDocumentManager();

        if (Window.Template is IDocumentsHostWindow documentsHost)
        {
            documentManager = documentsHost.DocumentManager;
            if (documentManager != null)
            {
                documentManager.ViewChanged += DocumentManager_ViewChanged;
                SubscribeTabbedView(documentManager.View);
            }
        }
    }

    private void DocumentManager_ViewChanged(object sender, ViewEventArgs e)
    {
        SubscribeTabbedView(e.View);
    }

    private void SubscribeTabbedView(BaseView view)
    {
        if (view is TabbedView tabbedView)
        {
            tabbedView.DocumentClosing -= TabbedView_DocumentClosing;
            tabbedView.DocumentClosing += TabbedView_DocumentClosing;
        }
    }

    private void TabbedView_DocumentClosing(object sender, DocumentCancelEventArgs e)
    {
        if (IsHubDocument(e.Document))
        {
            e.Cancel = true;
        }
    }

    private bool IsHubDocument(BaseDocument document)
    {
        if (document?.Control is not System.Windows.Forms.Form mdiChild)
            return false;

        // In XAF WinForms TabbedMDI, the MdiShowViewStrategy associates each MDI child form
        // with a WinWindow. Check via the strategy's Explorers collection.
        if (Application.ShowViewStrategy is MdiShowViewStrategy mdiStrategy)
        {
            foreach (WinWindow explorer in mdiStrategy.Explorers)
            {
                if (explorer.Form == mdiChild && explorer.View?.Id == HubViewId)
                    return true;
            }
        }

        return false;
    }

    private void UnsubscribeDocumentManager()
    {
        if (documentManager != null)
        {
            documentManager.ViewChanged -= DocumentManager_ViewChanged;
            if (documentManager.View is TabbedView tabbedView)
            {
                tabbedView.DocumentClosing -= TabbedView_DocumentClosing;
            }
            documentManager = null;
        }
    }

    protected override void OnDeactivated()
    {
        Window.TemplateChanged -= Window_TemplateChanged;
        UnsubscribeDocumentManager();
        base.OnDeactivated();
    }
}
