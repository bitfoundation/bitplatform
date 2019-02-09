using System.Globalization;

namespace System
{
    public static class StringExtensions
    {
        public static string Replace(this string str, string oldValue, string newValue, bool ignoreCase, CultureInfo culture)
        {
#if DotNetCore
            return str.Replace(oldValue, newValue, ignoreCase, culture);
#else
            return str.Replace(oldValue, newValue);
#endif
        }

        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
        {
#if DotNetCore
            return str.Replace(oldValue, newValue, comparisonType);
#else
            return str.Replace(oldValue, newValue);
#endif
        }

        public static string AsUnescaped(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return Uri.UnescapeDataString(source);
        }
    }
}
