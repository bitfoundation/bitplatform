using System.Globalization;

namespace System
{
    public static class StringExtensions
    {
        public static string Replace(this string str, string oldValue, string? newValue, bool ignoreCase, CultureInfo? culture)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));

#if DotNetCore
            return str.Replace(oldValue, newValue, ignoreCase, culture);
#else
            return str.Replace(oldValue, newValue);
#endif
        }

        public static string Replace(this string str, string oldValue, string? newValue, StringComparison comparisonType)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));

#if DotNetCore
            return str.Replace(oldValue, newValue, comparisonType);
#else
            return str.Replace(oldValue, newValue);
#endif
        }

        public static string AsUnescaped(this string stringToUnescape)
        {
            if (string.IsNullOrEmpty(stringToUnescape))
                return stringToUnescape;

            return Uri.UnescapeDataString(stringToUnescape);
        }

        /// <summary>
        /// Compares two strins while ignoring case/symbol/case.
        /// </summary>
        public static bool AreEqual(this string? string1, string? string2)
        {
            return CultureInfo.InvariantCulture.CompareInfo.Compare(string1, string2, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace) == 0;
        }

        /// <summary>
        /// Returns a value indicating whether a specified character occurs within this string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesContain(this string source, string value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace) != -1;
        }

        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesStartWith(this string source, string prefix)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(source, prefix, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace);
        }

        /// <summary>
        /// Determines whether the end of this string instance matches the specified string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesEndWith(this string source, string suffix)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(source, suffix, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace);
        }
    }
}
