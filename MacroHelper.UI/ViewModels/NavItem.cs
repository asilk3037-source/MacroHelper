using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace MacroHelper.UI.ViewModels;

/// <summary>Item de navegação da sidebar, usado para gerar os botões via ItemsControl em vez de XAML repetido.</summary>
public partial class NavItem : ObservableObject
{
    public string     Icone   { get; init; } = string.Empty;
    public string     Label   { get; init; } = string.Empty;
    public string     Chave   { get; init; } = string.Empty;
    public ICommand?  Comando { get; init; }

    [ObservableProperty] private bool _ativo;
    [ObservableProperty] private bool _visivel = true;
}
