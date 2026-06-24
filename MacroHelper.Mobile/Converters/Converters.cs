using System.Globalization;

namespace MacroHelper.Mobile.Converters;

/// <summary>Returns true/Visible when string is NOT null or empty</summary>
public class NotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        var hasValue = !string.IsNullOrWhiteSpace(s);
        if (targetType == typeof(bool)) return hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Inverts a boolean</summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Primeira letra do nome, em maiúscula — usado como avatar de categoria (sem depender de fonte de ícone).</summary>
public class PrimeiraLetraConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        return string.IsNullOrWhiteSpace(s) ? "?" : s.Trim()[..1].ToUpperInvariant();
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns different text based on bool.
/// parameter = "TextWhenFalse|TextWhenTrue"
/// </summary>
public class LoadingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isLoading = value is bool b && b;
        var parts     = (parameter as string)?.Split('|') ?? ["OK", "Carregando..."];
        return isLoading ? (parts.Length > 1 ? parts[1] : "Carregando...") : parts[0];
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
