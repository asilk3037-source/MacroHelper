using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.ViewModels;

namespace MacroHelper.Mobile.Pages;

[QueryProperty(nameof(Macro), "Macro")]
public partial class MacroFormPage : ContentPage
{
    private readonly MacroFormViewModel _vm;

    public MacroMobile? Macro
    {
        set => _ = _vm.InicializarAsync(value);
    }

    public MacroFormPage(MacroFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.Salvo     += async () => await Shell.Current.GoToAsync("..");
        vm.Cancelado += async () => await Shell.Current.GoToAsync("..");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Categorias.Count == 0)
            await _vm.InicializarAsync();
    }
}
