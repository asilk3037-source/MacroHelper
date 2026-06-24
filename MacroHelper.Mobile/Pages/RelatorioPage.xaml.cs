using MacroHelper.Mobile.ViewModels;
namespace MacroHelper.Mobile.Pages;
public partial class RelatorioPage : ContentPage
{
    public RelatorioPage(RelatorioPageViewModel vm) { InitializeComponent(); BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await ((RelatorioPageViewModel)BindingContext).CarregarAsync(); }
}
