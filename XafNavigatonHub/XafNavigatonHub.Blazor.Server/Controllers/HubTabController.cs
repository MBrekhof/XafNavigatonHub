using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Templates;

namespace XafNavigatonHub.Blazor.Server.Controllers;

public class HubTabController : WindowController
{
    private ITabbedMdiDetailFormTemplate hubTemplate;

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
        if (Window.Template is ITabbedMdiMainFormTemplate mainTemplate)
        {
            // Capture the hub template when it first appears as the active template
            mainTemplate.ChildTemplatesChanged += MainTemplate_ChildTemplatesChanged;
            // Also check immediately in case it's already loaded
            CaptureHubTemplate(mainTemplate);
        }
        if (Window.Template is ITabbedMdiMainFormTemplateClosing closingTemplate)
        {
            closingTemplate.Closing += Template_Closing;
        }
    }

    private void MainTemplate_ChildTemplatesChanged(object sender, EventArgs e)
    {
        if (sender is ITabbedMdiMainFormTemplate mainTemplate)
        {
            CaptureHubTemplate(mainTemplate);
        }
    }

    private void CaptureHubTemplate(ITabbedMdiMainFormTemplate mainTemplate)
    {
        // The startup navigation item (hub) is the first tab, so index 0
        if (hubTemplate == null && mainTemplate.ActiveTemplateIndex == 0 && mainTemplate.ActiveTemplate != null)
        {
            hubTemplate = mainTemplate.ActiveTemplate;
        }
    }

    private void Template_Closing(object sender, DetailFormTemplateClosingEventArgs e)
    {
        if (hubTemplate != null && e.Template == hubTemplate)
        {
            e.Cancel = true;
        }
    }

    protected override void OnDeactivated()
    {
        Window.TemplateChanged -= Window_TemplateChanged;
        if (Window.Template is ITabbedMdiMainFormTemplate mainTemplate)
        {
            mainTemplate.ChildTemplatesChanged -= MainTemplate_ChildTemplatesChanged;
        }
        if (Window.Template is ITabbedMdiMainFormTemplateClosing closingTemplate)
        {
            closingTemplate.Closing -= Template_Closing;
        }
        hubTemplate = null;
        base.OnDeactivated();
    }
}
