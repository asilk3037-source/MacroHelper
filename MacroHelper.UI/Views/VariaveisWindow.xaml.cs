using MacroHelper.Services;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;

namespace MacroHelper.UI.Views;

public partial class VariaveisWindow : Window
{
    private readonly List<VariavelInfo>  _variaveis;
    private readonly string              _template;
    private readonly List<TextBox>       _inputs = new();

    public string? ConteudoFinal { get; private set; }

    public VariaveisWindow(string template, List<VariavelInfo> variaveis)
    {
        InitializeComponent();
        _template  = template;
        _variaveis = variaveis;

        ConstruirCampos();
        AtualizarPrevia();
    }

    private void ConstruirCampos()
    {
        foreach (var v in _variaveis)
        {
            var bloco = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

            // Header do campo
            var header = new Grid();
            var col1 = new ColumnDefinition();
            var col2 = new ColumnDefinition { Width = System.Windows.GridLength.Auto };
            header.ColumnDefinitions.Add(col1);
            header.ColumnDefinitions.Add(col2);
            header.Margin = new Thickness(0, 0, 0, 6);

            // Badge com nome da variável
            var badge = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding      = new Thickness(7, 2, 7, 2),
                Background   = FindResource("AccentLightBrush") as System.Windows.Media.Brush
            };
            var badgeTxt = new TextBlock
            {
                Text       = $"{{{v.Nome}}}",
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize   = 11,
                FontWeight = FontWeights.Bold
            };
            badgeTxt.SetResourceReference(ForegroundProperty, "AccentTextBrush");
            badge.Child = badgeTxt;

            var labelPanel = new StackPanel { Orientation = Orientation.Horizontal };
            labelPanel.Children.Add(badge);
            var labelTxt = new TextBlock
            {
                Text       = $"  {v.Placeholder}",
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            labelTxt.SetResourceReference(ForegroundProperty, "TextSecondaryBrush");
            labelPanel.Children.Add(labelTxt);
            Grid.SetColumn(labelPanel, 0);
            header.Children.Add(labelPanel);

            // Badge "auto"
            if (v.AutoPreencher)
            {
                var autoBadge = new Border
                {
                    CornerRadius = new CornerRadius(5),
                    Padding      = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                autoBadge.SetResourceReference(Border.BackgroundProperty, "SuccessBgBrush");
                var autoTxt = new TextBlock { Text = "auto", FontSize = 10, FontWeight = FontWeights.SemiBold };
                autoTxt.SetResourceReference(ForegroundProperty, "SuccessBrush");
                autoBadge.Child = autoTxt;
                Grid.SetColumn(autoBadge, 1);
                header.Children.Add(autoBadge);
            }

            bloco.Children.Add(header);

            // Input
            var input = new TextBox
            {
                Tag    = v.Nome,
                Text   = v.ValorPadrao ?? string.Empty,
                Height = 40
            };
            input.SetResourceReference(StyleProperty, "InputStyle");
            input.TextChanged += (_, _) =>
            {
                v.ValorPadrao = input.Text;
                AtualizarPrevia();
            };

            _inputs.Add(input);
            bloco.Children.Add(input);
            PainelCampos.Children.Add(bloco);
        }

        // Foca no primeiro campo não-auto
        Loaded += (_, _) =>
        {
            var primeiro = _inputs.FirstOrDefault(t =>
            {
                var nome = t.Tag?.ToString() ?? string.Empty;
                return !new[] { "data","hora","datahora","usuario","mes" }
                    .Contains(nome.ToLower());
            }) ?? _inputs.FirstOrDefault();
            primeiro?.Focus();
            primeiro?.SelectAll();
        };
    }

    private void AtualizarPrevia()
    {
        var valores  = _variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? string.Empty);
        var preview  = VariavelService.Substituir(_template, valores);
        TxtPrevia.Text = preview.Length > 150 ? preview[..150] + "…" : preview;
    }

    private void BtnInserir_Click(object sender, RoutedEventArgs e)
    {
        var valores   = _variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? string.Empty);
        ConteudoFinal = VariavelService.Substituir(_template, valores);
        DialogResult  = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
