using System.Globalization;

namespace BedrockAddonTidy.Converters;

public class StringEmptyToBoolConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var invert = parameter as string == "invert";
		bool isTrue = !string.IsNullOrWhiteSpace(value as string);
		return invert ? !isTrue : isTrue;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}