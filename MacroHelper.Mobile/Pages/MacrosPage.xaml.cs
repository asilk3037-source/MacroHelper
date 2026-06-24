using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.ViewModels;

namespace MacroHelper.Mobile.Pages;

public partial class MacrosPage : ContentPage
{
    private readonly MacrosViewModel _vm;

    public MacrosPage(MacrosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _vm.EditarCommand = new Command<MacroMobile>(async m =>
            await Shell.Current.GoToAsync("macroform", new Dictionary<string, object> { ["Macro"] = m }));
        _vm.NovaMacroCommand = new Command(async () =>
            await Shell.Current.GoToAsync("macroform"));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
