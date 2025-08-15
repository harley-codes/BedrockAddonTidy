using System;
using System.Globalization;

namespace BedrockAddonTidy.Converters;

public class ObjectNullableToBoolConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var invert = parameter as string == "invert";
		bool isTrue = value is not null;
		return invert ? !isTrue : isTrue;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
