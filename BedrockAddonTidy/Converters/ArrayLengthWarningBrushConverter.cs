using System.Globalization;

namespace BedrockAddonTidy.Converters;

public class ArrayLengthWarningBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string[] warnings)
			throw new ArgumentException("Value must be a string array.", nameof(value));
		return warnings.Length > 0 ? SolidColorBrush.PaleVioletRed : SolidColorBrush.PaleGreen;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
