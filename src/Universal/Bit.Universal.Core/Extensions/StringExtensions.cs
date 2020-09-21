using System.Globalization;
using System.Linq;

namespace System
{
    public static class StringExtensions
    {
        public static string AsUnescaped(this string stringToUnescape)
        {
            if (string.IsNullOrEmpty(stringToUnescape))
                return stringToUnescape;

            return Uri.UnescapeDataString(stringToUnescape);
        }

        /// <summary>
        /// Compares two strings while ignoring case/symbol/case.
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

        public static string AsCamelCase(this string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return string.Join(string.Empty, value.ToCharArray().Select((c, i) => { if (i == 0) return char.ToLowerInvariant(c); else return c; }));
        }
    }
}
