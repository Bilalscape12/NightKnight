using System;
using System.Globalization;
using System.Windows.Data;

namespace NightKnight
{
    public class OvernightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is TimeSpan startTime && values[1] is TimeSpan endTime)
            {
                return startTime > endTime ? "Overnight" : "Same Day";
            }
            return "Unknown";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 