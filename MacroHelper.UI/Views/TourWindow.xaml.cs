using System.Windows;

namespace MacroHelper.UI.Views;

public partial class TourWindow : Window
{
    private readonly (string Icone, string Titulo, string Descricao)[] _passos =
    [
        ("", "Bem-vindo ao SK MacroHelper!", "Crie respostas e textos prontos e insira-os em qualquer aplicativo do Windows com um atalho rápido."),
        ("", "Gatilho por texto", "Digite \"/\" seguido do atalho da macro (ex: /chamado-recebido) em qualquer campo de texto para ver as sugestões."),
        ("", "Busca rápida", "Pressione Ctrl+Espaço em qualquer lugar do Windows para abrir o buscador rápido de macros, mesmo com o app minimizado."),
        ("", "Organize por categoria", "Use a aba Categorias para agrupar suas macros e a aba Relatório para acompanhar o uso ao longo do tempo."),
        ("", "Personalize em Configurações", "Ajuste tema, cor de destaque, prefixo do gatilho, backup automático e sincronização — tudo em Configurações.")
    ];

    private int _indice;

    public TourWindow()
    {
        InitializeComponent();
        AtualizarPasso();
    }

    private void AtualizarPasso()
    {
        var p = _passos[_indice];
        TxtIcone.Text = p.Icone;
        TxtTitulo.Text = p.Titulo;
        TxtDescricao.Text = p.Descricao;
        TxtPasso.Text = $"{_indice + 1} de {_passos.Length}";
        BtnProximo.Content = _indice == _passos.Length - 1 ? "Concluir" : "Próximo";
    }

    private void BtnProximo_Click(object sender, RoutedEventArgs e)
    {
        if (_indice < _passos.Length - 1) { _indice++; AtualizarPasso(); }
        else Close();
    }

    private void BtnPular_Click(object sender, RoutedEventArgs e) => Close();
}
