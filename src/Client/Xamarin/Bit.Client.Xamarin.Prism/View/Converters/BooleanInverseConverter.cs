using System;
using System.Globalization;

namespace Bit.View
{
    public class BooleanInverseConverter : ValueConverter<bool, bool>
    {
        public override bool Convert(bool value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return !value;
        }

        public override bool ConvertBack(bool value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return !value;
        }
    }
}
