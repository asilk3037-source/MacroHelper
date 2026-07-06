using MacroHelper.Data.Context;
using MacroHelper.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;
using Application = System.Windows.Application;

namespace MacroHelper.UI.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow()
    {
        InitializeComponent();
        _vm = App.Services.GetRequiredService<LoginViewModel>();
        DataContext = _vm;
        _vm.LoginSucesso += AbrirMain;
        Activated += (_, _) => App.RestaurarIdiomaDoSistema();
    }

    private void AbrirMain()
    {
        var supabaseCtx = App.Services.GetRequiredService<SupabaseContext>();
        var sessao = supabaseCtx.Auth.CurrentSession;
        if (sessao?.AccessToken != null && sessao.RefreshToken != null)
            SupabaseSessionStore.Save(sessao.AccessToken, sessao.RefreshToken);

        var main = App.Services.GetRequiredService<MainWindow>();
        main.Show();
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void TxtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.SenhaTemp = TxtSenha.Password;

    private void TxtSenha_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.EntrarCommand.CanExecute(null))
            _vm.EntrarCommand.Execute(null);
    }

    private void TxtNovaSenha_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.NovaSenhaTemp = TxtNovaSenha.Password;

    private void TxtConfirmar_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.ConfirmarTemp = TxtConfirmar.Password;
}
