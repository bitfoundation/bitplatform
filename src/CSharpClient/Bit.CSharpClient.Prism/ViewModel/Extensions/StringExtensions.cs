using System.Globalization;

namespace System
{
    public static class StringExtensions
    {
        public static string AsUnescaped(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return Uri.UnescapeDataString(source);
        }

        /// <summary>
        /// Compares two strins while ignoring case/symbol/case.
        /// </summary>
        public static bool AreEqual(this string str1, string str2)
        {
            return CultureInfo.InvariantCulture.CompareInfo.Compare(str1, str2, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace) == 0;
        }

        /// <summary>
        /// Returns a value indicating whether a specified character occurs within this string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesContain(this string source, string value)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace) != -1;
        }

        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesStartWith(this string source, string value)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace);
        }

        /// <summary>
        /// Determines whether the end of this string instance matches the specified string while ignoring case/symbol/case.
        /// </summary>
        public static bool DoesEndWith(this string source, string value)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace);
        }
    }
}
