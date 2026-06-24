using MacroHelper.Mobile.ViewModels;
namespace MacroHelper.Mobile.Pages;
public partial class ConfigPage : ContentPage
{
    public ConfigPage(ConfigPageViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
