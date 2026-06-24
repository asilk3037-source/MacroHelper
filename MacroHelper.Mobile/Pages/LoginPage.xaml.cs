using MacroHelper.Mobile.ViewModels;

namespace MacroHelper.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.LoginSucesso += () => Shell.Current.GoToAsync("//macros");
    }
}
