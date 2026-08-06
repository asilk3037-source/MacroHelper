using MacroHelper.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UserControl = System.Windows.Controls.UserControl;
namespace MacroHelper.UI.Views;

public partial class UsuariosView : UserControl
{
    public UsuariosView() => InitializeComponent();

    // Abre o ContextMenu do botão ⋯ ao clicar com o botão esquerdo
    private void BtnMais_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.ContextMenu is ContextMenu cm)
        {
            cm.PlacementTarget = btn;
            cm.Placement = PlacementMode.Bottom;
            cm.IsOpen = true;
        }
    }

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
