using MacroHelper.Core.Entities;
using MacroHelper.UI.ViewModels;
using ComboBox = System.Windows.Controls.ComboBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MacroHelper.UI.Views;

public partial class GruposView : UserControl
{
    public GruposView() => InitializeComponent();

    private void ComboGrupo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0) return;
        if (sender is ComboBox cb && cb.Tag is Usuario usuario && DataContext is GruposViewModel vm)
        {
            usuario.GrupoId = cb.SelectedValue is int id ? id : (int?)null;
            _ = vm.AlterarGrupoUsuarioCommand.ExecuteAsync(usuario);
        }
    }
}
