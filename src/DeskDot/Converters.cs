using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeskDot;

/// <summary>Maps a 0..1 ratio (used by the week bars) onto a 0..100 ProgressBar value.</summary>
public sealed class RatioToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            double ratio => Math.Clamp(ratio, 0, 1) * 100,
            float ratio => Math.Clamp(ratio, 0f, 1f) * 100,
            _ => 0d
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double percent ? percent / 100 : 0d;
}

/// <summary>Shows an element only when the bound flag is false (empty states).</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Visibility.Visible;
}
