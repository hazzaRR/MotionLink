using System.Globalization;

namespace MotionLink.Converters;
public class GuidEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = the UUID of the device in the row
        // parameter = the UUID of the connected device from the Service
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }


    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();

}