using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MacroHelper.UI.Converters;

/// <summary>Visível quando o objeto é não-nulo (qualquer referência, não só string). ConverterParameter="invert" inverte.</summary>
public class ObjectNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool ehNulo = value == null;
        bool inverter = parameter as string == "invert";
        return (inverter ? !ehNulo : ehNulo) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Visível quando o valor (string) é igual ao ConverterParameter. Ex: ConverterParameter="Admin".</summary>
public class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Compara o valor (string) com o ConverterParameter e devolve bool — usado para pré-marcar RadioButtons a partir de uma propriedade string.</summary>
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Compara dois valores (ex: int? selecionado vs Id do item) e devolve Visibility.
/// ConverterParameter="invert" inverte o resultado (visível quando DIFERENTE).</summary>
public class EqualsToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return Visibility.Collapsed;
        bool iguais = Equals(values[0], values[1]);
        bool inverter = parameter as string == "invert";
        return (inverter ? !iguais : iguais) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte um hex (#RRGGBB) em SolidColorBrush. ConverterParameter="soft" devolve a mesma cor em baixa opacidade (uso como fundo de tile).</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value as string;
        System.Windows.Media.Color color;
        try { color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(hex) ? "#818CF8" : hex); }
        catch { color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#818CF8"); }
        return new SolidColorBrush(color) { Opacity = parameter as string == "soft" ? 0.16 : 1.0 };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>True (modo totem) -> coluna recolhida (0); False -> largura normal da sidebar.</summary>
public class KioskSidebarWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? new GridLength(0) : new GridLength(248);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntGreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Inverso do IntGreaterThanZeroToVisibilityConverter — usado para estados vazios (mostra quando a contagem é 0).</summary>
public class IntZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


/// <summary>Qualquer objeto não-nulo → true; null → false. Útil para IsEnabled.</summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Nome completo -> iniciais (até 2 letras), usado no avatar circular do usuário.</summary>
public class NameInitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var nome = (value as string)?.Trim();
        if (string.IsNullOrEmpty(nome)) return "?";
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant(),
            _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Status de IntimacaoErro → SolidColorBrush colorido para o pill de status.</summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value as string switch
        {
            "Novo"              => "#F59E0B",
            "Recorrente"        => "#EF4444",
            "Em andamento"      => "#3B82F6",
            "Pendente cliente"  => "#EA580C",
            "Resolvido"         => "#10B981",
            _                   => "#6B7280",
        };
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>bool → Visibility. True = Visible, False = Collapsed (ConverterParameter="invert" inverte).</summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool bv && bv;
        bool inv = parameter as string == "invert";
        return (inv ? !b : b) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
