using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DS3ConnectionInfo
{
    public sealed class ColorLabelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 0 || !(values[0] is Color color)) return "";
            if (values.Length > 1 && values[1] is System.Collections.Generic.Dictionary<Color?, string> names &&
                names.TryGetValue(color, out string name)) return name;
            return color.ToString();
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    public sealed class FieldColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string color = null;
            if (values.Length > 0 && values[0] is StringCollection colors &&
                int.TryParse(parameter?.ToString(), out int index) && index >= 0 && index < colors.Count)
                color = colors[index];
            if (!string.IsNullOrEmpty(color))
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
                catch (Exception) { } // Ignore invalid legacy settings and preserve the original rule.
            }
            if (values.Length > 1)
            {
                if (values[1] is Brush brush) return brush;
                if (values[1] is string original)
                {
                    try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(original)); }
                    catch (Exception) { }
                }
            }
            return Brushes.White;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
