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
        if (sender is ComboBox { Tag: Usuario usuario } && DataContext is GruposViewModel vm)
            _ = vm.AlterarGrupoUsuarioCommand.ExecuteAsync(usuario);
    }
}
