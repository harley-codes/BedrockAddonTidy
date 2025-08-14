using System.Globalization;

namespace BedrockAddonTidy.Converters;

public class BoolInvertConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var invert = parameter as string == "invert";
		return value is bool b ? (invert ? !b : b) : value;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var invert = parameter as string == "invert";
		return value is bool b ? (invert ? !b : b) : value;
	}
}
