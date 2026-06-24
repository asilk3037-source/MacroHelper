using MacroHelper.Mobile.ViewModels;
namespace MacroHelper.Mobile.Pages;
public partial class CategoriasPage : ContentPage
{
    public CategoriasPage(CategoriasPageViewModel vm) { InitializeComponent(); BindingContext = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await ((CategoriasPageViewModel)BindingContext).CarregarAsync(); }
}
