using MacroHelper.Core.Entities;
using MacroHelper.UI.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace MacroHelper.UI.Views;

public partial class NotificacoesView : UserControl
{
    public NotificacoesView() => InitializeComponent();

    private void Item_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: Notificacao n } && DataContext is NotificacoesViewModel vm)
            _ = vm.MarcarComoLidaCommand.ExecuteAsync(n);
    }
}
