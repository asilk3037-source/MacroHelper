namespace MacroHelper.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("macroform", typeof(Pages.MacroFormPage));
    }
}
