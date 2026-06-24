using MacroHelper.UI.ViewModels;
using UserControl = System.Windows.Controls.UserControl;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace MacroHelper.UI.Views;

public partial class PerfilView : UserControl
{
    public PerfilView() => InitializeComponent();

    private void SenhaAtualBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PerfilViewModel vm) vm.SenhaAtual = ((PasswordBox)sender).Password;
    }

    private void NovaSenhaBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PerfilViewModel vm) vm.NovaSenha = ((PasswordBox)sender).Password;
    }

    private void ConfirmarSenhaBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PerfilViewModel vm) vm.ConfirmarSenha = ((PasswordBox)sender).Password;
    }
}
