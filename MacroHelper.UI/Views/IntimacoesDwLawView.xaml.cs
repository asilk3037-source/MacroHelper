using MacroHelper.UI.ViewModels;
using System.Windows;
using ComboBox = System.Windows.Controls.ComboBox;
using UserControl = System.Windows.Controls.UserControl;

namespace MacroHelper.UI.Views;

public partial class IntimacoesDwLawView : UserControl
{
    public IntimacoesDwLawView() => InitializeComponent();

    // Persiste a alteração de status quando o ComboBox perde o foco
    private void StatusCombo_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox cb && cb.DataContext is IntimacaoItem item)
        {
            var vm = DataContext as IntimacoesDwLawViewModel;
            vm?.AlterarStatusCommand.Execute(item);
        }
    }
}
