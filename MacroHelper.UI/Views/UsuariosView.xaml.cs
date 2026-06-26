using MacroHelper.UI.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
namespace MacroHelper.UI.Views;

public partial class UsuariosView : UserControl
{
    public UsuariosView() => InitializeComponent();

    private void RadioPerfilUsuario_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel vm) vm.FormPerfil = "Usuario";
    }

    private void RadioPerfilAdmin_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel vm) vm.FormPerfil = "Admin";
    }

    private void RadioPerfilAuditor_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel vm) vm.FormPerfil = "Auditor";
    }
}
